resource "yandex_logging_group" "logs" {
  name             = local.prefix
  retention_period = "24h"
}

resource "yandex_resourcemanager_folder_iam_member" "log-writer" {
  folder_id = data.yandex_resourcemanager_folder.current.folder_id

  role   = "logging.writer"
  member = "serviceAccount:${yandex_iam_service_account.instance_sa.id}"
}

locals {

  fluentbit_time_key    = "Timestamp"
  fluentbit_time_format = "%Y-%m-%dT%H:%M:%S.%L%z"
  fluentbit_message_key = "Message"
  fluentbit_level_key   = "LogLevel"

  fluentbit_conf = <<EOF
[SERVICE]
    Flush         1
    Log_File      /var/log/fluentbit.log
    Log_Level     error
    Daemon        off
    Parsers_File  /fluent-bit/etc/parsers.conf

[FILTER]
    Name          parser
    Key_Name      log
    Parser        docker
    Reserve_Data  On
    Match         stdout.*

[INPUT]
    name          opentelemetry
    listen        0.0.0.0
    tag_from_uri  false
    tag           otel
    port          4318

[INPUT]
    Name              forward
    Listen            0.0.0.0
    Port              24224
    Buffer_Chunk_Size 1M
    Buffer_Max_Size   6M
    Tag_Prefix        stdout

[OUTPUT]
    Name            yc-logging
    Match           otel
    group_id        ${yandex_logging_group.logs.id}
    default_level   INFO
    authorization   instance-service-account

[OUTPUT]
    Name            stdout
    Match           otel

[OUTPUT]
    Name            yc-logging
    Match           stdout.*
    group_id        ${yandex_logging_group.logs.id}
    message_key     ${local.fluentbit_message_key}
    level_key       ${local.fluentbit_level_key}
    default_level   INFO
    authorization   instance-service-account
  EOF

  fluentbit_parsers_conf = <<EOF
[PARSER]
    Name        docker
    Format      json
    Time_Key    ${local.fluentbit_time_key}
    Time_Format ${local.fluentbit_time_format}
    Time_Keep   On
  EOF
  fluentbit_compose = {
    container_name = "fluentbit"
    image          = "cr.yandex/${data.yandex_container_repository.fluentbit.name}:dev-4.0.0"
    ports = [
      "24224:24224",
      "4318:4318",
      "24224:24224/udp"
    ]
    restart = "always"
    environment = {
      YC_GROUP_ID = yandex_logging_group.logs.id
    }
    volumes = [
      "/etc/fluentbit/fluentbit.conf:/fluent-bit/etc/fluent-bit.conf",
      "/etc/fluentbit/parsers.conf:/fluent-bit/etc/parsers.conf",
      "/var/log/fluentbit.log:/var/log/fluentbit.log"
    ]
  }

  fluentbit_cloud_config_files = [
    {
      content = local.fluentbit_conf
      path    = "/etc/fluentbit/fluentbit.conf"
    },
    {
      content = local.fluentbit_parsers_conf
      path    = "/etc/fluentbit/parsers.conf"
    }
  ]
}

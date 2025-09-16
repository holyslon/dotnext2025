resource "yandex_iam_service_account" "instance_sa" {
  folder_id = data.yandex_resourcemanager_folder.current.folder_id
  name      = "${local.prefix}-instance-sa"
}

resource "yandex_iam_service_account" "instance_group_sa" {
  folder_id = data.yandex_resourcemanager_folder.current.folder_id
  name      = "${local.prefix}-instance-group-sa"
}

resource "yandex_container_repository_iam_binding" "image_puller" {
  repository_id = data.yandex_container_repository.image.id
  role          = "container-registry.images.puller"

  members = [
    "serviceAccount:${yandex_iam_service_account.instance_sa.id}",
  ]
}

resource "yandex_storage_bucket_iam_binding" "instance_sa_data_binding" {
  bucket = resource.yandex_storage_bucket.data.bucket
  role   = "storage.admin"
  members = [
    "serviceAccount:${yandex_iam_service_account.instance_sa.id}",
  ]
}

resource "yandex_container_repository_iam_binding" "fluentbit_image_puller" {
  repository_id = data.yandex_container_repository.fluentbit.id
  role          = "container-registry.images.puller"

  members = [
    "serviceAccount:${yandex_iam_service_account.instance_sa.id}",
  ]
}

resource "random_password" "tg_callback_token" {
  length           = 24
  special          = true
  override_special = "_%@"
}

locals {
  connection_string = <<EOF
    Server=${yandex_mdb_postgresql_cluster.database.host[0].fqdn};
    Port=6432;
    Database=${yandex_mdb_postgresql_database.database.name};
    User ID=${yandex_mdb_postgresql_user.user.name};
    Password=${yandex_mdb_postgresql_user.user.password};
    Encoding=UTF8;
    Client Encoding=UTF8;
  EOF

  app_compose = {
    container_name = "server"
    image          = "cr.yandex/${data.yandex_container_repository.image.name}:${local.version}"
    ports = [
      "80:80",
    ]
    logging = {
      driver = "fluentd"
      options = {
        fluentd-address = "localhost:24224"
        tag             = "server"
      }
    }
    restart = "always"
    environment = {
      Telegram__Token                       = var.telegram_api_key
      NETWORKINGBOT_UseTracingExporter      = "OTLP"
      NETWORKINGBOT_UseMetricsExporter      = "OTLP"
      NETWORKINGBOT_UseLogExporter          = "OTLP"
      NETWORKINGBOT_ConnectionStrings__Otlp = "http://logger:4318"
      NETWORKINGBOT_Leaderboard__BucketName = resource.yandex_storage_bucket.data.bucket
      NETWORKINGBOT_App__UpdateSecretToken  = resource.random_password.tg_callback_token.result
      ConnectionStrings__PG                 = local.connection_string
      App__BaseUrl                          = "https://${local.full_domain}"
      ASPNETCORE_ENVIRONMENT                = "Production"
      ASPNETCORE_URLS                       = "http://0.0.0.0:80"
    }
  }


  compose = {
    version = "3.7"
    services = {
      server = local.app_compose
      logger = local.fluentbit_compose
    }
  }

  cloud_config = {
    write_files = local.fluentbit_cloud_config_files
  }
}

data "yandex_compute_image" "container-optimized-image" {
  family = "container-optimized-image"
}
resource "yandex_resourcemanager_folder_iam_member" "editor" {
  folder_id = data.yandex_resourcemanager_folder.current.folder_id

  role   = "editor"
  member = "serviceAccount:${yandex_iam_service_account.instance_group_sa.id}"
}
resource "yandex_compute_instance_group" "compute" {
  name               = "${local.prefix}-compute"
  service_account_id = yandex_iam_service_account.instance_group_sa.id
  instance_template {
    service_account_id = yandex_iam_service_account.instance_sa.id
    platform_id        = "standard-v1"
    resources {
      memory = 2
      cores  = 2
    }
    boot_disk {
      mode = "READ_WRITE"
      initialize_params {
        image_id = data.yandex_compute_image.container-optimized-image.id
      }
    }
    network_interface {
      network_id = data.yandex_vpc_network.vpc.network_id
      subnet_ids = [data.yandex_vpc_subnet.subnet_a.subnet_id]
      nat        = true
    }
    metadata = {
      enable-oslogin     = true
      docker-compose     = yamlencode(local.compose)
      user-data          = "#cloud-config\n${yamlencode(local.cloud_config)}"
      serial-port-enable = 1
    }
  }
  scale_policy {
    fixed_scale {
      size = 1
    }
  }
  allocation_policy {
    zones = [data.yandex_vpc_subnet.subnet_a.zone]
  }
  deploy_policy {
    max_unavailable = 2
    max_creating    = 2
    max_expansion   = 2
    max_deleting    = 2
  }

  application_load_balancer {
    target_group_name = "${local.prefix}-target-group"
  }

  health_check {
    http_options {
      path = "/health"
      port = 80

    }
    healthy_threshold   = 3
    unhealthy_threshold = 5
    timeout             = 30
    interval            = 60
  }

  labels = {
    app = local.prefix
  }
  depends_on = [yandex_resourcemanager_folder_iam_member.editor]
}

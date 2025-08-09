resource "yandex_iam_service_account" "compute_sa" {
  folder_id = data.yandex_resourcemanager_folder.current.folder_id
  name      = "${local.prefix}-compute-sa"
}
resource "yandex_container_repository_iam_binding" "image_puller" {
  repository_id = data.yandex_container_repository.image.id
  role          = "container-registry.images.puller"

  members = [
    "serviceAccount:${yandex_iam_service_account.compute_sa.id}",
  ]
}

data "yandex_lockbox_secret_version" "bot_api_key_current" {
  secret_id = data.yandex_lockbox_secret.bot_api_key.secret_id
  version_id = data.yandex_lockbox_secret.bot_api_key.current_version[0]["id"]
}

locals {
  app_compose = {
    container_name = "server"
    image          = "${data.yandex_container_repository.image.name}:${local.version}"
    ports = [
      "80:80",
    ]
    logging = {
      driver = "local"
      options = {
        max-size = "20m"
      }
    }
    restart = "always"
    environment = {
      Telegram__Token        = data.yandex_lockbox_secret_version.bot_api_key_current.entries[0]["text_value"]
      ASPNETCORE_ENVIRONMENT = "Production"
      ASPNETCORE_URLS        = "http://0.0.0.0:80"
    }
  }


  compose = {
    version = "3.7"
    services = {
      server = local.app_compose
    }
  }
}

data "yandex_compute_image" "container-optimized-image" {
  family = "container-optimized-image"
}

resource "yandex_resourcemanager_folder_iam_member" "vpc_admin" {
  folder_id = data.yandex_resourcemanager_folder.current.folder_id

  role   = "vpc.admin"
  member = "serviceAccount:${yandex_iam_service_account.compute_sa.id}"
}
resource "yandex_resourcemanager_folder_iam_member" "vpc_user" {
  folder_id = data.yandex_resourcemanager_folder.current.folder_id

  role   = "vpc.user"
  member = "serviceAccount:${yandex_iam_service_account.compute_sa.id}"
}
resource "yandex_resourcemanager_folder_iam_member" "editor" {
  folder_id = data.yandex_resourcemanager_folder.current.folder_id

  role   = "editor"
  member = "serviceAccount:${yandex_iam_service_account.compute_sa.id}"
}
resource "yandex_compute_instance_group" "signature_ig" {
  name               = "${local.prefix}-compute"
  service_account_id = yandex_iam_service_account.compute_sa.id
  instance_template {
    service_account_id = yandex_iam_service_account.compute_sa.id
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
      serial-port-enable = 1
    }
  }
  application_load_balancer {
    target_group_name = "signature-compute-target-group"
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

  depends_on = [yandex_resourcemanager_folder_iam_member.vpc_admin]
}

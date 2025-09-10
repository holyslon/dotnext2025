terraform {
  required_providers {
    yandex = {
      source = "yandex-cloud/yandex"
    }
  }
  required_version = ">= 0.13"
  backend "s3" {
    endpoints = { s3 = "https://storage.yandexcloud.net" }
    bucket    = "onikiychuk-dotnet2025-tf"
    region    = "ru-central1"
    key       = "networking-bot.tfstate"

    skip_region_validation      = true
    skip_credentials_validation = true
    skip_metadata_api_check     = true
    skip_requesting_account_id  = true
    skip_s3_checksum            = true

  }
}

variable "telegram_api_key" {
  sensitive = true
}

variable "app_version" {
  default = "0.1.5"
}

variable "folder_id" {
  default = "b1gnafc9kku5rbbnd3j9"
}
variable "prefix" {
  default = "dotnext2025"
}
locals {
  folder_id = var.folder_id
  prefix    = var.prefix
  domain    = var.prefix
}


provider "yandex" {
  zone                     = "ru-central1-a"
  service_account_key_file = "key.json"
  folder_id                = local.folder_id
}

data "yandex_resourcemanager_folder" "current" {
  folder_id = local.folder_id
}

data "yandex_container_registry" "registry" {
  registry_id = "crpqhh1mlq02qggf1aj3"
  folder_id   = data.yandex_resourcemanager_folder.current.folder_id
}

data "yandex_vpc_network" "vpc" {
  network_id = "enptnu47bb7tjspdlt30"
  folder_id  = data.yandex_resourcemanager_folder.current.folder_id
}

data "yandex_vpc_subnet" "subnet_a" {
  subnet_id = "e9b25td1ck5v22ikgfkp"
  folder_id = data.yandex_resourcemanager_folder.current.folder_id
}

locals {
  version = var.app_version
}

data "yandex_container_repository" "image" {
  name = "${data.yandex_container_registry.registry.id}/networking-bot"
}


data "yandex_container_repository" "fluentbit" {
  name = "${data.yandex_container_registry.registry.id}/fluent-bit-plugin-yandex"
}

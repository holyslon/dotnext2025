resource "yandex_vpc_security_group" "database" {
  name       = "${local.prefix}_database_sg"
  network_id = data.yandex_vpc_network.vpc.network_id

  ingress {
    description    = "PostgreSQL"
    port           = 6432
    protocol       = "TCP"
    v4_cidr_blocks = data.yandex_vpc_subnet.subnet_a.v4_cidr_blocks
  }
  labels = {
    app = local.prefix
  }
}

resource "yandex_mdb_postgresql_cluster" "database" {
  name                = "${local.prefix}_pg"
  environment         = "PRODUCTION"
  network_id          = data.yandex_vpc_network.vpc.network_id
  security_group_ids  = [yandex_vpc_security_group.database.id]
  deletion_protection = true

  config {
    version = 15
    resources {
      resource_preset_id = "s2.micro"
      disk_type_id       = "network-ssd"
      disk_size          = "20"
    }
    postgresql_config = {
      timezone            = "UTC"
      password_encryption = "PASSWORD_ENCRYPTION_MD5"
    }
    access {
      web_sql = true
    }
  }

  host {
    zone      = data.yandex_vpc_subnet.subnet_a.zone
    name      = "${local.prefix}-pg-host-a"
    subnet_id = data.yandex_vpc_subnet.subnet_a.subnet_id
  }

  labels = {
    app = local.prefix
  }
}

resource "random_password" "database_password" {
  length           = 24
  special          = true
  override_special = "_%@"
}


resource "yandex_mdb_postgresql_user" "user" {
  cluster_id = yandex_mdb_postgresql_cluster.database.id
  name       = local.prefix
  password   = random_password.database_password.result

}

resource "yandex_mdb_postgresql_database" "database" {
  cluster_id = yandex_mdb_postgresql_cluster.database.id
  name       = local.prefix
  owner      = yandex_mdb_postgresql_user.user.name
  lc_collate = "en_US.UTF-8"
  lc_type    = "en_US.UTF-8"

}

resource "yandex_storage_bucket" "data" {
  bucket = "${local.prefix}-data-bucket"
}
resource "yandex_storage_bucket_grant" "data_grant" {
  bucket = resource.yandex_storage_bucket.data.bucket
  acl    = "public-read"
}
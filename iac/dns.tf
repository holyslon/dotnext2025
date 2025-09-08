
data "yandex_resourcemanager_folder" "dns_folder" {
  folder_id = "b1gdhsnjfspgs9a49hk9"
}
data "yandex_dns_zone" "zone" {
  dns_zone_id = "dnse9tiukt59ob8dtom0"
  folder_id   = data.yandex_resourcemanager_folder.dns_folder.folder_id
}

resource "yandex_vpc_address" "addr" {
  name = "${var.prefix}-external-ip-address"

  external_ipv4_address {
    zone_id = data.yandex_vpc_subnet.subnet_a.zone
  }
  labels = {
    app = local.prefix
  }
}

locals {
  base_domain = data.yandex_dns_zone.zone.zone
  full_domain = trimsuffix("${local.domain}.${local.base_domain}", ".")
}

resource "yandex_dns_recordset" "domain" {
  zone_id = data.yandex_dns_zone.zone.dns_zone_id
  name    = "${local.full_domain}."
  type    = "A"
  ttl     = 600
  data    = [for map in yandex_vpc_address.addr.external_ipv4_address : lookup(map, "address", "")]

}


resource "yandex_cm_certificate" "signature_certificate" {
  name    = "${var.prefix}-certificate"
  domains = [local.full_domain]

  managed {
    challenge_type  = "DNS_CNAME"
    challenge_count = 1 # for each domain
  }
  labels = {
    app = local.prefix
  }
}

resource "yandex_dns_recordset" "dns_ssl_challenges" {
  count   = yandex_cm_certificate.signature_certificate.managed[0].challenge_count
  zone_id = data.yandex_dns_zone.zone.dns_zone_id
  name    = yandex_cm_certificate.signature_certificate.challenges[count.index].dns_name
  type    = yandex_cm_certificate.signature_certificate.challenges[count.index].dns_type
  data    = [yandex_cm_certificate.signature_certificate.challenges[count.index].dns_value]
  ttl     = 60
}
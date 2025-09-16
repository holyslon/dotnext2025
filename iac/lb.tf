resource "yandex_alb_http_router" "router" {
  name = "${local.prefix}-router"
  labels = {
    app = local.prefix
  }
}

resource "yandex_alb_backend_group" "server-backend-group" {
  name = "${local.prefix}-server-backend-group"

  http_backend {
    name             = "${local.prefix}-server-http-backend"
    weight           = 1
    port             = 80
    target_group_ids = [for alb in yandex_compute_instance_group.compute.application_load_balancer : alb.target_group_id]
    load_balancing_config {
      panic_threshold = 50
    }
    healthcheck {
      timeout  = "30s"
      interval = "30s"
      http_healthcheck {
        path = "/ready"
      }
    }
  }
}

resource "yandex_alb_backend_group" "static-api-backend-group" {
  name = "${local.prefix}-static-api-backend-group"

  http_backend {
    name           = "${local.prefix}-static-api-http-backend"
    storage_bucket = resource.yandex_storage_bucket.data.bucket
  }
}


resource "yandex_alb_virtual_host" "vhost" {
  name           = "${local.prefix}-virtual-host"
  http_router_id = yandex_alb_http_router.router.id
  authority      = [local.full_domain]
  route {
    name = "static-json-route"
    http_route {
      http_match {
        path {
          prefix = "/api/static"
        }
      }
      http_route_action {
        backend_group_id = yandex_alb_backend_group.static-api-backend-group.id
        timeout          = "50m0s"
        prefix_rewrite   = " "
      }
    }
  }
  route {
    name = "server-route"

    http_route {
      http_match {
        path {
          prefix = "/"
        }
      }
      http_route_action {
        backend_group_id = yandex_alb_backend_group.server-backend-group.id
        timeout          = "50m0s"
        prefix_rewrite   = "/"
      }
    }
  }

}

resource "yandex_alb_load_balancer" "balancer" {
  name = "${local.prefix}-balancer"

  network_id = data.yandex_vpc_network.vpc.network_id

  allocation_policy {
    location {
      zone_id   = "ru-central1-a"
      subnet_id = data.yandex_vpc_subnet.subnet_a.subnet_id
    }
  }

  listener {
    name = "http-listener"
    endpoint {
      address {
        external_ipv4_address {
          address = yandex_vpc_address.addr.external_ipv4_address[0].address
        }
      }
      ports = [80]
    }
    http {
      redirects {
        http_to_https = true
      }
    }
  }

  listener {
    name = "https-listener"
    endpoint {
      address {
        external_ipv4_address {
          address = yandex_vpc_address.addr.external_ipv4_address[0].address
        }
      }
      ports = [443]
    }
    tls {
      default_handler {
        http_handler {
          http_router_id = yandex_alb_http_router.router.id
        }
        certificate_ids = [yandex_cm_certificate.certificate.id]
      }
    }
  }



  log_options {
    discard_rule {
      http_code_intervals = ["HTTP_2XX"]
      discard_percent     = 75
    }
  }
}

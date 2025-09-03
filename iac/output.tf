output "instance_public_ips" {
  value       = [for instance in yandex_compute_instance_group.compute.instances : instance.network_interface[*].ip_address] # The actual value to be outputted
  description = "The public IP address of the EC2 instance"                                                                  # Description of what this output represents
}
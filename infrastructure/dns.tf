# 1. Look up your Route 53 Hosted Zone
data "aws_route53_zone" "main" {
  name         = "jackmcbride.dev"
  private_zone = false
}

# 2. Request a free SSL certificate from ACM for your subdomain
resource "aws_acm_certificate" "cert" {
  
  domain_name       = "playlistsearchtool.jackmcbride.dev"
  validation_method = "DNS"

  lifecycle {
    create_before_destroy = true
  }

  tags = { Name = "playlist-search-tool-cert" }
}

# 3. Automatically create the DNS validation records in Route 53
resource "aws_route53_record" "cert_validation" {
  for_each = {
    for dvo in aws_acm_certificate.cert.domain_validation_options : dvo.domain_name => {
      name   = dvo.resource_record_name
      record = dvo.resource_record_value
      type   = dvo.resource_record_type
    }
  }

  allow_overwrite = true
  name            = each.value.name
  records         = [each.value.record]
  ttl             = 60
  type            = each.value.type
  zone_id         = data.aws_route53_zone.main.zone_id
}

# 4. Wait until ACM confirms the certificate is validated
resource "aws_acm_certificate_validation" "cert" {
  certificate_arn         = aws_acm_certificate.cert.arn
  validation_record_fqdns = [for record in aws_route53_record.cert_validation : record.fqdn]
}

# 5. Create the Route 53 Alias Record pointing your subdomain to your ALB
resource "aws_route53_record" "subdomain" {
  zone_id = data.aws_route53_zone.main.zone_id
  name    = "playlistsearchtool.jackmcbride.dev"
  type    = "A"

  alias {
    name                   = aws_lb.main.dns_name
    zone_id                = aws_lb.main.zone_id
    evaluate_target_health = true
  }
}
resource "aws_ecr_repository" "api" {
  name                 = "playlist-search-tool-api"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }
}

resource "aws_ecr_repository" "migrate" {
  name                 = "playlist-search-tool-migrations"
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }
}

output "api_ecr_repository_url" {
  value = aws_ecr_repository.api.repository_url
}

output "migrate_ecr_repository_url" {
  value = aws_ecr_repository.migrate.repository_url
}
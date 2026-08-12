terraform {
  required_version = ">= 1.6.0"
  required_providers {
    aws = {
      source  = "registry.opentofu.org/hashicorp/aws"
      version = "~> 5.0"
    }
  }
  
  backend "s3" {
    bucket         = "spotify-playlist-search-tool-tfstate-687979656894"
    key            = "infrastructure/state.tfstate"
    region         = "eu-west-2"
    encrypt        = true
  }
}

provider "aws" {
  region = "eu-west-2"
}

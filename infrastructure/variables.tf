variable "spotify_client_id" {
  type      = string
  sensitive = true
}

variable "spotify_client_secret" {
  type      = string
  sensitive = true
}

variable "spotify_redirect_uri" {
  type = string
}

variable "db_name" {
  type    = string
  default = "playlistdb"
}

variable "db_username" {
  type    = string
  default = "dbadmin"
}

variable "db_password" {
  type      = string
  sensitive = true
}

variable "db_port" {
  type    = number
  default = 5432
}
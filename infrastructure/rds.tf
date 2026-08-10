# 1. DB Subnet Group (Places the database in your public subnets)
resource "aws_db_subnet_group" "main" {
  name       = "playlist-search-tool-db-subnet-group"
  subnet_ids = [aws_subnet.public_a.id, aws_subnet.public_b.id]

  lifecycle {
    create_before_destroy = true
  }
  
  tags = {
    Name = "playlist-search-tool-db-subnet-group"
  }
}

# 2. Security Group for PostgreSQL (Allows public connection from the internet)
resource "aws_security_group" "rds" {
  name        = "playlist-search-tool-rds-sg"
  description = "Allow inbound traffic to PostgreSQL"
  vpc_id      = aws_vpc.main.id

  ingress {
    description = "PostgreSQL from anywhere (Public)"
    from_port   = var.db_port
    to_port     = var.db_port
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"] # WARNING: Exposes your database port to the entire internet
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "playlist-search-tool-rds-sg"
  }
}

# 3. PostgreSQL RDS Instance
resource "aws_db_instance" "postgres" {
  identifier             = "playlist-search-tool-db"
  engine                 = "postgres"
  engine_version         = "15"
  auto_minor_version_upgrade = true
  instance_class         = "db.t3.micro"
  allocated_storage      = 20
  max_allocated_storage  = 100
  storage_type           = "gp2"

  db_name  = var.db_name
  username = var.db_username
  password = var.db_password

  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.rds.id]

  publicly_accessible    = true # consider at some point if we can find a way to run the db migrations without this

  skip_final_snapshot    = true

  tags = {
    Name = "playlist-search-tool-rds"
  }
}

output "database_host" {
  value = aws_db_instance.postgres.address
}
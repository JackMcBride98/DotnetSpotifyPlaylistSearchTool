# 1. DB Subnet Group (Places the database in your public subnets)
resource "aws_db_subnet_group" "main" {
  name       = "playlist-search-tool-db-subnet-group"
  subnet_ids = [aws_subnet.private_a.id, aws_subnet.private_b.id]
  
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
    description     = "PostgreSQL from ECS Tasks only"
    from_port       = var.db_port
    to_port         = var.db_port
    protocol        = "tcp"
    security_groups = [aws_security_group.ecs_tasks.id]
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

  publicly_accessible    = false

  skip_final_snapshot    = true

  tags = {
    Name = "playlist-search-tool-rds"
  }
}

output "database_host" {
  value = aws_db_instance.postgres.address
}

# 4. Task for running database migrations
resource "aws_ecs_task_definition" "migrations" {
  family                   = "playlist-search-tool-migrations"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = "256"
  memory                   = "512"
  execution_role_arn       = aws_iam_role.ecs_execution_role.arn

  container_definitions = jsonencode([
    {
      name      = "migrations"
      essential = true
      environment = [
        { name = "Database__ConnectionString", value = "Host=${aws_db_instance.postgres.address};Port=${var.db_port};Database=${var.db_name};Username=${var.db_username};Password=${var.db_password}" }
      ]
    }
  ])
}
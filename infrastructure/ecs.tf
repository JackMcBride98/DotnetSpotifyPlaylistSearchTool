# 1. ECS Cluster
resource "aws_ecs_cluster" "main" {
  name = "playlist-search-tool-cluster"
}

# 2. Security Group for ECS Tasks
resource "aws_security_group" "ecs_tasks" {
  name        = "playlist-search-tool-ecs-tasks-sg"
  description = "Allow outbound access for ECS tasks"
  vpc_id      = aws_vpc.main.id

  ingress {
    from_port       = 8080
    to_port         = 8080
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]
  }
  
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "playlist-search-tool-ecs-tasks-sg"
  }
}

# 3. IAM Role for ECS Task Execution (Allows pulling images and writing logs)
resource "aws_iam_role" "ecs_execution_role" {
  name = "playlist-search-tool-ecs-execution-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "ecs-tasks.amazonaws.com"
        }
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "ecs_execution_role_policy" {
  role       = aws_iam_role.ecs_execution_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

# 4. ECS Task Definition (Points to your container image, e.g., from Docker Hub or ECR)
resource "aws_ecs_task_definition" "api" {
  family                   = "playlist-search-tool-api"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = "256"
  memory                   = "512"
  execution_role_arn       = aws_iam_role.ecs_execution_role.arn

  container_definitions = jsonencode([
    {
      name      = "backend"
      essential = true
      portMappings = [
        {
          containerPort = 8080
          hostPort      = 8080
        }
      ]
      environment = [
        { name = "Spotify__ClientId", value = var.spotify_client_id },
        { name = "Spotify__ClientSecret", value = var.spotify_client_secret },
        { name = "Spotify__RedirectUri", value = var.spotify_redirect_uri },
        { name = "Database__ConnectionString", value = "Host=${aws_db_instance.postgres.address};Port=${var.db_port};Database=${var.db_name};Username=${var.db_username};Password=${var.db_password}" }
      ]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = "/ecs/playlist-search-tool-api"
          "awslogs-region"        = "eu-west-2"
          "awslogs-stream-prefix" = "backend"
        }
      }
    }
  ])
}

resource "aws_cloudwatch_log_group" "api_logs" {
  name              = "/ecs/playlist-search-tool-api"
  retention_in_days = 7
}

# 5. The ECS Service (Maintains your desired container count and hooks into the ALB)
resource "aws_ecs_service" "api" {
  name            = "playlist-search-tool-service"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.api.arn
  desired_count   = 1
  launch_type     = "FARGATE"
  
  deployment_circuit_breaker {
    enable   = true
    rollback = true
  }

  network_configuration {
    subnets          = [aws_subnet.public_a.id, aws_subnet.public_b.id] # Public so we can pull the docker images
    security_groups  = [aws_security_group.ecs_tasks.id]
    assign_public_ip = true
  }

  # This is the crucial link: connects ECS to your Application Load Balancer Target Group
  load_balancer {
    target_group_arn = aws_lb_target_group.backend.arn
    container_name   = "backend"
    container_port   = 8080
  }

  depends_on = [aws_lb_listener.http]
}
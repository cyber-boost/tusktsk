# Configure the AWS Provider
provider "aws" {
  region = var.aws_region
}

# VPC Configuration
resource "aws_vpc" "tusklang_vpc" {
  cidr_block           = var.vpc_cidr
  enable_dns_hostnames = true
  enable_dns_support   = true

  tags = {
    Name = "tusklang-vpc"
    Environment = var.environment
  }
}

# Internet Gateway
resource "aws_internet_gateway" "tusklang_igw" {
  vpc_id = aws_vpc.tusklang_vpc.id

  tags = {
    Name = "tusklang-igw"
    Environment = var.environment
  }
}

# Public Subnets
resource "aws_subnet" "public" {
  count             = length(var.public_subnets)
  vpc_id            = aws_vpc.tusklang_vpc.id
  cidr_block        = var.public_subnets[count.index]
  availability_zone = var.availability_zones[count.index]

  map_public_ip_on_launch = true

  tags = {
    Name = "tusklang-public-${count.index + 1}"
    Environment = var.environment
  }
}

# Private Subnets
resource "aws_subnet" "private" {
  count             = length(var.private_subnets)
  vpc_id            = aws_vpc.tusklang_vpc.id
  cidr_block        = var.private_subnets[count.index]
  availability_zone = var.availability_zones[count.index]

  tags = {
    Name = "tusklang-private-${count.index + 1}"
    Environment = var.environment
  }
}

# Route Tables
resource "aws_route_table" "public" {
  vpc_id = aws_vpc.tusklang_vpc.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.tusklang_igw.id
  }

  tags = {
    Name = "tusklang-public-rt"
    Environment = var.environment
  }
}

# Route Table Associations
resource "aws_route_table_association" "public" {
  count          = length(var.public_subnets)
  subnet_id      = aws_subnet.public[count.index].id
  route_table_id = aws_route_table.public.id
}

# EKS Cluster
resource "aws_eks_cluster" "tusklang_cluster" {
  name     = "tusklang-${var.environment}"
  role_arn = aws_iam_role.eks_cluster.arn
  version  = var.kubernetes_version

  vpc_config {
    subnet_ids              = aws_subnet.private[*].id
    endpoint_private_access = true
    endpoint_public_access  = true
  }

  depends_on = [
    aws_iam_role_policy_attachment.eks_cluster_policy,
    aws_iam_role_policy_attachment.eks_vpc_resource_controller,
  ]

  tags = {
    Name = "tusklang-${var.environment}"
    Environment = var.environment
  }
}

# EKS Node Group
resource "aws_eks_node_group" "tusklang_nodes" {
  cluster_name    = aws_eks_cluster.tusklang_cluster.name
  node_group_name = "tusklang-nodes"
  node_role_arn   = aws_iam_role.eks_nodes.arn
  subnet_ids      = aws_subnet.private[*].id
  instance_types  = var.node_instance_types

  scaling_config {
    desired_size = var.node_desired_size
    max_size     = var.node_max_size
    min_size     = var.node_min_size
  }

  depends_on = [
    aws_iam_role_policy_attachment.eks_worker_node_policy,
    aws_iam_role_policy_attachment.eks_cni_policy,
    aws_iam_role_policy_attachment.ec2_container_registry_read_only,
  ]

  tags = {
    Name = "tusklang-nodes"
    Environment = var.environment
  }
}

# RDS Database
resource "aws_db_instance" "tusklang_db" {
  identifier = "tusklang-${var.environment}"

  engine         = "postgres"
  engine_version = "13.7"
  instance_class = var.db_instance_class

  allocated_storage     = var.db_allocated_storage
  max_allocated_storage = var.db_max_allocated_storage
  storage_encrypted     = true

  db_name  = "tusklang"
  username = var.db_username
  password = var.db_password

  vpc_security_group_ids = [aws_security_group.rds.id]
  db_subnet_group_name   = aws_db_subnet_group.tusklang.name

  backup_retention_period = 7
  backup_window          = "03:00-04:00"
  maintenance_window     = "sun:04:00-sun:05:00"

  skip_final_snapshot = true

  tags = {
    Name = "tusklang-${var.environment}"
    Environment = var.environment
  }
}

# ElastiCache Redis
resource "aws_elasticache_cluster" "tusklang_redis" {
  cluster_id           = "tusklang-${var.environment}"
  engine               = "redis"
  node_type            = var.redis_node_type
  num_cache_nodes      = 1
  parameter_group_name = "default.redis6.x"
  port                 = 6379
  security_group_ids   = [aws_security_group.redis.id]
  subnet_group_name    = aws_elasticache_subnet_group.tusklang.name

  tags = {
    Name = "tusklang-${var.environment}"
    Environment = var.environment
  }
}

# Application Load Balancer
resource "aws_lb" "tusklang_alb" {
  name               = "tusklang-${var.environment}"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb.id]
  subnets            = aws_subnet.public[*].id

  enable_deletion_protection = false

  tags = {
    Name = "tusklang-${var.environment}"
    Environment = var.environment
  }
}

# ALB Target Group
resource "aws_lb_target_group" "tusklang_tg" {
  name     = "tusklang-${var.environment}"
  port     = 80
  protocol = "HTTP"
  vpc_id   = aws_vpc.tusklang_vpc.id

  health_check {
    enabled             = true
    healthy_threshold   = 2
    interval            = 30
    matcher             = "200"
    path                = "/health"
    port                = "traffic-port"
    protocol            = "HTTP"
    timeout             = 5
    unhealthy_threshold = 2
  }

  tags = {
    Name = "tusklang-${var.environment}"
    Environment = var.environment
  }
}

# ALB Listener
resource "aws_lb_listener" "tusklang_listener" {
  load_balancer_arn = aws_lb.tusklang_alb.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.tusklang_tg.arn
  }
}

# CloudWatch Log Group
resource "aws_cloudwatch_log_group" "tusklang_logs" {
  name              = "/aws/eks/tusklang-${var.environment}/cluster"
  retention_in_days = 30

  tags = {
    Name = "tusklang-${var.environment}"
    Environment = var.environment
  }
} 
output "public_subnet_ids" {
  value = [aws_subnet.public_a.id, aws_subnet.public_b.id]
}

output "ecs_tasks_security_group_id" {
  value = aws_security_group.ecs_tasks.id
}
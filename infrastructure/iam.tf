# 1. Create the OpenID Connect provider for GitHub Actions as a resource
resource "aws_iam_openid_connect_provider" "github" {
  url             = "https://token.actions.githubusercontent.com"
  client_id_list  = ["sts.amazonaws.com"]
  thumbprint_list = ["6938fd4d98bab03faadb97b34396831e3780aea1"]
}

# 2. Define the IAM Role that GitHub Actions will assume
resource "aws_iam_role" "github_actions" {
  name = "playlist-search-tool-github-actions-role"

  # Trust policy allowing GitHub's OIDC provider to assume this role for your specific repo
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Federated = aws_iam_openid_connect_provider.github.arn
        }
        Action = "sts:AssumeRoleWithWebIdentity"
        Condition = {
          StringEquals = {
            "token.actions.githubusercontent.com:aud" = "sts.amazonaws.com"
          }
          StringLike = {
            "token.actions.githubusercontent.com:sub" = "repo:JackMcBride98/DotnetSpotifyPlaylistSearchTool:*"
          }
        }
      }
    ]
  })
}

# 3. Attach Administrator permissions to the role
resource "aws_iam_role_policy_attachment" "github_actions_admin" {
  role       = aws_iam_role.github_actions.name
  policy_arn = "arn:aws:iam::aws:policy/AdministratorAccess"
}

# Output the role ARN so you can verify it matches your workflow env vars
output "github_actions_role_arn" {
  value = aws_iam_role.github_actions.arn
}
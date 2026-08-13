# Infrastructure Documentation and Write Up

A containerised full-stack Spotify playlist search tool featuring:
- React Progressive Web App frontend
- .NET 10 API backend (which serves the frontend)
- RDS PostgreSQL database
- Fully automated CI/CD with OpenTofu

# Architecture

We have a VPC (Virtual Private Cloud) which contains two public subnets and two private subnets, you need two for zone
redundancy. On the Public subnet we have an ALB (Application Load Balancer), which acts as the front door for people on the internet
to access our services on the VPC. 

We use AWS Route 53 to create the DNS records required to route playlistsearchtool.jackmcbride.dev to the ALB's url, this is
backed up by an ACM SSL/TLS certificate for HTTPS.

On the public subnets we have the ECS Service which hosts the API, the API is serving the frontend. Ideally we would have put
this on a private subnet, but it needs access to the internet to get the docker images from ECR, which we could have setup with a 
NAT gateway, but these are expensive to run, so we have it on public and access to public internet/ECR setup with a Security Group.
The ALB forwards traffic from the internet to the API over http and https.
All http traffic is rerouted to https as the ALB's default action for port 80 (http) is redirect. The ECS service is running the 
latest docker image of the API which is uploaded as part of CI/CD deploy script, more on that later.

On our private subnets we have an RDS (Relational Database Service) PostgreSQL database. This is a good security practice,
as having a database in a private subnet means even with the connection string, it's impossible to connect to the database unless
inside the VPC. We have a security group and route tables which allows the API to connect to the private subnet via the connection string
since they both live in the same VPC.

We use ECR to host our docker images of the migrations and API project. ECS hosts defintions of our API and Migrations tasks.
Tasks reference docker images in ECR. 

We create a role for the Github Actions, with Administrator access (bad habit would be better to tie down to individual roles required)
We setup OIDC connect for this and put our created access token for this in the actions secrets in GitHub.


# Deployment & CI/CD Pipeline
Using the permission created in IAM and connecting via OIDC connect. The github actions runner has permission to login to AWS.
We then use tofu to detect infra changes and apply any that need applying. Once that is done we build and publish the application code
for the migrations and api project. And then upload these to ECR as docker images. Then we update the task defintions in ECS 
to point at the latest uploaded docker images, these are tagged by commit sha, we then run these tasks, first the database migrations
as a one off task and then the deploy of the API task to the API ECS service, which handles zero downtime deployment.

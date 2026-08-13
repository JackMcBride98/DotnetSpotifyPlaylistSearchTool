# Overview

This is a project used to search a Spotify User's saved playlists by their contained tracks title and artist.

The user's playlists are fetched via the Spotify API and saved into a PostgresSQL Database the first time they use the app
and then updated periodically (weekly if the user was active in the last week) by a background job. This helps reduce the calls to Spotify API.

The project uses Cake (C# Make) as a build automation system. Run ./cake at the root for a list of commands.
Tip: You can use the --exclusive flag to run Cake tasks without running the tasks they are dependent on

# Prerequisites
- .NET 10 SDK
- node 24+
- Docker Desktop (for running PostgresSQL database)
- Install yamllint
- CSharpier extension for formatting C# code
- Install AWS CLI and opentofu (for deploying to AWS)
- Most likely only works on a Windows machine, as developed in Windows using Rider IDE and VsCode

# Project folder structure

`/client` contains the vite, typscript and React frontend

`/SpotifyPlaylistSearchTool.Api` contains the C# .NET, FastEndpoints, EFCore backend API

`/SpotifyPlaylistSearchTool.Database` contains the DbUp console project and docker compose file for creating and migrating the database. 
This is used instead of EFCore migrations, to allow complete control of the generated SQL, allowing more flexibility in for example data migrations. 
There are no down migrations as DbUp is opinionated against them. Although they could be added, there is a package to handle that. See DbUp documentation.

`./build` contains the Cake Frosting project which organises local scripts for the project. Run ./cake in the root directory to see a list of commands
(side note for self) These take a while on my machine, I'm unsure if its the way I've set it up or my computer being an 8 year old brick.

`./BackendE2ETests` contains integration/end-to-end tests for the backend API. These are run against a local test database 
and use mock the results of Spotify API. These are setup as reccomened by FastEndpoints [documentation](https://fast-endpoints.com/docs/integration-unit-testing).

# Running the app locally
Ensure you have installed the necceessary deps in prerequisites. Build the solution and run `npm install` in the client folder

First create the local database by running `./cake CreateLocalDatabase`

Then migrate the local database by running `./cake MigrateLocalDatabase`

Within the `./SpotifyPlaylistSearchTool.Api` folder
Update `appsettings.Development.json` with the Spotify API client ID and secret, these can be found through the Spotify Developer Dashboard.

Update the Database::ConnectionString in the appsettings to `"Host=localhost;Port=5433;Database=SpotifyPlaylistSearchTool;Username=postgres;Password=mysecretpassword"`

Then run the backend with `dotnet watch run` and go to `http://localhost:5030/`

Run the frontend by navigating to the `/client` directory and then running `npm run dev`

# Development
Attach the client folder to the solution to get automcpletes

# Running migrations
Use `./cake MigrateLocalDatabase` to run the latest migrations e.g. if you have added any new ones in development

# Testing
To run the BackendE2ETests, first create a local test database by running `./cake CreateTestDatabase` 
and then run the tests with `./cake RunBackendE2ETests` (you can run them within Rider as well, make sure to change the 
environment variable of Rider's test environment to "Testing" so that the test database is used instead of the development database,
this is set in committed DotSettings.user, so you may not need to do this.)

# CI Pipelines
Pipelines are ran using Github Actions. These live in ./github/workflows. They call jobs defined in the Cake Frosting
build project. 

# Infrastructure
the `/infrastructure` folder contains terraform files for creating the AWS resources used to host the DB, Server and frontend.

Create a `terraform.tfvars` file in the `/infrastructure` folder with the variables from `terraform.tfvars.example`.

When running tofu commands locally make sure to set the AWS_PROFILE environment variable to the profile you want to use for deployment.
$env:AWS_PROFILE = "spotify-playlist-search-tool-admin"
tofu plan

https://687979656894.signin.aws.amazon.com/console - console sign in link for playlist-search-tool-admin. This is
who you want to be signed in with when using tofu locally.

# TODO
- (later/stretch) Error Handling could be much better (think I found a limitation of the client-gen library). We need to surface error messages and status codes from the backend to frontend in  a typesafe way.
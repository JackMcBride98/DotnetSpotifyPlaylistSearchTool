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
- Most likely only works on a Windows machine, as developed in Windows using Rider IDE and VsCode

# Project folder structure

`/client` contains the vite, typscript and React frontend

`/SpotifyPlaylistSearchTool.Api` contains the C# .NET, FastEndpoints, EFCore backend API

`/SpotifyPlaylistSearchTool.Database` contains the DbUp console project and docker compose file for creating and migrating the database. 
This is used instead of EFCore migrations, to allow complete control of the generated SQL, allowing more flexibility in for example data migrations. 
There are no down migrations as DbUp is opinionated against them. Although they could be added, there is a package to handle that. See DbUp documentation.

`./build` contains the Cake Frosting project which organises local scripts for the project. Run ./cake in the root directory to see a list of commands
(side note for self) These take a while on my machine, I'm unsure if its the way I've set it up or my computer being not so good.

`./BackendE2ETests` contains integration/end-to-end tests for the backend API. These are run against a local test database 
and use mock the results of Spotify API. These are setup as reccomened by FastEndpoints [documentation](https://fast-endpoints.com/docs/integration-unit-testing).

# Running the app locally
First create the local database by running `./cake CreateLocalDatabase`

Then migrate the local database by running `./cake MigrateLocalDatabase`

Within the `./SpotifyPlaylistSearchTool.Api` folder
Update `appsettings.Development.json` with the Spotify API client ID and secret, these can be found through the Spotify Developer Dashboard.

Update the Database::ConnectionString in the appsettings to `"Host=localhost;Port=5433;Database=SpotifyPlaylistSearchTool;Username=postgres;Password=mysecretpassword"`

Then run the backend with `dotnet watch run` and go to `http://localhost:5030/`

Run the frontend by navigating to the `/client` directory and then running `npm run dev`

# Running migrations
Use `./cake MigrateLocalDatabase` to run the latest migrations e.g. if you have added any new ones in development

# Testing
To run the BackendE2ETests, first create a local test database by running `./cake CreateLocalTestDatabase` 
and then run the tests with `./cake RunBackendE2ETests` (you can run them within Rider as well, make sure to change the 
environment variable of Rider's test environment to "Testing" so that the test database is used instead of the development database,
this is set in committed DotSettings.user, so you may not need to do this.)

# CI Pipelines
Pipelines are ran using Github Actions. These live in ./github/workflows. They call jobs defined in the Cake Frosting
build project. 

Pipelines can be ran locally using act-cli `choco install act-cli`
Then at root level run `act pull_request`
If you want to test them locally


# TODO
Plan: Client Gen -> Frontent Lint, format and test setup -> Background job for syncing playlists -> Update branding and fix bugs -> AWS Deployment -> IaaC deployment -> PWA (stretch goal)

- Create frontend tests (do we need this)

- Create background job for syncing playlists
- Better logging for SyncSpotifyPlaylistService

- Using owner name instead of ownerId for the only own playlists filter (this is not very safe e.g. multiple users with the same name)
- remove the total playlist count from search (only an issue when searching during sync which is not a common use case) could add a warning text saying playlist counts are unreliable during sync.

- Update branding (purple?)
- Return home when errors
- Redirect to home when can't find user or spotify client?

- PWA
- AWS Deployment
- IaaC deployment
# Overview

This is a project used to search a Spotify User's saved playlists by their contained tracks title and artist.

The user's playlists are fetched via the Spotify API and saved into a PostgresSQL Database the first time they use the app
and then updated periodically (weekly if the user was active in the last week) by a background job. This helps reduce the calls to Spotify API.

The project uses Cake (C# Make) as a build automation system. Run ./cake at the root for a list of commands.

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


# TODO
- Thoroughly test all endpoints
- Builders for test data?
- Test for pending model changes database vs data model? context.Database.HasPendingModelChanges();
- Create frontend tests (do we need this)
- Prettier setup
- ESLint setup (Job for format and lint frontend)
- Client Gen?
- Better logging for SyncSpotifyPlaylistService
- Create background job for syncing playlists

- Update branding (purple?)
- Return home when errors
- Redirect to home when can't find user or spotify client?
- Clicking Link on Random playlist retriggers the request and loads in a new random playlist
- PWA
- AWS Deployment
- IaaC deployment
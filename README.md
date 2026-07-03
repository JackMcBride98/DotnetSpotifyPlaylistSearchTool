# Overview

This is a project used to search a Spotify User's saved playlists by its contained tracks title and artist.

The user's playlists are fetched via the Spotify API andsaved into a PostgresSQL Database the first time they use the app and they updated periodically by a background job. This helps reduce the calls to Spotify API.

# Project folder structure

`/client` contains the vite, typscript and React frontend

`/SpotifyPlaylistSearchTool.Api` contains the C# .NET, FastEndpoints, EFCore backend API

`/SpotifyPlaylistSearchTool.Database` contains the DbUp console project and docker compose file for creating and migrating the database. 
This is used instead of EFCore migrations, to allow complete control of the generated SQL, allowing more flexibility in for example data migrations. 
There are no down migrations as DbUp is opinionated against them. Although they could be added, there is a package to handle that. See DbUp documentation.

`./build` contains the Cake Frosting project which organises local scripts for the project. Run ./cake in the root directory to see a list of commands
(side note for self) These take a while on my machine, I'm unsure if its the way I've set it up or my computer being not so good.

# Running the app locally
First create the local database by running `./cake CreateLocalDatabase`

Then migrate the local database by running `./cake MigrateLocalDatabase`

Within the `./SpotifyPlaylistSearchTool.Api` folder
Update `appsettings.Development.json` with the Spotify API client ID and secret, these can be found through the Spotify Developer Dashboard.

Then run the backend with `dotnet watch run` and go to `http://localhost:5030/`

Run the frontend by navigating to the `/client` directory and then running `npm run dev`

# Running migrations
Use `./cake MigrateLocalDatabase` to run the latest migrations e.g. if you have added any new ones in development


# TODO
- Create backend tests
- Test for pending model changes database vs data model? context.Database.HasPendingModelChanges();
- Create frontend tests (do we need this)
- Client Gen?
- Better logging for SyncSpotifyPlaylistService
- Create background job for syncing playlists

- Prettier setup
- Update branding (purple?)
- Return home when errors
- Redirect to home when can't find user or spotify client?
- Advise users against refreshing or closing page during sync
- Clicking Link on Random playlist retriggers the request and loads in a new random playlist
- PWA
- AWS Deployment
- IaaC deployment
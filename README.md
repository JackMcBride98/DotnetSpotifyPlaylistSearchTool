# Overview

This is a project used to search a Spotify User's saved playlists by its contained tracks title and artist.

The user's playlists are fetched via the Spotify API andsaved into a PostgresSQL Database the first time they use the app and they updated periodically by a background job. This helps reduce the calls to Spotify API.

# Running the app locally
First create the database using the `Database/docker-compose.yaml` file. Run `docker-compose up -d`

Update `appsettings.Development.json` with the Spotify API client ID and secret, these can be found through the Spotify Developer Dashboard.

Also, add the database connection string which is `"Host=localhost;Port=5433;Database=postgres;Username=postgres;Password=mysecretpassword"`

Now we are using dotnet ef migrations to handle database migrations, so run those against the container with `dotnet ef database update`

Run the frontend by navigating to the `/client` directory and then running `npm run dev`

Then run the backend with `dotnet watch run` and go to `http://localhost:5030/`

# Running migrations
If you make changes to the database models run `dotnet ef migrations add <migration_name>` then `dotnet ef database update`


# TODO
- Make random playlist query more performant
- Search and Random whilst syncing?!
- Create background job for syncing playlists
- Create backend tests
- Create frontend tests
- NUKE?
- DB Up?
- Client Gen?
- Azure Deployment
- AWS Deployment
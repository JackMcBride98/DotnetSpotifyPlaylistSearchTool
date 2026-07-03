-- 1. Create Image Table
CREATE TABLE "Image" (
    "ImageId" INTEGER GENERATED ALWAYS AS IDENTITY,
    "Height" INTEGER NOT NULL,
    "Width" INTEGER NOT NULL,
    "Url" VARCHAR(500) NOT NULL,
    CONSTRAINT "PK_Image" PRIMARY KEY ("ImageId")
);

-- 2. Create Users Table
CREATE TABLE "Users" (
    "UserId" VARCHAR(100) NOT NULL,
    "AccessToken" VARCHAR(500) NULL,
    "RefreshToken" VARCHAR(500) NULL,
    "FirstSyncTotalPlaylists" INTEGER NULL,
    "UpdatedAt" TIMESTAMPTZ NULL, -- Map NodaTime's Instant to 'timestamp with time zone'
    "Username" VARCHAR(5000) NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("UserId")
);

-- 3. Create Playlists Table
CREATE TABLE "Playlists" (
    "PlaylistId" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(5000) NOT NULL,
    "ImageId" INTEGER NULL,
    "Name" VARCHAR(5000) NOT NULL,
    "OwnerName" VARCHAR(5000) NOT NULL,
    "SnapshotId" VARCHAR(100) NOT NULL,
    CONSTRAINT "PK_Playlists" PRIMARY KEY ("PlaylistId"),
    CONSTRAINT "FK_Playlists_Image_ImageId" FOREIGN KEY ("ImageId") REFERENCES "Image" ("ImageId") ON DELETE SET NULL
);

-- 4. Create Tracks Table
CREATE TABLE "Tracks" (
    "TrackId" INTEGER GENERATED ALWAYS AS IDENTITY,
    "ArtistName" VARCHAR(5000) NOT NULL,
    "Index" INTEGER NOT NULL,
    "Name" VARCHAR(5000) NOT NULL,
    "PlaylistId" VARCHAR(100) NOT NULL,
    CONSTRAINT "PK_Tracks" PRIMARY KEY ("TrackId"),
    CONSTRAINT "FK_Tracks_Playlists_PlaylistId" FOREIGN KEY ("PlaylistId") REFERENCES "Playlists" ("PlaylistId") ON DELETE CASCADE
);

-- 5. Create PlaylistUser Join Table (Many-to-Many Relationship)
CREATE TABLE "PlaylistUser" (
    "PlaylistsPlaylistId" VARCHAR(100) NOT NULL,
    "UsersUserId" VARCHAR(100) NOT NULL,
    CONSTRAINT "PK_PlaylistUser" PRIMARY KEY (
        "PlaylistsPlaylistId",
        "UsersUserId"
    ),
    CONSTRAINT "FK_PlaylistUser_Playlists_PlaylistsPlaylistId" FOREIGN KEY ("PlaylistsPlaylistId") REFERENCES "Playlists" ("PlaylistId") ON DELETE CASCADE,
    CONSTRAINT "FK_PlaylistUser_Users_UsersUserId" FOREIGN KEY ("UsersUserId") REFERENCES "Users" ("UserId") ON DELETE CASCADE
);

---
-- Indexes for optimized performance (as defined in the snapshot)

CREATE INDEX "IX_Playlists_ImageId" ON "Playlists" ("ImageId");

CREATE INDEX "IX_Tracks_PlaylistId" ON "Tracks" ("PlaylistId");

CREATE INDEX "IX_PlaylistUser_UsersUserId" ON "PlaylistUser" ("UsersUserId");
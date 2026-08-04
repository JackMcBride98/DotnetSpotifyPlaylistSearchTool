CREATE TYPE sync_status AS ENUM ('NotStarted', 'InProgress', 'Completed', 'Failed');

ALTER TABLE "Users"
    DROP COLUMN "FirstSyncTotalPlaylists",
    DROP COLUMN "UpdatedAt",
    ADD COLUMN "LastActiveAt" TIMESTAMPTZ NULL,
    ADD COLUMN "SyncStatus" sync_status NOT NULL DEFAULT 'NotStarted',
    ADD COLUMN "SyncTotalPlaylists" INTEGER NULL,
    ADD COLUMN "SyncErrorMessage" VARCHAR(500) NULL,
    ADD COLUMN "SyncCompletedAt" TIMESTAMPTZ NULL;
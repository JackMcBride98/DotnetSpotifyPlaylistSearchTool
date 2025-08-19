import { SpinnerCircularFixed } from "spinners-react";
import { SyncProgressResponse } from "../features/Profile.tsx";

type Props = {
  syncProgressData: SyncProgressResponse | undefined;
  isSyncingPlaylists: boolean;
};

export const PlaylistsSyncProgress = ({
  isSyncingPlaylists,
  syncProgressData,
}: Props) => {
  if (!isSyncingPlaylists || !syncProgressData) return null;

  const { totalPlaylists, syncedPlaylists } = syncProgressData;

  return (
    <div className="flex flex-col items-center gap-2 w-full">
      <SpinnerCircularFixed />
      {totalPlaylists ? (
        <p>
          Synced {syncedPlaylists} of {totalPlaylists} saved playlists...
        </p>
      ) : (
        <p>Preparing to sync playlists...</p>
      )}
    </div>
  );
};

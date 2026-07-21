import { SpinnerCircularFixed } from "spinners-react";
import { SyncProgressResponse } from "../api";

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
    <div className="flex w-full flex-col items-center gap-2">
      <SpinnerCircularFixed color={"#7c3aed"} />
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

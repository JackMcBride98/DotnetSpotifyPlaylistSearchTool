import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { motion } from "framer-motion";
import { SpinnerCircularFixed } from "spinners-react";
import { useEffect, useRef, useState } from "react";
import { GetProfileSyncStatusResponse } from "../api";
import {
  getProfileOptions,
  getProfileQueryKey,
  syncPlaylistsMutation,
} from "../api/@tanstack/react-query.gen.ts";
import { client } from "../api/client.gen.ts";
import { LogoutButton } from "../components/LogoutButton";
import { RandomPlaylist } from "../components/RandomPlaylist";
import { SearchPlaylists } from "../components/SearchPlaylists.tsx";
import { formatDate } from "../helpers/dateHelpers.ts";
import { getErrorMessage } from "../helpers/getErrorMessage.ts";
import { UpIcon } from "../icons/UpIcon.tsx";

export const Profile = () => {
  const queryClient = useQueryClient();
  const ref = useRef<HTMLDivElement>(null);

  const [showScrollToTop, setShowScrollToTop] = useState(false);
  const [showOnlyOwnPlaylists, setShowOnlyOwnPlaylists] = useState(false);
  const [isSyncing, setIsSyncing] = useState(false);

  const { isLoading, isError, error, isSuccess, data } = useQuery({
    ...getProfileOptions({ client }),
    refetchInterval: (query) => {
      const status = query.state.data?.syncStatus.status;

      if (status === "Completed" || status === "Failed") {
        return false;
      }

      return isSyncing || status === "InProgress" ? 1000 : false;
    },
    refetchIntervalInBackground: true,
  });

  const {
    mutate: syncPlaylists,
    error: syncError,
    isError: isSyncError,
  } = useMutation({
    ...syncPlaylistsMutation(),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: getProfileQueryKey() });
    },
    onError: () => {
      setIsSyncing(false);
    },
  });

  if (
    (isSyncing && data?.syncStatus.status === "Completed") ||
    data?.syncStatus.status === "Failed"
  ) {
    setIsSyncing(false);
  }

  useEffect(() => {
    const handleScroll = () => {
      setShowScrollToTop(window.scrollY > 500);
    };

    document.addEventListener("scroll", handleScroll);
    return () => {
      document.removeEventListener("scroll", handleScroll);
    };
  }, []);

  const handleSync = () => {
    setIsSyncing(true);
    syncPlaylists({});
  };

  if (isLoading) {
    return (
      <div className="flex h-full min-h-screen w-full min-w-screen flex-col items-center space-y-4 bg-black text-white">
        <SpinnerCircularFixed color="#7c3aed" />
      </div>
    );
  }

  if (isError || !isSuccess) {
    return (
      <div className="flex h-full min-h-screen w-full min-w-screen flex-col items-center space-y-4 bg-black text-white">
        <p className="text-red-600">Error: {getErrorMessage(error)}</p>{" "}
        <a href="/" className="text-violet-600 hover:underline">
          Go back to the home page
        </a>
      </div>
    );
  }

  const { user, syncStatus } = data;

  return (
    <div
      ref={ref}
      className="flex h-full min-h-screen w-full min-w-screen flex-col items-center space-y-4 overflow-y-auto bg-black pb-8 text-white"
    >
      <h1 className="text-xl font-bold text-violet-600 md:text-3xl">
        Playlist Search Tool
      </h1>

      <p>Hello {user.displayName.split(" ")[0]}</p>
      <img
        className="rounded-full"
        src={user.profileImageUrl || undefined}
        width={150}
        height={150}
        alt="User's spotify profile"
      />

      <ProfileSyncStatusView
        syncStatus={syncStatus}
        userTotalPlaylists={user.totalPlaylists}
        showOnlyOwnPlaylists={showOnlyOwnPlaylists}
        setShowOnlyOwnPlaylists={setShowOnlyOwnPlaylists}
        isSyncing={isSyncing}
        isSyncError={isSyncError}
        syncError={syncError}
        handleSync={handleSync}
      />

      <LogoutButton />
      <motion.button
        onClick={() => window.scrollTo({ top: 0, behavior: "smooth" })}
        whileHover={{ scale: 1.1 }}
        whileTap={{ scale: 0.9 }}
        className={
          "fixed right-2 bottom-2 h-12 w-12 rounded-full bg-violet-600 text-center text-xl text-black opacity-80 transition-all hover:opacity-100 focus:opacity-100 md:right-28 md:bottom-4 md:h-20 md:w-20 " +
          (!showScrollToTop && "hidden")
        }
      >
        <UpIcon />
      </motion.button>
    </div>
  );
};

type ProfileSyncStatusViewProps = {
  syncStatus: GetProfileSyncStatusResponse;
  userTotalPlaylists: number;
  showOnlyOwnPlaylists: boolean;
  setShowOnlyOwnPlaylists: (value: boolean) => void;
  isSyncing: boolean;
  isSyncError: boolean;
  syncError: unknown;
  handleSync: () => void;
};

const ProfileSyncStatusView = ({
  syncStatus,
  userTotalPlaylists,
  showOnlyOwnPlaylists,
  setShowOnlyOwnPlaylists,
  isSyncing,
  isSyncError,
  syncError,
  handleSync,
}: ProfileSyncStatusViewProps) => {
  switch (syncStatus.status) {
    case "NotStarted":
      return (
        <SyncNotStarted
          isSyncing={isSyncing}
          isSyncError={isSyncError}
          syncError={syncError}
          handleSync={handleSync}
        />
      );

    case "InProgress":
      return (
        <SyncInProgress
          totalPlaylists={syncStatus.totalPlaylists}
          syncedPlaylists={userTotalPlaylists}
          showOnlyOwnPlaylists={showOnlyOwnPlaylists}
          setShowOnlyOwnPlaylists={setShowOnlyOwnPlaylists}
        />
      );

    case "Completed":
      return (
        <SyncCompleted
          totalPlaylists={userTotalPlaylists}
          completedAt={syncStatus.completedAt}
          showOnlyOwnPlaylists={showOnlyOwnPlaylists}
          setShowOnlyOwnPlaylists={setShowOnlyOwnPlaylists}
        />
      );

    case "Failed":
      return <SyncFailed errorMessage={syncStatus.errorMessage} />;

    default:
      return null;
  }
};

type SyncNotStartedProps = {
  isSyncing: boolean;
  isSyncError: boolean;
  syncError: unknown;
  handleSync: () => void;
};

const SyncNotStarted = ({
  isSyncing,
  isSyncError,
  syncError,
  handleSync,
}: SyncNotStartedProps) => {
  return (
    <div className="flex w-full flex-col items-center gap-2">
      <motion.button
        whileHover={isSyncing ? {} : { scale: 1.1 }}
        whileTap={isSyncing ? {} : { scale: 0.9 }}
        className="flex items-center space-x-2 rounded-full bg-violet-600 p-4 text-center disabled:cursor-not-allowed disabled:opacity-50"
        onClick={handleSync}
        disabled={isSyncing}
      >
        {isSyncing ? "Starting sync..." : "Sync playlists"}
      </motion.button>
      {isSyncing && <SpinnerCircularFixed color="#7c3aed" />}
      {isSyncError && (
        <p className="text-red-600">Error: {getErrorMessage(syncError)}</p>
      )}
    </div>
  );
};

type SyncInProgressProps = {
  totalPlaylists?: number | null;
  syncedPlaylists: number;
  showOnlyOwnPlaylists: boolean;
  setShowOnlyOwnPlaylists: (value: boolean) => void;
};

const SyncInProgress = ({
  totalPlaylists,
  syncedPlaylists,
  showOnlyOwnPlaylists,
  setShowOnlyOwnPlaylists,
}: SyncInProgressProps) => {
  return (
    <div className="flex w-full flex-col items-center gap-2">
      <SpinnerCircularFixed color="#7c3aed" />
      {totalPlaylists ? (
        <p>
          Synced {syncedPlaylists} of {totalPlaylists} saved playlists...
        </p>
      ) : (
        <p>Preparing to sync playlists...</p>
      )}
      <RandomPlaylist showOnlyOwnPlaylists={showOnlyOwnPlaylists} />
      <SearchPlaylists
        totalPlaylists={totalPlaylists ?? 0}
        showOnlyOwnPlaylists={showOnlyOwnPlaylists}
        setShowOnlyOwnPlaylists={setShowOnlyOwnPlaylists}
      />
    </div>
  );
};

type SyncCompletedProps = {
  totalPlaylists: number;
  completedAt?: string | null;
  showOnlyOwnPlaylists: boolean;
  setShowOnlyOwnPlaylists: (value: boolean) => void;
};

const SyncCompleted = ({
  totalPlaylists,
  completedAt,
  showOnlyOwnPlaylists,
  setShowOnlyOwnPlaylists,
}: SyncCompletedProps) => {
  return (
    <div className="flex w-full flex-col items-center gap-4">
      <div className="flex flex-col items-center space-x-1">
        <p>You have {totalPlaylists} playlists saved</p>
        <p>Last updated: {completedAt ? formatDate(completedAt) : "never"}</p>
      </div>
      <RandomPlaylist showOnlyOwnPlaylists={showOnlyOwnPlaylists} />
      <SearchPlaylists
        totalPlaylists={totalPlaylists}
        showOnlyOwnPlaylists={showOnlyOwnPlaylists}
        setShowOnlyOwnPlaylists={setShowOnlyOwnPlaylists}
      />
    </div>
  );
};

type SyncFailedProps = {
  errorMessage?: string | null;
};

const SyncFailed = ({ errorMessage }: SyncFailedProps) => {
  return (
    <div className="flex flex-col items-center gap-2">
      <p className="text-red-600">
        Syncing playlists failed
        {errorMessage ? `: ${errorMessage}` : "."}
      </p>
      <p className="text-sm text-gray-400">
        This is unescapable for now RIP, contact Jack
      </p>
    </div>
  );
};

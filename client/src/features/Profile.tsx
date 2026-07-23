import { useQuery } from "@tanstack/react-query";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { motion } from "framer-motion";
import { SpinnerCircularFixed } from "spinners-react";
import { useEffect, useRef, useState } from "react";
import {
  getProfileOptions,
  getProfileQueryKey,
  syncPlaylistsMutation,
  syncProgressOptions,
} from "../api/@tanstack/react-query.gen.ts";
import { client } from "../api/client.gen.ts";
import { LogoutButton } from "../components/LogoutButton";
import { PlaylistsSyncProgress } from "../components/PlaylistsSyncProgress.tsx";
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

  const { isLoading, isError, error, isSuccess, data } = useQuery({
    ...getProfileOptions({ client }),
  });

  // useEffect(() => {
  //   if (isError) {
  //     window.location.href = "/";
  //   }
  // }, [isError]);

  const {
    isPending: isSyncingPlaylists,
    mutate: syncPlaylists,
    error: syncError,
    isError: isSyncError,
  } = useMutation({
    ...syncPlaylistsMutation(),
    onSettled: () =>
      queryClient.invalidateQueries({
        queryKey: getProfileQueryKey({ client }),
      }),
  });

  const { data: syncProgressData } = useQuery({
    ...syncProgressOptions({ client }),
    enabled: isSyncingPlaylists,
    refetchInterval: 500,
    staleTime: 500,
  });

  useEffect(() => {
    const handleScroll = () => {
      if (window.scrollY > 500) {
        setShowScrollToTop(true);
      } else {
        setShowScrollToTop(false);
      }
    };

    document.addEventListener("scroll", handleScroll);

    return () => {
      document.removeEventListener("scroll", handleScroll);
    };
  }, []);

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
      </div>
    );
  }

  const { user, totalPlaylists, lastSyncedAt } = data;

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

      {lastSyncedAt == null && !isSyncingPlaylists ? (
        <div className="flex w-full flex-col items-center gap-2">
          <motion.button
            whileHover={{ scale: 1.1 }}
            whileTap={{ scale: 0.9 }}
            className="flex items-center space-x-2 rounded-full bg-violet-600 p-4 text-center"
            onClick={() => syncPlaylists({})}
            disabled={isSyncingPlaylists}
          >
            Sync playlists
          </motion.button>
          {isSyncError && (
            <p className="text-red-600">Error: {syncError?.message}</p>
          )}
        </div>
      ) : (
        <div className="flex w-full flex-col items-center gap-4">
          {isSyncingPlaylists ? (
            <PlaylistsSyncProgress
              syncProgressData={syncProgressData}
              isSyncingPlaylists={isSyncingPlaylists}
            />
          ) : (
            <div className="flex flex-col items-center space-x-1">
              <p>You have {totalPlaylists} playlists saved</p>
              <p>
                Last updated:{" "}
                {lastSyncedAt ? formatDate(lastSyncedAt) : "never"}
              </p>
            </div>
          )}
          <RandomPlaylist showOnlyOwnPlaylists={showOnlyOwnPlaylists} />
          <SearchPlaylists
            totalPlaylists={
              isSyncingPlaylists &&
              syncProgressData &&
              syncProgressData?.syncedPlaylists
                ? syncProgressData.syncedPlaylists
                : totalPlaylists
            }
            showOnlyOwnPlaylists={showOnlyOwnPlaylists}
            setShowOnlyOwnPlaylists={setShowOnlyOwnPlaylists}
          />
        </div>
      )}

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

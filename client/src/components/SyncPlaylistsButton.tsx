import { useMutation, useQueryClient } from "@tanstack/react-query";
import { motion } from "framer-motion";
import { SpinnerCircularFixed } from "spinners-react";

export const SyncPlaylistsButton = () => {
  const queryClient = useQueryClient();
  const { isPending, mutate, error, isError } = useMutation({
    mutationKey: ["syncPlaylists"],
    mutationFn: async () => {
      const res = await fetch("/api/sync-playlists", { method: "POST" });
      if (!res.ok) {
        const errorMessage = await res.text();
        throw new Error(`HTTP Error ${errorMessage}`);
      }
      await queryClient.invalidateQueries({ queryKey: ["profile"] });
    },
  });
  if (isPending) {
    return (
      <>
        <SpinnerCircularFixed />
        <p>This may take a while</p>
      </>
    );
  }
  return (
    <>
      <motion.button
        whileHover={{ scale: 1.1 }}
        whileTap={{ scale: 0.9 }}
        className="text-center p-4 rounded-full bg-green-600 flex space-x-2 items-center "
        onClick={() => mutate()}
        disabled={isPending}
      >
        Sync playlists
      </motion.button>
      {isError && <p className="text-red-600">Error: {error?.message}</p>}
    </>
  );
};

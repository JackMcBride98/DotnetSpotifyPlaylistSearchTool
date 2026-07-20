import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { motion } from "framer-motion";
import { SpinnerCircularFixed } from "spinners-react";
import { Playlist } from "./SearchResults";
import { getRandomPlaylistOptions } from "../api/@tanstack/react-query.gen.ts";
import { client } from "../api/client.gen.ts";

interface RandomPlaylistProps {
  showOnlyOwnPlaylists: boolean;
}

export const RandomPlaylist = ({
  showOnlyOwnPlaylists,
}: RandomPlaylistProps) => {
  const queryClient = useQueryClient();
  const [visible, setVisible] = useState(false);
  const { isLoading, isError, error, data, refetch, isRefetching } = useQuery({
    ...getRandomPlaylistOptions({
      client,
      query: { onlyOwnPlaylists: showOnlyOwnPlaylists },
    }),
    enabled: visible,
    refetchOnWindowFocus: false,
  });

  if (!visible) {
    return (
      <motion.button
        whileHover={{ scale: 1.1 }}
        whileTap={{ scale: 0.9 }}
        className="text-center p-4 rounded-full bg-green-600 flex space-x-2 items-center"
        onClick={() => {
          void refetch();
          setVisible(true);
        }}
        disabled={isLoading || isRefetching}
      >
        Get random playlist
      </motion.button>
    );
  }

  return (
    <>
      <motion.button
        whileHover={{ scale: 1.1 }}
        whileTap={{ scale: 0.9 }}
        className="text-center p-4 rounded-full bg-green-600 flex space-x-2 items-center"
        onClick={() => refetch()}
        disabled={isLoading || isRefetching}
      >
        Get random playlist
      </motion.button>
      {(isLoading || isRefetching) && <SpinnerCircularFixed />}
      {isError && <p className="text-red-600">Error: {error?.message}</p>}
      {data && data.randomPlaylist && (
        <Playlist
          playlist={data.randomPlaylist}
          onClose={() => {
            setVisible(false);
            queryClient.removeQueries({ queryKey: ["getRandomPlaylist"] });
          }}
        />
      )}
    </>
  );
};

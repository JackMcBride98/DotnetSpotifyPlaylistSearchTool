import { useQuery, useQueryClient } from "@tanstack/react-query";
import { motion } from "framer-motion";
import { SpinnerCircularFixed } from "spinners-react";
import { useState } from "react";
import { getRandomPlaylistOptions } from "../api/@tanstack/react-query.gen.ts";
import { client } from "../api/client.gen.ts";
import { getErrorMessage } from "../helpers/getErrorMessage.ts";
import { Playlist } from "./SearchResults";

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
        className="flex items-center space-x-2 rounded-full bg-violet-600 p-4 text-center"
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
        className="flex items-center space-x-2 rounded-full bg-violet-600 p-4 text-center"
        onClick={() => refetch()}
        disabled={isLoading || isRefetching}
      >
        Get random playlist
      </motion.button>
      {(isLoading || isRefetching) && (
        <SpinnerCircularFixed color={"#7c3aed"} />
      )}
      {isError && (
        <p className="text-red-600">Error: {getErrorMessage(error)}</p>
      )}
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

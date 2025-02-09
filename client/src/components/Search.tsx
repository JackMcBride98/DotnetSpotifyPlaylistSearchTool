import { motion } from "framer-motion";
import { useState } from "react";
import searchIcon from "../assets/search.svg";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { SpinnerCircularFixed } from "spinners-react";
import { Playlist, SearchResults } from "./SearchResults.tsx";

type Response = {
  matchingPlaylists: PlaylistResponse[];
};

type RandomPlaylistResponse = {
  randomPlaylist: PlaylistResponse;
};

export type PlaylistResponse = {
  id: string;
  name: string;
  description: string;
  ownerName: string;
  image: ImageResponse;
  tracks: TrackResponse[];
};

export type TrackResponse = {
  name: string;
  artistName: string;
  match: boolean;
};

type ImageResponse = {
  url: string;
};

type Props = {
  totalPlaylists: number;
};

export const Search = ({ totalPlaylists }: Props) => {
  const queryClient = useQueryClient();
  const [searchTerm, setSearchTerm] = useState("");
  const [intermediateSearchTerm, setIntermediateSearchTerm] = useState("");
  const [showOnlyOwnPlaylists, setShowOnlyOwnPlaylists] = useState(false);

  const { isLoading, isError, error, data } = useQuery<Response, Error>({
    queryKey: ["search", searchTerm, showOnlyOwnPlaylists],
    queryFn: async () => {
      if (!searchTerm) {
        return null;
      }
      const res = await fetch(
        `/api/search-playlists?searchTerm=${searchTerm}&showOnlyOwnPlaylists=${showOnlyOwnPlaylists}`
      );
      if (!res.ok) {
        const errorMessage = await res.text();
        throw new Error(`Error: ${errorMessage}`);
      }
      return res.json();
    },
  });

  const {
    isLoading: isRandomPlaylistLoading,
    isError: isRandomPlaylistError,
    error: randomPlaylistError,
    data: randomPlaylistData,
    isRefetching: isRandomPlaylistRefetching,
    refetch: refetchRandomPlaylist,
  } = useQuery<RandomPlaylistResponse, Error>({
    queryKey: ["getRandomPlaylist"],
    queryFn: async () => {
      const res = await fetch(
        `/api/random-playlist?onlyOwnPlaylists=${showOnlyOwnPlaylists}`
      );
      if (!res.ok) {
        const errorMessage = await res.text();
        throw new Error(`Error: ${errorMessage}`);
      }

      return res.json();
    },
    enabled: false,
  });

  const handleSearch = () => {
    queryClient.removeQueries({ queryKey: ["getRandomPlaylist"] });
    setSearchTerm(intermediateSearchTerm);
  };

  return (
    <>
      <motion.button
        whileHover={{ scale: 1.1 }}
        whileTap={{ scale: 0.9 }}
        className="text-center p-4 rounded-full bg-green-600 flex space-x-2 items-center "
        onClick={() => refetchRandomPlaylist()}
        disabled={isRandomPlaylistLoading}
      >
        Get random playlist
      </motion.button>
      <motion.div
        whileTap={{ scale: 0.9 }}
        className="md:w-80 w-72 flex items-center bg-green-600 rounded-md pl-1"
      >
        <input
          className="p-2 outline-0 bg-black w-full overflow-visible"
          type="text"
          placeholder="Search for songs or artists"
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              void handleSearch();
            }
          }}
          value={intermediateSearchTerm}
          onChange={(e) => setIntermediateSearchTerm(e.target.value)}
        />
        <button
          disabled={isLoading}
          className="p-4 disabled:cursor-not-allowed"
          onClick={handleSearch}
        >
          <img
            src={searchIcon}
            alt="search"
            className="w-6 h-6 fill-black bg-green-600"
          />
        </button>
      </motion.div>
      <label className="flex items-center">
        Show only your own playlists
        <input
          type="checkbox"
          className="accent-green-600 w-4 h-4 ml-2"
          value={showOnlyOwnPlaylists.toString()}
          onChange={() => {
            setShowOnlyOwnPlaylists(!showOnlyOwnPlaylists);
          }}
        />
      </label>
      {(isLoading || isRandomPlaylistLoading || isRandomPlaylistRefetching) && (
        <SpinnerCircularFixed />
      )}
      {isError && <p className="text-red-600">Error: {error?.message}</p>}
      {isRandomPlaylistError && (
        <p className="text-red-600">Error: {randomPlaylistError?.message}</p>
      )}
      {!(isRandomPlaylistLoading || isRandomPlaylistRefetching || isLoading) &&
      randomPlaylistData ? (
        <Playlist playlist={randomPlaylistData.randomPlaylist} />
      ) : (
        data && (
          <SearchResults
            playlists={data.matchingPlaylists}
            totalPlaylists={totalPlaylists}
          />
        )
      )}
    </>
  );
};

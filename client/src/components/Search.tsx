import { motion } from "framer-motion";
import { useState } from "react";
import searchIcon from "../assets/search.svg";
import { useQuery } from "@tanstack/react-query";
import { SpinnerCircularFixed } from "spinners-react";
import { SearchResults } from "./SearchResults.tsx";

type Response = {
  matchingPlaylists: PlaylistResponse[];
};

export type PlaylistResponse = {
  name: string;
  ownerName: string;
  image: ImageResponse;
  tracks: TrackResponse[];
};

type TrackResponse = {
  name: string;
  artistName: string;
  match: boolean;
};

type ImageResponse = {
  url: string;
};

export const Search = () => {
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

  const handleSearch = () => {
    setSearchTerm(intermediateSearchTerm);
  };

  return (
    <>
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
      {isLoading && <SpinnerCircularFixed />}
      {isError && <p className="text-red-600">Error: {error?.message}</p>}
      {data && <SearchResults playlists={data.matchingPlaylists} />}
    </>
  );
};

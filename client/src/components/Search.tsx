import { motion } from "framer-motion";
import { useState } from "react";
import searchIcon from "../assets/search.svg";
import { useQuery } from "@tanstack/react-query";
import { SpinnerCircularFixed } from "spinners-react";

type Response = {
  MatchingPlaylists: PlaylistResponse[];
};

type PlaylistResponse = {
  Name: string;
  OwnerName: string;
  Image: ImageResponse;
  Tracks: TrackResponse[];
};

type TrackResponse = {
  Name: string;
  ArtistName: string;
  Match: boolean;
};

type ImageResponse = {
  Url: string;
};

export const Search = () => {
  const [searchTerm, setSearchTerm] = useState("");
  const [showOnlyOwnPlaylists, setShowOnlyOwnPlaylists] = useState(false);

  const { refetch, isLoading, isError, error } = useQuery<Response, Error>({
    queryKey: ["search", searchTerm, showOnlyOwnPlaylists],
    queryFn: async () => {
      const res = await fetch(
        `/api/search-playlists?searchTerm=${searchTerm}&showOnlyOwnPlaylists=${showOnlyOwnPlaylists}`
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
    if (searchTerm) {
      void refetch();
    }
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
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
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
    </>
  );
};

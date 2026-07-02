import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { SpinnerCircularFixed } from "spinners-react";
import { SearchResults } from "./SearchResults.tsx";
import { SearchBar } from "./SearchBar.tsx";

type Response = {
  matchingPlaylists: PlaylistResponse[];
  totalPlaylists: number;
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
  showOnlyOwnPlaylists: boolean;
  setShowOnlyOwnPlaylists: (val: boolean) => void;
};

export const SearchPlaylists = ({
  showOnlyOwnPlaylists,
  setShowOnlyOwnPlaylists,
}: Props) => {
  const [searchTerm, setSearchTerm] = useState("");

  const { isLoading, isError, error, data } = useQuery<Response, Error>({
    queryKey: ["search", searchTerm, showOnlyOwnPlaylists],
    queryFn: async () => {
      if (!searchTerm) {
        return null;
      }
      const res = await fetch(
        `/api/search-playlists?searchTerm=${searchTerm}&showOnlyOwnPlaylists=${showOnlyOwnPlaylists}`,
      );
      if (!res.ok) {
        const errorMessage = await res.text();
        throw new Error(`Error: ${errorMessage}`);
      }
      return res.json();
    },
  });

  return (
    <div className="flex flex-col items-center w-full">
      <SearchBar onSearch={setSearchTerm} isLoading={isLoading} />
      <label className="flex items-center mt-2">
        Show only your own playlists
        <input
          type="checkbox"
          className="accent-green-600 w-4 h-4 ml-2"
          checked={showOnlyOwnPlaylists}
          onChange={() => setShowOnlyOwnPlaylists(!showOnlyOwnPlaylists)}
        />
      </label>
      {isLoading && <SpinnerCircularFixed />}
      {isError && <p className="text-red-600">Error: {error?.message}</p>}
      {data && data.matchingPlaylists && (
        <SearchResults
          playlists={data.matchingPlaylists}
          totalPlaylists={data.totalPlaylists}
        />
      )}
    </div>
  );
};

import { useQuery } from "@tanstack/react-query";
import { SpinnerCircularFixed } from "spinners-react";
import { useState } from "react";
import { searchPlaylistsOptions } from "../api/@tanstack/react-query.gen.ts";
import { client } from "../api/client.gen.ts";
import { SearchBar } from "./SearchBar.tsx";
import { SearchResults } from "./SearchResults.tsx";

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

  const { isLoading, isError, error, data } = useQuery({
    ...searchPlaylistsOptions({
      client,
      query: { searchTerm, showOnlyOwnPlaylists },
    }),
    enabled: searchTerm != "",
  });

  return (
    <div className="flex w-full flex-col items-center">
      <SearchBar onSearch={setSearchTerm} isLoading={isLoading} />
      <label className="mt-2 flex items-center">
        Show only your own playlists
        <input
          type="checkbox"
          className="ml-2 h-4 w-4 accent-violet-600"
          checked={showOnlyOwnPlaylists}
          onChange={() => setShowOnlyOwnPlaylists(!showOnlyOwnPlaylists)}
        />
      </label>
      {isLoading && <SpinnerCircularFixed color={"#7c3aed"} />}
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

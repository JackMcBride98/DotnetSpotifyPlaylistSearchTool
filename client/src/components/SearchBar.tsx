import { useState } from "react";
import searchIcon from "../assets/search.svg";

interface SearchBarProps {
  onSearch: (searchTerm: string) => void;
  isLoading: boolean;
}

export const SearchBar = ({ onSearch, isLoading }: SearchBarProps) => {
  const [intermediateSearchTerm, setIntermediateSearchTerm] = useState("");

  const handleSearch = () => {
    onSearch(intermediateSearchTerm);
  };

  return (
    <>
      <div className="flex w-72 items-center rounded-md bg-violet-600 pl-1 md:w-80">
        <input
          className="w-full overflow-visible bg-black p-2 outline-0"
          type="text"
          placeholder="Search for songs or artists"
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              handleSearch();
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
            className="h-6 w-6 bg-violet-600 fill-black"
          />
        </button>
      </div>
    </>
  );
};

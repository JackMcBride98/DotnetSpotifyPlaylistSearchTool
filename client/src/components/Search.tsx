import { motion } from "framer-motion";
import { useState } from "react";
import searchIcon from "../assets/search.svg";
export const Search = () => {
  const [searchTerm, setSearchTerm] = useState("");
  const [showOnlyOwnPlaylists, setShowOnlyOwnPlaylists] = useState(false);

  return (
    <>
      <motion.div
        whileTap={{ scale: 0.9 }}
        className="md:w-80 w-72 flex items-center space-x-2 bg-green-600 rounded-md pr-2 p-1"
      >
        <input
          className="p-2 outline-0 bg-black w-full overflow-visible"
          type="text"
          placeholder="Search for songs or artists"
          // onKeyDown={(e) => {
          //   if (e.key === "Enter") {
          //     search(searchTerm, showOwn);
          //   }
          // }}
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
        />
        <button
          className=" p-2"
          // onClick={() => {
          //   search(searchTerm, showOwn);
          // }}
        >
          <img
            src={searchIcon}
            alt="search"
            className="w-5 h-5 fill-black bg-green-600"
          />
        </button>
      </motion.div>
      <label className="flex items-center">
        Show only your made playlists
        <input
          type="checkbox"
          className="accent-green-600 w-4 h-4 ml-2"
          value={showOnlyOwnPlaylists.toString()}
          onChange={() => {
            setShowOnlyOwnPlaylists(!showOnlyOwnPlaylists);
          }}
        />
      </label>
    </>
  );
};

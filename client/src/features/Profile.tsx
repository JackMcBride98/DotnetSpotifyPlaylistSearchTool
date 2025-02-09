import { useQuery } from "@tanstack/react-query";
import { SyncPlaylistsButton } from "../components/SyncPlaylistsButton.tsx";
import { SpinnerCircularFixed } from "spinners-react";
import { Search } from "../components/Search.tsx";
import { motion } from "framer-motion";
import { useRef, useEffect, useState } from "react";
import { formatDate } from "../helpers/dateHelpers.ts";

type User = {
  country: string;
  displayName: string;
  email: string;
  externalUrls: Record<string, string>;
  followers: {
    href: string;
    total: number;
  };
  href: string;
  id: string;
  images: {
    height: number;
    url: string;
    width: number;
  }[];
  product: string;
  type: string;
  uri: string;
};

type UserResponse = {
  user: User;
  totalPlaylists: number;
  lastSyncedAt?: string;
};

export const Profile = () => {
  const ref = useRef<HTMLDivElement>(null);
  const [showScrollToTop, setShowScrollToTop] = useState(false);
  const { isLoading, isError, error, isSuccess, data } = useQuery<
    UserResponse,
    Error
  >({
    queryKey: ["profile"],
    queryFn: async () => {
      const res = await fetch("/api/profile");
      if (!res.ok) {
        const errorMessage = await res.text();
        throw new Error(`HTTP Error ${res.status}: ${errorMessage}`);
      }

      return res.json();
    },
  });

  useEffect(() => {
    document.addEventListener("scroll", () => {
      if (window.scrollY > 500) {
        setShowScrollToTop(true);
      } else {
        setShowScrollToTop(false);
      }
    });
  }, []);

  if (isLoading) {
    return (
      <div className="w-full min-w-screen h-full min-h-screen flex flex-col items-center space-y-4 bg-black text-white">
        <SpinnerCircularFixed />
      </div>
    );
  }

  if (isError || !isSuccess) {
    return (
      <div className="w-full min-w-screen h-full min-h-screen flex flex-col items-center space-y-4 bg-black text-white">
        <p className="text-red-600">Error: {error?.message}</p>{" "}
      </div>
    );
  }

  const { user, totalPlaylists, lastSyncedAt } = data;

  return (
    <div
      ref={ref}
      className="w-full min-w-screen h-full min-h-screen flex flex-col items-center space-y-4 bg-black text-white overflow-y-auto pb-8"
    >
      <h1 className="font-bold text-xl md:text-3xl">
        Spotify Playlist Search Tool
      </h1>

      <p>Hello {user.displayName.split(" ")[0]}</p>
      <img
        className="rounded-full"
        src={user.images[0]?.url}
        width={150}
        height={150}
        alt="User's spotify profile"
      />
      <div className="flex flex-col items-center space-x-1">
        <p>You have {totalPlaylists} playlists saved</p>
        <p>Last updated: {lastSyncedAt ? formatDate(lastSyncedAt) : "never"}</p>
      </div>
      {totalPlaylists <= 0 ? (
        <SyncPlaylistsButton />
      ) : (
        <Search totalPlaylists={totalPlaylists} />
      )}
      <motion.button
        whileHover={{ scale: 1.1 }}
        whileTap={{ scale: 0.9 }}
        className="text-center p-4 rounded-full bg-green-600 flex space-x-2 items-center "
      >
        Logout
      </motion.button>
      <motion.button
        onClick={() => window.scrollTo({ top: 0, behavior: "smooth" })}
        whileHover={{ scale: 1.1 }}
        whileTap={{ scale: 0.9 }}
        className={
          "rounded-full text-black text-xl bg-green-600 w-12 h-12 md:w-20 md:h-20  text-center fixed transition-all bottom-2 md:bottom-4 right-2 md:right-28 opacity-80 hover:opacity-100 focus:opacity-100 " +
          (!showScrollToTop && "hidden")
        }
      >
        <svg className={"w-12 h-12 md:w-20 md:h-20"} viewBox="0 0 100 100">
          <polygon
            points="47,75 47,50 38,50 50,28 62,50 53,50 53,75"
            fill="black"
            stroke="black"
          />
        </svg>
      </motion.button>
    </div>
  );
};

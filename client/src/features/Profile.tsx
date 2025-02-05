import { useQuery } from "@tanstack/react-query";
import { SyncPlaylistsButton } from "../components/SyncPlaylistsButton.tsx";
import { SpinnerCircularFixed } from "spinners-react";
import { Search } from "../components/Search.tsx";

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

type UserResponse = { user: User; totalPlaylists: number };

export const Profile = () => {
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

  const { user, totalPlaylists } = data;

  return (
    <div className="w-full min-w-screen h-full min-h-screen flex flex-col items-center space-y-4 bg-black text-white">
      <h1 className="font-bold text-2xl md:text-3xl">
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
      <p>You have {totalPlaylists} total playlists saved</p>
      {totalPlaylists <= 0 ? <SyncPlaylistsButton /> : <Search />}
    </div>
  );
};

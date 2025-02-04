import { Credits } from "../components/Credits.tsx";
import { LoginButton } from "../components/LoginButton.tsx";

export const Home = () => {
  return (
    <div className="w-screen h-screen flex flex-col items-center space-y-8 bg-black text-white">
      <h1 className="font-bold text-3xl">Spotify Playlist Search Tool</h1>
      <p className="w-72 md:w-80 pb-5">
        A tool for finding out which of your saved playlists contain a given
        artist or song. Search your saved spotify playlists by artist or song.
      </p>
      <LoginButton />
      <Credits />
    </div>
  );
};

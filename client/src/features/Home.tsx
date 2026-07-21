import magnifyingGlass from "../assets/magnifying-glass-solid.svg";
import { Credits } from "../components/Credits.tsx";
import { LoginButton } from "../components/LoginButton.tsx";

export const Home = () => {
  return (
    <div className="flex h-screen w-screen flex-col items-center space-y-16 bg-black text-white">
      <h1 className="text-xl font-bold text-violet-600 md:text-3xl">
        Playlist Search Tool
      </h1>
      <p className="w-72 pb-5 md:w-80">
        A tool for finding out which of your owned Spotify playlists contain a
        given artist or song. Search your owned spotify playlists by artist or
        song.
      </p>
      <img src={magnifyingGlass} alt="Magnifying Glass" className="h-40 w-40" />
      <LoginButton />
      <Credits />
    </div>
  );
};

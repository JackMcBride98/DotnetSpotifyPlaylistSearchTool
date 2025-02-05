import { PlaylistResponse } from "./Search.tsx";

type Props = {
  playlists: PlaylistResponse[];
};

export const SearchResults = ({ playlists }: Props) => {
  return (
    <div>
      {playlists.map((playlist) => (
        <Playlist playlist={playlist} />
      ))}
    </div>
  );
};

type PlaylistProps = {
  playlist: PlaylistResponse;
};

const Playlist = ({ playlist }: PlaylistProps) => {
  return (
    <div className="m-2 my-6 md:my-8 p-2 md:w-full md:max-w-md justify-start w-72 border-gray-600 border-2 rounded-md mx-auto">
      <div className="flex space-x-4">
        <img className="h-20 w-20" src={playlist.image.url} alt="playlist" />
        <div className="w-full">
          <h1 className="text-lg">{playlist.name}</h1>
          <p className="text-sm">{playlist.ownerName}</p>
          <a
            // href={playlist.}
            rel="noreferrer"
            target="_blank"
            className="text-base text-green-300 hover:italic"
          >
            Link
          </a>
          <p className="text-xs break-words w-44 md:w-[19rem] font-light text-slate-200 mb-2">
            Placeholder for description
          </p>
        </div>
      </div>
    </div>
  );
};

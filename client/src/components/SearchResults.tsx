import { PlaylistResponse, TrackResponse } from "./Search.tsx";
import { useVirtualizer } from "@tanstack/react-virtual";
import { useLayoutEffect, useRef } from "react";
type Props = {
  playlists: PlaylistResponse[];
  totalPlaylists: number;
};

export const SearchResults = ({ playlists, totalPlaylists }: Props) => {
  const parentRef = useRef<HTMLDivElement>(null);

  const virtualizer = useVirtualizer({
    count: playlists.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => 600,
  });

  const items = virtualizer.getVirtualItems();

  return (
    <>
      <p>
        Showing {items.length} matching out of {totalPlaylists} total playlists
      </p>
      <div
        ref={parentRef}
        className="flex-grow"
        style={{
          height: virtualizer.getTotalSize() + (items.length > 0 ? 0 : 100),
          width: "100%",
          contain: "strict",
        }}
      >
        <div
          style={{
            width: "100%",
            position: "relative",
          }}
        >
          <div
            style={{
              position: "absolute",
              top: 0,
              left: 0,
              height: "100%",
              width: "100%",
              transform: `translateY(${items[0]?.start ?? 0}px)`,
            }}
          >
            {items.map((virtualRow) => (
              <div
                key={virtualRow.key}
                data-index={virtualRow.index}
                ref={virtualizer.measureElement}
                className={
                  virtualRow.index % 2 ? "ListItemOdd" : "ListItemEven"
                }
              >
                <Playlist playlist={playlists[virtualRow.index]} />
              </div>
            ))}
            {items.length === 0 && (
              <p className="text-center">No results found</p>
            )}
          </div>
        </div>
      </div>
    </>
  );
};

type PlaylistProps = {
  playlist: PlaylistResponse;
};

export const Playlist = ({ playlist }: PlaylistProps) => {
  return (
    <div className="m-2 my-6 md:my-8 p-2 md:w-full md:max-w-md justify-start w-72 border-gray-600 border-2 rounded-md mx-auto">
      <div className="flex space-x-4">
        <img className="h-20 w-20" src={playlist.image.url} alt="playlist" />
        <div className="w-full">
          <h1 className="text-lg">{playlist.name}</h1>
          <p className="text-sm">{playlist.ownerName}</p>
          <a
            href={`spotify:playlist:${playlist.id}`}
            rel="noreferrer"
            target="_blank"
            className="text-base text-green-300 hover:italic"
          >
            Link
          </a>
          <p
            dangerouslySetInnerHTML={{ __html: playlist.description }}
            className="text-xs break-words w-44 md:w-[19rem] font-light text-slate-200 mb-2"
          />
        </div>
      </div>
      <Tracks tracks={playlist.tracks} />
    </div>
  );
};

type TracksProps = {
  tracks: TrackResponse[];
};

const Tracks = ({ tracks }: TracksProps) => {
  const parentRef = useRef<HTMLDivElement>(null);

  const virtualizer = useVirtualizer({
    count: tracks.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => 40.8,
  });

  const items = virtualizer.getVirtualItems();

  const firstMatchIndex = tracks.findIndex((track) => track.match);

  useLayoutEffect(() => {
    if (firstMatchIndex !== -1) {
      virtualizer.scrollToIndex(firstMatchIndex, { align: "start" });
    }
  }, [firstMatchIndex, virtualizer]);

  return (
    <div
      ref={parentRef}
      className="List"
      style={{
        height: 400,
        width: "100%",
        overflowY: "auto",
        contain: "strict",
      }}
    >
      <div
        style={{
          height: virtualizer.getTotalSize(),
          width: "100%",
          position: "relative",
        }}
      >
        <div
          style={{
            position: "absolute",
            top: 0,
            left: 0,
            width: "100%",
            transform: `translateY(${items[0]?.start ?? 0}px)`,
          }}
        >
          {items.map((item) => {
            const track = tracks[item.index];
            return (
              <div
                key={item.key}
                data-index={item.index}
                ref={virtualizer.measureElement}
              >
                <p
                  className={
                    "border-b border-gray-300 p-2 " +
                    (track.match
                      ? "bg-gradient-to-r from-green-600/80 via-black  to-green-600/80 "
                      : "")
                  }
                >
                  <span className="text-gray-200 font-light mr-2">
                    {item.index + 1}
                  </span>{" "}
                  {track.name} -{" "}
                  <span className="text-white font-light">
                    {track.artistName}
                  </span>
                </p>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};

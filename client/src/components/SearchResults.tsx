import { useVirtualizer } from "@tanstack/react-virtual";
import { useLayoutEffect, useRef } from "react";
import { PlaylistResponse, TrackResponse } from "../api";
type Props = {
  playlists: Array<PlaylistResponse>;
  totalPlaylists: number;
};

export const SearchResults = ({ playlists, totalPlaylists }: Props) => {
  const parentRef = useRef<HTMLDivElement>(null);

  const virtualizer = useVirtualizer({
    count: playlists.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => 532,
    gap: 16,
  });

  const items = virtualizer.getVirtualItems();

  return (
    <>
      <p>
        Showing {items.length} / {totalPlaylists} playlists
      </p>
      <div
        ref={parentRef}
        className="grow"
        style={{
          height: virtualizer.getTotalSize() + 50,
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
                  (virtualRow.index % 2 ? "ListItemOdd" : "ListItemEven") +
                  " mt-4"
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
  onClose?: () => void;
};

export const Playlist = ({ playlist, onClose }: PlaylistProps) => {
  return (
    <div className="relative mx-auto w-72 justify-start rounded-md border-2 border-gray-600 p-2 md:w-full md:max-w-md">
      {onClose && (
        <button
          className="absolute top-2 right-2 z-10 rounded-full p-2 text-xl text-white"
          onClick={onClose}
          aria-label="Hide playlist"
        >
          ×
        </button>
      )}
      <div className="flex space-x-4">
        <img className="h-20 w-20" src={playlist.image.url} alt="playlist" />
        <div className="w-full">
          <h1 className="text-lg">{playlist.name}</h1>
          <p className="text-sm">{playlist.ownerName}</p>
          <a
            href={`spotify:playlist:${playlist.id}`}
            rel="noreferrer"
            target="_blank"
            className="text-base text-violet-300 hover:italic"
          >
            Link
          </a>
          <p
            dangerouslySetInnerHTML={{ __html: playlist.description }}
            className="mb-2 w-44 text-xs font-light wrap-break-word text-slate-200 md:w-76"
          />
        </div>
      </div>
      <Tracks tracks={playlist.tracks} />
    </div>
  );
};

type TracksProps = {
  tracks: Array<TrackResponse>;
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
                      ? "bg-linear-to-r from-violet-600/80 via-black to-violet-600/80"
                      : "")
                  }
                >
                  <span className="mr-2 font-light text-gray-200">
                    {item.index + 1}
                  </span>{" "}
                  {track.name} -{" "}
                  <span className="font-light text-white">
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

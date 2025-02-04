import { useMutation } from "@tanstack/react-query";

export const SyncPlaylistsButton = () => {
  const { isPending, mutate } = useMutation({
    mutationKey: ["syncPlaylists"],
    mutationFn: async () => {
      const res = await fetch("/api/sync-playlists", { method: "POST" });
      if (!res.ok) {
        const errorMessage = await res.text();
        throw new Error(`HTTP Error ${res.status}: ${errorMessage}`);
      }
    },
  });
  return (
    <button
      onClick={() => mutate()}
      disabled={isPending}
      className="border border-black rounded-md p-2"
    >
      {isPending ? "Loading ..." : "Sync playlists"}
    </button>
  );
};

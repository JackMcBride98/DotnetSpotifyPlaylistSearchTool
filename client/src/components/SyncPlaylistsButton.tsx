import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import { SpinnerCircularFixed } from 'spinners-react';

type SyncProgressResponse = {
	totalPlaylists: number | null;
	syncedPlaylists: number;
};

export const SyncPlaylistsButton = () => {
	const queryClient = useQueryClient();
	const { isPending, mutate, error, isError } = useMutation({
		mutationKey: ['syncPlaylists'],
		mutationFn: async () => {
			const res = await fetch('/api/sync-playlists', { method: 'POST' });
			if (!res.ok) {
				const errorMessage = await res.text();
				throw new Error(`HTTP Error ${errorMessage}`);
			}
			await queryClient.invalidateQueries({ queryKey: ['profile'] });
		},
	});

	const { data: progress, isFetching: isProgressFetching } =
		useQuery<SyncProgressResponse>({
			queryKey: ['syncProgress'],
			queryFn: async () => {
				const res = await fetch('/api/sync-progress');
				if (!res.ok) throw new Error('Failed to fetch sync progress');
				return res.json();
			},
			enabled: isPending,
			refetchInterval: 500,
			staleTime: 500,
		});

	if (isPending || isProgressFetching) {
		return (
			<div className="flex flex-col items-center gap-2">
				<SpinnerCircularFixed />
				{progress && progress.totalPlaylists ? (
					<p>
						Synced {progress.syncedPlaylists} of {progress.totalPlaylists} saved
						playlists...
					</p>
				) : (
					<p>Preparing to sync playlists...</p>
				)}
			</div>
		);
	}
	return (
		<>
			<motion.button
				whileHover={{ scale: 1.1 }}
				whileTap={{ scale: 0.9 }}
				className="text-center p-4 rounded-full bg-green-600 flex space-x-2 items-center "
				onClick={() => mutate()}
				disabled={isPending}
			>
				Sync playlists
			</motion.button>
			{isError && <p className="text-red-600">Error: {error?.message}</p>}
		</>
	);
};

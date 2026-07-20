import { useQuery } from '@tanstack/react-query';
import { PlaylistsSyncProgress } from '../components/PlaylistsSyncProgress.tsx';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { LogoutButton } from '../components/LogoutButton';
import { SpinnerCircularFixed } from 'spinners-react';
import { RandomPlaylist } from '../components/RandomPlaylist';
import { SearchPlaylists } from '../components/SearchPlaylists.tsx';
import { motion } from 'framer-motion';
import { useRef, useEffect, useState } from 'react';
import { formatDate } from '../helpers/dateHelpers.ts';
import { UpIcon } from '../icons/UpIcon.tsx';
import {getProfileOptions, syncProgressOptions, syncPlaylistsMutation, getProfileQueryKey} from "../api/@tanstack/react-query.gen.ts";
import {client} from "../api/client.gen.ts";

export const Profile = () => {
	const queryClient = useQueryClient();

	const ref = useRef<HTMLDivElement>(null);
	const [showScrollToTop, setShowScrollToTop] = useState(false);
	const [showOnlyOwnPlaylists, setShowOnlyOwnPlaylists] = useState(false);

	const { isLoading, isError, error, isSuccess, data } = useQuery({
        ...getProfileOptions({client}),
	});

	const {
		isPending: isSyncingPlaylists,
		mutate: syncPlaylists,
		error: syncError,
		isError: isSyncError,
	} = useMutation({
        ...syncPlaylistsMutation(),
		onSettled: () => queryClient.invalidateQueries({ queryKey: getProfileQueryKey({client}) }),
	});

	const { data: syncProgressData } = useQuery({
        ...syncProgressOptions({client}),
		enabled: isSyncingPlaylists,
		refetchInterval: 500,
		staleTime: 500,
	});

	useEffect(() => {
		const handleScroll = () => {
			if (window.scrollY > 500) {
				setShowScrollToTop(true);
			} else {
				setShowScrollToTop(false);
			}
		};

		document.addEventListener('scroll', handleScroll);

		return () => {
			document.removeEventListener('scroll', handleScroll);
		};
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
				<p className="text-red-600">Error: {error?.message}</p>{' '}
			</div>
		);
	}

	const { user, totalPlaylists, lastSyncedAt } = data;

	return (
		<div
			ref={ref}
			className="w-full min-w-screen h-full min-h-screen flex flex-col items-center space-y-4 bg-black text-white overflow-y-auto pb-8"
		>
			<h1 className="font-bold text-xl md:text-3xl">Playlist Search Tool</h1>

			<p>Hello {user.displayName.split(' ')[0]}</p>
			<img
				className="rounded-full"
				src={user.profileImageUrl || undefined}
				width={150}
				height={150}
				alt="User's spotify profile"
			/>

			{lastSyncedAt == null && !isSyncingPlaylists ? (
				<div className="flex flex-col items-center gap-2 w-full">
					<motion.button
						whileHover={{ scale: 1.1 }}
						whileTap={{ scale: 0.9 }}
						className="text-center p-4 rounded-full bg-green-600 flex space-x-2 items-center"
						onClick={() => syncPlaylists({})}
						disabled={isSyncingPlaylists}
					>
						Sync playlists
					</motion.button>
					{isSyncError && (
						<p className="text-red-600">Error: {syncError?.message}</p>
					)}
				</div>
			) : (
				<div className="flex flex-col items-center gap-4 w-full">
					{isSyncingPlaylists ? (
						<PlaylistsSyncProgress
							syncProgressData={syncProgressData}
							isSyncingPlaylists={isSyncingPlaylists}
						/>
					) : (
						<div className="flex flex-col items-center space-x-1">
							<p>You have {totalPlaylists} playlists saved</p>
							<p>
								Last updated:{' '}
								{lastSyncedAt ? formatDate(lastSyncedAt) : 'never'}
							</p>
						</div>
					)}
					<RandomPlaylist showOnlyOwnPlaylists={showOnlyOwnPlaylists} />
					<SearchPlaylists
						totalPlaylists={
							isSyncingPlaylists &&
							syncProgressData &&
							syncProgressData?.syncedPlaylists
								? syncProgressData.syncedPlaylists
								: totalPlaylists
						}
						showOnlyOwnPlaylists={showOnlyOwnPlaylists}
						setShowOnlyOwnPlaylists={setShowOnlyOwnPlaylists}
					/>
				</div>
			)}

			<LogoutButton />
			<motion.button
				onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })}
				whileHover={{ scale: 1.1 }}
				whileTap={{ scale: 0.9 }}
				className={
					'rounded-full text-black text-xl bg-green-600 w-12 h-12 md:w-20 md:h-20  text-center fixed transition-all bottom-2 md:bottom-4 right-2 md:right-28 opacity-80 hover:opacity-100 focus:opacity-100 ' +
					(!showScrollToTop && 'hidden')
				}
			>
				<UpIcon />
			</motion.button>
		</div>
	);
};

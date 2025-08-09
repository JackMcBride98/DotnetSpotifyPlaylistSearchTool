import { useState } from 'react';
import searchIcon from '../assets/search.svg';

interface SearchBarProps {
	onSearch: (searchTerm: string) => void;
	isLoading: boolean;
}

export const SearchBar = ({ onSearch, isLoading }: SearchBarProps) => {
	const [intermediateSearchTerm, setIntermediateSearchTerm] = useState('');

	const handleSearch = () => {
		onSearch(intermediateSearchTerm);
	};

	return (
		<>
			<div className="md:w-80 w-72 flex items-center bg-green-600 rounded-md pl-1">
				<input
					className="p-2 outline-0 bg-black w-full overflow-visible"
					type="text"
					placeholder="Search for songs or artists"
					onKeyDown={(e) => {
						if (e.key === 'Enter') {
							handleSearch();
						}
					}}
					value={intermediateSearchTerm}
					onChange={(e) => setIntermediateSearchTerm(e.target.value)}
				/>
				<button
					disabled={isLoading}
					className="p-4 disabled:cursor-not-allowed"
					onClick={handleSearch}
				>
					<img
						src={searchIcon}
						alt="search"
						className="w-6 h-6 fill-black bg-green-600"
					/>
				</button>
			</div>
		</>
	);
};

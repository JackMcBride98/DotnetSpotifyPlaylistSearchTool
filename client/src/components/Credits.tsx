import githubLogo from "../assets/github-logo.png";

export const Credits = () => {
  return (
    <div className="flex flex-col items-center space-y-2">
      <div className="">
        Created by{" "}
        <a
          href="https://portfolio-jackmcbride.vercel.app/"
          rel="noopener noreferrer"
          target="_blank"
          className="text-violet-600"
        >
          Jack McBride
        </a>
      </div>
      <a
        href="https://github.com/JackMcBride98/DotnetSpotifyPlaylistSearchTool"
        rel="noopener noreferrer"
        target="_blank"
        className="mx-auto mb-4"
      >
        <img
          src={githubLogo}
          alt="Github"
          className="mt-2 h-8 w-8 invert"
        ></img>
      </a>
    </div>
  );
};

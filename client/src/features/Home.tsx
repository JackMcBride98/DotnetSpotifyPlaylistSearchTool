import { useMutation } from "@tanstack/react-query";
import { LoadingSpinner } from "../components/LoadingSpinner.tsx";

type LoginResponse = {
  loginUri: string;
};

export const Home = () => {
  const mutation = useMutation({
    mutationFn: async () => {
      const response = await fetch("/api/login", {
        method: "POST",
      });
      if (!response.ok) {
        const errorMessage = await response.text();
        throw new Error(`HTTP Error ${response.status}: ${errorMessage}`);
      }
      const body: LoginResponse = await response.json();
      if (!body.loginUri) {
      }
      window.location.href = body.loginUri;
    },
  });

  return (
    <div className="w-screen h-screen flex flex-col items-center space-y-10">
      <h1 className="font-bold text-5xl">Hello, World!</h1>
      <button
        onClick={() => mutation.mutate()}
        className="mt-10 rounded-md border border-black p-4"
      >
        {mutation.isPending ? <LoadingSpinner /> : "Log in"}
      </button>
      {mutation.isError && (
        <p className="text-red-500">{mutation.error.message}</p>
      )}
    </div>
  );
};

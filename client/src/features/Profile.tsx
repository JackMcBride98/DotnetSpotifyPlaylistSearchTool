import { useQuery } from "@tanstack/react-query";

type User = {
  country: string;
  displayName: string;
  email: string;
  externalUrls: Record<string, string>;
  followers: {
    href: string;
    total: number;
  };
  href: string;
  id: string;
  images: {
    height: number;
    url: string;
    width: number;
  }[];
  product: string;
  type: string;
  uri: string;
};

type UserResponse = { user: User };

export const Profile = () => {
  const { isLoading, isError, error, isSuccess, data } = useQuery<
    UserResponse,
    Error
  >({
    queryKey: ["profile"],
    queryFn: async () => {
      const res = await fetch("/api/profile");
      if (!res.ok) {
        const errorMessage = await res.text();
        throw new Error(`HTTP Error ${res.status}: ${errorMessage}`);
      }

      return res.json();
    },
  });

  if (isLoading) {
    return <p>Loading...</p>;
  }

  if (isError || !isSuccess) {
    return <p>Error: {error?.message}</p>;
  }

  const { user } = data;

  return (
    <div className="flex flex-col">
      <h1>Profile</h1>
      <p>{user.displayName}</p>
    </div>
  );
};

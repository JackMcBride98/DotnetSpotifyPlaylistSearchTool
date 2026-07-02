import { SpinnerCircularFixed } from "spinners-react";
import { useState } from "react";
import { useNavigate } from "react-router";
import { useMutation } from "@tanstack/react-query";
import { motion } from "framer-motion";

export const LogoutButton = () => {
  const navigate = useNavigate();
  const [isLoggingOut, setIsLoggingOut] = useState(false);

  const { isError, error, mutate, isPending } = useMutation({
    mutationFn: async () => {
      setIsLoggingOut(true);
      const res = await fetch("/api/logout", {
        method: "POST",
        credentials: "include",
      });
      if (!res.ok) {
        const errorMessage = await res.text();
        setIsLoggingOut(false);
        throw new Error(`HTTP Error ${res.status}: ${errorMessage}`);
      }
      setIsLoggingOut(false);
      return res.text();
    },
    onSuccess: () => {
      navigate("/");
    },
  });

  return (
    <>
      <motion.button
        whileHover={{ scale: isLoggingOut ? 1 : 1.1 }}
        whileTap={{ scale: 0.9 }}
        className="text-center p-4 rounded-full bg-green-600 flex space-x-2 items-center "
        onClick={() => mutate()}
        disabled={isLoggingOut}
      >
        <p>Logout</p>
      </motion.button>
      {isError && <p className="text-red-600">Error: {error?.message}</p>}
      {isPending && <SpinnerCircularFixed />}
    </>
  );
};

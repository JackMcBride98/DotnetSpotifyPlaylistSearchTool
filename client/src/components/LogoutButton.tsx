import { SpinnerCircularFixed } from "spinners-react";
import { useState } from "react";
import { useNavigate } from "react-router";
import { useMutation } from "@tanstack/react-query";
import { motion } from "framer-motion";
import { logoutMutation } from "../api/@tanstack/react-query.gen.ts";

export const LogoutButton = () => {
  const navigate = useNavigate();
  const [isLoggingOut, setIsLoggingOut] = useState(false);

  const { isError, error, mutate, isPending } = useMutation({
    ...logoutMutation(),
    onSuccess: () => {
      navigate("/");
    },
    onSettled: () => {
      setIsLoggingOut(false);
    },
  });

  const handleLogout = () => {
    setIsLoggingOut(true);
    mutate({});
  };

  return (
    <>
      <motion.button
        whileHover={{ scale: isLoggingOut ? 1 : 1.1 }}
        whileTap={{ scale: 0.9 }}
        className="text-center p-4 rounded-full bg-green-600 flex space-x-2 items-center "
        onClick={() => handleLogout()}
        disabled={isLoggingOut}
      >
        <p>Logout</p>
      </motion.button>
      {isError && <p className="text-red-600">Error: {error?.message}</p>}
      {isPending && <SpinnerCircularFixed />}
    </>
  );
};

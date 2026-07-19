import { motion } from "framer-motion";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { SpinnerCircularFixed } from "spinners-react";
import { logInMutation } from "../api/@tanstack/react-query.gen.ts";

export const LoginButton = () => {
  const [isLoggingIn, setIsLoggingIn] = useState(false);

  const { isError, error, mutate } = useMutation({
      ...logInMutation(),
      onSuccess: (data) => {
        window.location.href = data.loginUri;
      },
      onError: (err) => {
          console.error("Login failed:", err);
      },
      onSettled: () => {
          setIsLoggingIn(false);
      }
  });

  const handleLogin = () => {
    setIsLoggingIn(true);
    mutate({});
  };

  return (
    <>
      <motion.button
        whileHover={{ scale: isLoggingIn ? 1 : 1.1 }}
        whileTap={{ scale: 0.9 }}
        className="text-center p-4 rounded-full bg-green-600 flex space-x-2 items-center "
        onClick={handleLogin}
        disabled={isLoggingIn}
      >
        <svg className="w-7 h-7" viewBox="0 0 100 100">
          <circle className="fill-white" cx="50" cy="50" r="40" />
          <path
            className="stroke-green-600"
            d="M 31 62 Q 50 56, 67 66"
            fill="transparent"
            strokeLinecap="round"
            strokeWidth="5"
            strokeDashoffset="0"
          />
          <path
            className="stroke-green-600"
            d="M 29 49 Q 51 42, 72 54"
            fill="transparent"
            strokeLinecap="round"
            strokeWidth="6"
          />
          <path
            className="stroke-green-600"
            d="M 26 36 Q 52 27 , 76 40"
            fill="transparent"
            strokeLinecap="round"
            strokeWidth="7"
          />
        </svg>
        <p>Login with Spotify</p>
      </motion.button>
      {isError && <p className="text-red-600">Error: {error?.message}</p>}
      {isLoggingIn && <SpinnerCircularFixed />}
    </>
  );
};

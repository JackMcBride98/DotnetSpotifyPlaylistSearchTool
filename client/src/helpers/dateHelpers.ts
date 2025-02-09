export const formatDate = (isoString: string) => {
  if (!isoString) return "never";

  const date = new Date(isoString);

  return date.toLocaleString("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  });
};

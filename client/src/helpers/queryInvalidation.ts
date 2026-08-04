import type { QueryKey } from "@tanstack/react-query";

/**
 * Creates a TanStack Query predicate to invalidate all queries matching a specific base ID,
 * regardless of their dynamic query parameters or options.
 */
export const baseIdPredicate = (baseId: string) => {
  return (query: { queryKey: QueryKey }) => {
    const firstPartOfQueryKey = query.queryKey[0];

    if (
      typeof firstPartOfQueryKey === "object" &&
      firstPartOfQueryKey !== null &&
      "_id" in firstPartOfQueryKey
    ) {
      return firstPartOfQueryKey._id === baseId;
    }

    return false;
  };
};

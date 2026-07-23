export function getErrorMessage(error: unknown): string {
  console.log(error);
  if (!error) return "An unknown error occurred";

  const apiError = (error as any)?.error || (error as any)?.body || error;

  if (typeof apiError?.detail === "string" && apiError.detail.trim() !== "") {
    return apiError.detail;
  }

  if (Array.isArray(apiError?.errors) && apiError.errors.length > 0) {
    const firstReason = apiError.errors[0]?.reason;
    if (typeof firstReason === "string") {
      return firstReason;
    }
  }

  if (typeof apiError?.title === "string") {
    return apiError.title;
  }

  if ("reason" in apiError && typeof apiError.reason === "string") {
    return apiError.reason;
  }

  if (error instanceof Error && error.message) {
    return error.message;
  }

  return "An unknown error occurred";
}

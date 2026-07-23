type ProblemDetailErrorItem = {
  name?: string;
  reason?: string;
};

type ApiErrorPayload = {
  detail?: string;
  title?: string;
  reason?: string;
  errors?: ProblemDetailErrorItem[];
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function extractErrorPayload(error: unknown): ApiErrorPayload | null {
  if (!isRecord(error)) return null;

  // Handle wrapped structures like Axios/Fetch responses: error.error or error.body
  const nested = error.error ?? error.body;
  if (isRecord(nested)) {
    return nested as ApiErrorPayload;
  }

  return error as ApiErrorPayload;
}

export function getErrorMessage(error: unknown): string {
  if (!error) return "An unknown error occurred";

  const apiError = extractErrorPayload(error);

  if (apiError) {
    // 1. Check for ProblemDetails 'detail'
    if (typeof apiError.detail === "string" && apiError.detail.trim() !== "") {
      return apiError.detail;
    }

    // 2. Check FastEndpoints ProblemDetails 'errors' array
    if (Array.isArray(apiError.errors) && apiError.errors.length > 0) {
      const firstReason = apiError.errors[0]?.reason;
      if (typeof firstReason === "string" && firstReason.trim() !== "") {
        return firstReason;
      }
    }

    // 3. Check for ProblemDetails 'title'
    if (typeof apiError.title === "string" && apiError.title.trim() !== "") {
      return apiError.title;
    }

    // 4. Check for top-level 'reason'
    if (typeof apiError.reason === "string" && apiError.reason.trim() !== "") {
      return apiError.reason;
    }
  }

  // 5. Check standard JS Error instance
  if (error instanceof Error && error.message) {
    return error.message;
  }

  // 6. Check primitive string error (e.g. throw "Some string")
  if (typeof error === "string" && error.trim() !== "") {
    return error;
  }

  return "An unknown error occurred";
}

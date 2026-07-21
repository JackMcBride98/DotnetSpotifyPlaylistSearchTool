import { describe, expect, test } from "vitest";
import { formatDate } from "./dateHelpers";

describe("formatDate", () => {
  test("if string is empty returns never", () => {
    const formattedDate = formatDate("");
    expect(formattedDate).toBe("never");
  });

  test("parses isoString to expected date format", () => {
    const date = new Date();
    const formattedDate = date.toISOString();
    const expectedDate = date.toLocaleString("en-GB", {
      day: "2-digit",
      month: "short",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
      hour12: false,
    });

    console.log(expectedDate);

    expect(formatDate(formattedDate)).toBe(expectedDate);
  });
});

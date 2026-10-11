/**
 * Date helpers for talking to the API.
 *
 * Date.toISOString() is the trap these exist to avoid. It converts to UTC, so in any zone
 * ahead of it, local midnight becomes the previous day: in Manila, Monday 5 October 00:00
 * is sent as 2026-10-04T16:00:00Z. The server then reads a Sunday, and its "start of week"
 * rule pushes that back six days to the Monday before. One call turned a whole week's
 * timesheet into the wrong week's.
 *
 * These send what the person sees on their own calendar, with no zone conversion.
 */

/** A date as yyyy-MM-dd, taken from local time rather than UTC. */
export function toIsoDate(date: Date): string {
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

/** Today, as the person's own calendar has it. */
export function todayIso(): string {
  return toIsoDate(new Date());
}

/**
 * A local date and time with no zone suffix, for an API parameter typed as DateTime.
 * Without the trailing Z the server takes the value as written instead of shifting it.
 */
export function toLocalDateTime(date: Date): string {
  return `${toIsoDate(date)}T00:00:00`;
}

/** Monday of the week containing the given date, matching the server's week start. */
export function startOfWeek(date: Date): Date {
  const result = new Date(date);
  const daysSinceMonday = (result.getDay() - 1 + 7) % 7;
  result.setDate(result.getDate() - daysSinceMonday);
  result.setHours(0, 0, 0, 0);
  return result;
}

/** The last day of the week containing the given date. */
export function endOfWeek(date: Date): Date {
  const result = startOfWeek(date);
  result.setDate(result.getDate() + 6);
  return result;
}

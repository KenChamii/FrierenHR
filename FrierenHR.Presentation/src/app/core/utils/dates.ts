/** Date helpers for talking to the API. */

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

/** A local date and time with no zone suffix, for an API parameter typed as DateTime. */
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

/** Calendar days in the user's time zone; months are numbered 1-12. */
export interface CalendarDay {
  year: number;
  month: number;
  day: number;
}

export function toDate({ year, month, day }: CalendarDay): Date {
  return new Date(year, month - 1, day);
}

export function fromDate(date: Date): CalendarDay {
  return { year: date.getFullYear(), month: date.getMonth() + 1, day: date.getDate() };
}

export function addDays(day: CalendarDay, count: number): CalendarDay {
  const date = toDate(day);
  date.setDate(date.getDate() + count);
  return fromDate(date);
}

export function daysInMonth(year: number, month: number): number {
  return new Date(year, month, 0).getDate();
}

export function isWeekend(day: CalendarDay): boolean {
  const weekday = toDate(day).getDay();
  return weekday === 0 || weekday === 6;
}

export function compare(a: CalendarDay, b: CalendarDay): number {
  return a.year - b.year || a.month - b.month || a.day - b.day;
}

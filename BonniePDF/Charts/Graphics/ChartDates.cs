// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
namespace BonniePDF;

/// <summary>Calendar units a time axis ticks in: <c>year</c>, <c>quarter</c>, <c>month</c>, <c>week</c>, <c>day</c> and <c>hour</c>.</summary>
public static class ChartDates
{
    private const long MsPerHour = 3_600_000;
    private const long MsPerDay = 86_400_000;
    private static readonly string[] Months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
    private static readonly string[] Units = ["hour", "day", "week", "month", "quarter", "year"];

    private static long DaysFromCivil(long year, int month, int day)
    {
        long y = month <= 2 ? year - 1 : year;
        long era = (y >= 0 ? y : y - 399) / 400;
        long yoe = y - era * 400;
        long doy = (153 * (month + (month > 2 ? -3 : 9)) + 2) / 5 + day - 1;
        long doe = yoe * 365 + yoe / 4 - yoe / 100 + doy;
        return era * 146097 + doe - 719468;
    }

    private static (long Year, int Month, int Day) CivilFromDays(long days)
    {
        long z = days + 719468;
        long era = (z >= 0 ? z : z - 146096) / 146097;
        long doe = z - era * 146097;
        long yoe = (doe - doe / 1460 + doe / 36524 - doe / 146096) / 365;
        long doy = doe - (365 * yoe + yoe / 4 - yoe / 100);
        long mp = (5 * doy + 2) / 153;
        int day = (int)(doy - (153 * mp + 2) / 5 + 1);
        int month = (int)(mp < 10 ? mp + 3 : mp - 9);
        return (yoe + era * 400 + (month <= 2 ? 1 : 0), month, day);
    }

    private static int DaysInMonth(long year, int month) =>
        month == 2 ? (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0) ? 29 : 28) : month is 4 or 6 or 9 or 11 ? 30 : 31;

    private static long FloorDiv(long a, long b) => a >= 0 ? a / b : -((-a + b - 1) / b);

    /// <summary>
    /// Parses an ISO 8601 date (<c>2026-11-24</c>) or UTC date-time (<c>2026-11-24T09:30:00Z</c>) into milliseconds since the Unix epoch.
    /// There is no time-zone database: callers convert first.
    /// </summary>
    public static long Parse(string input)
    {
        if (input is null || input.Length is not (10 or 17 or 20)) throw new InvalidOperationException($"'{input}' is not an ISO 8601 date or UTC date-time.");
        static bool Digits(string s, int at, int count) { for (int i = at; i < at + count; i++) if (s[i] is < '0' or > '9') return false; return true; }
        static int Num(string s, int at, int count) => int.Parse(s.AsSpan(at, count), NumberStyles.None, CultureInfo.InvariantCulture);
        bool ok = Digits(input, 0, 4) && input[4] == '-' && Digits(input, 5, 2) && input[7] == '-' && Digits(input, 8, 2);
        int hour = 0, minute = 0, second = 0;
        if (ok && input.Length > 10)
        {
            ok = input[10] == 'T' && Digits(input, 11, 2) && input[13] == ':' && Digits(input, 14, 2) && input[^1] == 'Z';
            if (ok && input.Length == 20) ok = input[16] == ':' && Digits(input, 17, 2);
            if (ok && input.Length is 17 or 20) { hour = Num(input, 11, 2); minute = Num(input, 14, 2); second = input.Length == 20 ? Num(input, 17, 2) : 0; }
            if (ok && input.Length == 18) ok = false;
        }
        if (!ok) throw new InvalidOperationException($"'{input}' is not an ISO 8601 date or UTC date-time.");
        int year = Num(input, 0, 4), month = Num(input, 5, 2), day = Num(input, 8, 2);
        if (month < 1 || month > 12 || day < 1 || day > DaysInMonth(year, month) || hour > 23 || minute > 59 || second > 59) throw new InvalidOperationException($"'{input}' is not a valid date.");
        return DaysFromCivil(year, month, day) * MsPerDay + hour * MsPerHour + minute * 60_000L + second * 1000L;
    }

    /// <summary>Milliseconds since the Unix epoch of a <see cref="DateTime"/>, converted to UTC unless its kind is already UTC.</summary>
    public static long Parse(DateTime value) => new DateTimeOffset(value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime()).ToUnixTimeMilliseconds();

    /// <summary>Milliseconds since the Unix epoch of a <see cref="DateTimeOffset"/>.</summary>
    public static long Parse(DateTimeOffset value) => value.ToUnixTimeMilliseconds();

    /// <summary>Milliseconds since the Unix epoch of midnight UTC on a <see cref="DateOnly"/>.</summary>
    public static long Parse(DateOnly value) => DaysFromCivil(value.Year, value.Month, value.Day) * MsPerDay;

    /// <summary>The default tick format of a unit.</summary>
    public static string DefaultFormat(string unit) => unit switch
    {
        "year" => "yyyy",
        "quarter" => "'Q'Q yyyy",
        "month" => "MMM yyyy",
        "hour" => "HH:mm",
        _ => "dd MMM",
    };

    private static string Pad(long n, int width = 2) => n.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');

    /// <summary>
    /// Formats milliseconds since the epoch. Tokens: <c>yyyy</c>, <c>yy</c>, <c>Q</c> (quarter), <c>MMM</c> (English abbreviation), <c>MM</c>, <c>dd</c>, <c>d</c>, <c>HH</c>, <c>mm</c>;
    /// text in single quotes is literal. Culture-invariant, always UTC.
    /// </summary>
    public static string Format(long ms, string format)
    {
        long days = FloorDiv(ms, MsPerDay);
        (long year, int month, int day) = CivilFromDays(days);
        long rest = ms - days * MsPerDay;
        long hour = rest / MsPerHour;
        long minute = (rest - hour * MsPerHour) / 60_000;
        StringBuilder output = new();
        for (int i = 0; i < format.Length;)
        {
            if (format[i] == '\'')
            {
                int end = format.IndexOf('\'', i + 1);
                if (end < 0) throw new InvalidOperationException("A date format has an unterminated quote.");
                output.Append(format, i + 1, end - i - 1);
                i = end + 1;
            }
            else if (string.CompareOrdinal(format, i, "yyyy", 0, 4) == 0) { output.Append(Pad(year, 4)); i += 4; }
            else if (string.CompareOrdinal(format, i, "yy", 0, 2) == 0) { output.Append(Pad(year % 100)); i += 2; }
            else if (string.CompareOrdinal(format, i, "MMM", 0, 3) == 0) { output.Append(Months[month - 1]); i += 3; }
            else if (string.CompareOrdinal(format, i, "MM", 0, 2) == 0) { output.Append(Pad(month)); i += 2; }
            else if (string.CompareOrdinal(format, i, "dd", 0, 2) == 0) { output.Append(Pad(day)); i += 2; }
            else if (format[i] == 'd') { output.Append(day.ToString(CultureInfo.InvariantCulture)); i++; }
            else if (string.CompareOrdinal(format, i, "HH", 0, 2) == 0) { output.Append(Pad(hour)); i += 2; }
            else if (string.CompareOrdinal(format, i, "mm", 0, 2) == 0) { output.Append(Pad(minute)); i += 2; }
            else if (format[i] == 'Q') { output.Append((month - 1) / 3 + 1); i++; }
            else output.Append(format[i++]);
        }
        return output.ToString();
    }

    /// <summary>The start of the unit that contains <paramref name="ms"/>: 1 January, the first of a quarter or month, an ISO Monday, midnight or the hour.</summary>
    public static long StartOfUnit(long ms, string unit)
    {
        long days = FloorDiv(ms, MsPerDay);
        (long year, int month, _) = CivilFromDays(days);
        switch (unit)
        {
            case "year": return DaysFromCivil(year, 1, 1) * MsPerDay;
            case "quarter": return DaysFromCivil(year, (month - 1) / 3 * 3 + 1, 1) * MsPerDay;
            case "month": return DaysFromCivil(year, month, 1) * MsPerDay;
            case "week":
            {
                long weekday = ((days + 4) % 7 + 7) % 7; // 0 = Sunday
                return (days - (weekday + 6) % 7) * MsPerDay;
            }
            case "day": return days * MsPerDay;
            default: return FloorDiv(ms, MsPerHour) * MsPerHour;
        }
    }

    /// <summary>The start of the unit after the one starting at <paramref name="ms"/>.</summary>
    public static long NextUnit(long ms, string unit)
    {
        long days = FloorDiv(ms, MsPerDay);
        (long year, int month, _) = CivilFromDays(days);
        switch (unit)
        {
            case "year": return DaysFromCivil(year + 1, 1, 1) * MsPerDay;
            case "quarter":
            case "month":
            {
                int step = string.Equals(unit, "quarter", StringComparison.Ordinal) ? 3 : 1;
                long total = year * 12 + (month - 1) + step;
                return DaysFromCivil(FloorDiv(total, 12), (int)(total % 12) + 1, 1) * MsPerDay;
            }
            case "week": return ms + 7 * MsPerDay;
            case "day": return ms + MsPerDay;
            default: return ms + MsPerHour;
        }
    }

    /// <summary>The tick positions of <paramref name="unit"/> from <paramref name="min"/> to <paramref name="max"/>: every unit start within the range.</summary>
    public static IReadOnlyList<long> Ticks(long min, long max, string unit)
    {
        List<long> output = [];
        long t = StartOfUnit(min, unit);
        if (t < min) t = NextUnit(t, unit);
        while (t <= max && output.Count < 10_000) { output.Add(t); t = NextUnit(t, unit); }
        return output;
    }

    /// <summary>The smallest unit from hour to year whose tick count fits <c>plotLength / 64</c> ticks.</summary>
    public static string ChooseUnit(long min, long max, double plotLength)
    {
        int limit = Math.Max(1, (int)Math.Floor(plotLength / 64));
        foreach (string unit in Units) if (Ticks(min, max, unit).Count <= limit) return unit;
        return "year";
    }
}

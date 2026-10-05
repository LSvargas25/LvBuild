namespace LvApplication.Common;

/// <summary>
/// Timestamps are always stored in UTC (PostgreSQL timestamptz). Calendar dates entered by
/// users (weeks, periods, start dates) are Costa Rica dates, so any comparison between the two
/// must translate the Costa Rica day boundaries to UTC first. Use this only at those
/// boundaries and for presentation; never to produce values that get persisted.
/// </summary>
public static class CostaRicaTime
{
    // Costa Rica has no daylight saving time; the fixed offset is the fallback if the host
    // has no time zone database (e.g. a minimal container image without ICU/tzdata).
    private static readonly TimeSpan FixedOffset = TimeSpan.FromHours(-6);

    public static readonly TimeZoneInfo Zone = ResolveZone();

    /// <summary>The UTC instant at which the given Costa Rica calendar day starts.</summary>
    public static DateTime StartOfDayUtc(DateTime costaRicaDate) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(costaRicaDate.Date, DateTimeKind.Unspecified),
            Zone
        );

    private static TimeZoneInfo ResolveZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Costa_Rica");
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(
                "America/Costa_Rica",
                FixedOffset,
                "Costa Rica",
                "Costa Rica"
            );
        }
    }
}

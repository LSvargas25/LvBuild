namespace LvApplication.Common;

/// <summary>
/// PostgreSQL compares text case-sensitively (SQL Server's default collation did not), so
/// emails are stored and looked up in a single canonical form.
/// </summary>
public static class EmailNormalizer
{
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}

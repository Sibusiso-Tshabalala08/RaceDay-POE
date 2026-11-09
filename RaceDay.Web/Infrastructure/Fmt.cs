using System.Globalization;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Display formatting used by every view, so dates, money, distances and
/// status colours look identical on every page.
/// </summary>
public static class Fmt
{
    private static readonly CultureInfo C = CultureInfo.InvariantCulture;

    public static string Date(DateTime d) => d.ToString("ddd, d MMM yyyy", C);

    public static string DateTimeLong(DateTime d) => d.ToString("ddd, d MMM yyyy '·' HH:mm", C);

    public static string ShortDate(DateTime d) => d.ToString("d MMM yyyy", C);

    public static string Money(decimal amount) => amount == 0 ? "Free" : "R" + amount.ToString("0.00", C);

    public static string Distance(decimal? km) =>
        km is null ? "—" : km.Value.ToString("0.##", C) + " km";

    public static string Time(TimeSpan? t) =>
        t is null ? "—" : t.Value.ToString(@"hh\:mm\:ss", C);

    /// <summary>1 -> "1st", 2 -> "2nd", 11 -> "11th", 47 -> "47th".</summary>
    public static string Ordinal(int n)
    {
        var lastTwo = n % 100;
        var suffix = (lastTwo is >= 11 and <= 13) ? "th" : (n % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
        return n.ToString(C) + suffix;
    }

    public static string Initials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "?";
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..1].ToUpperInvariant()
            : (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }

    /// <summary>Colour-coded status: green = good, amber = waiting/partial, red = cancelled/disqualified.</summary>
    public static string StatusClass(string? status) => (status ?? string.Empty).ToLowerInvariant() switch
    {
        "confirmed" or "finished" => "rd-badge rd-badge-green",
        "pending" or "dnf" => "rd-badge rd-badge-amber",
        "cancelled" or "dq" => "rd-badge rd-badge-red",
        _ => "rd-badge rd-badge-grey"
    };

    public static string StatusIcon(string? status) => (status ?? string.Empty).ToLowerInvariant() switch
    {
        "confirmed" or "finished" => "bi-check-circle-fill",
        "pending" or "dnf" => "bi-hourglass-split",
        "cancelled" or "dq" => "bi-x-circle-fill",
        _ => "bi-dash-circle"
    };

    public static string TypeIcon(string? eventType) => eventType switch
    {
        "Run" => "bi-lightning-charge-fill",
        "Walk" => "bi-person-walking",
        "Cycle" => "bi-bicycle",
        _ => "bi-flag-fill"
    };

    public static string TypeClass(string? eventType) => "rd-type-" + (eventType ?? "other").ToLowerInvariant();
}

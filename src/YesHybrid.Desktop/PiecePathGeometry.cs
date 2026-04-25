using Avalonia;
using Avalonia.Media;

namespace YesHybrid.Desktop;

/// <summary>
/// Piece glyphs use stroke outlines from <see href="https://lucide.dev">Lucide</see> (MIT);
/// 24×24 viewBox, scaled in the view. Filled “flat” art was replaced for clearer, familiar icons.
/// </summary>
internal static class PiecePathGeometry
{
    public readonly struct PieceIcon
    {
        public Geometry? Geometry { get; init; }
    }

    public static PieceIcon ForFenChar(char c)
    {
        if (c is '.') return default;
        if (c is '*' or '#') return new PieceIcon { Geometry = GridTerrain() };
        return char.ToLowerInvariant(c) switch
        {
            'd' or 'e' => new PieceIcon { Geometry = TryP(LucideShield) },
            's' or 'y' => new PieceIcon { Geometry = Compass() },
            'c' or 'v' => new PieceIcon { Geometry = WandSparkles() },
            'l' or 'x' or 'o' or 'q' => new PieceIcon { Geometry = Swords() },
            't' => new PieceIcon { Geometry = Package() },
            'b' => new PieceIcon { Geometry = TryP(LucideGhostBody) },
            'a' => new PieceIcon { Geometry = Crosshair() },
            'u' => new PieceIcon { Geometry = TryP(LucideFlame) },
            'm' => new PieceIcon { Geometry = Minion() },
            _ => default,
        };
    }

    // --- Lucide path strings (single-figure; multi-figure use helpers below) ---

    // shield
    private const string LucideShield =
        "M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z";

    // Swords — rogues (two crossed blades + hilts; Lucide "swords")
    private static Geometry? Swords() =>
        Combine(
            TryP("M 14.5 17.5 L 3 6 L 3 3 L 6 3 L 17.5 14.5"),
            TryP("M 13 19 L 19 13"),
            TryP("M 16 16 L 20 20"),
            TryP("M 19 21 L 21 19"),
            TryP("M 14.5 6.5 L 18 3 L 21 3 L 21 6 L 17.5 9.5"),
            TryP("M 5 14 L 9 18"),
            TryP("M 7 17 L 4 20"),
            TryP("M 3 19 L 5 21"));

    // Box — trove
    private static Geometry? Package() =>
        Combine(
            TryP("M11 21.73a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73z"),
            TryP("M12 22V12"),
            TryP("M3.29 7L12 12L20.71 7"),
            TryP("m7.5 4.27l9 5.15"));

    // wraith — main silhouette only
    private const string LucideGhostBody =
        "M12 2a8 8 0 0 0-8 8v12l3-3 2.5 2.5L12 19l2.5 2.5L17 19l3 3V10a8 8 0 0 0-8-8z";

    //fiend
    private const string LucideFlame =
        "M12 3q1 4 4 6.5t3 5.5a1 1 0 0 1-14 0 5 5 0 0 1 1-3 1 1 0 0 0 5 0c0-2-1.5-3-1.5-5q0-2 2.5-4";

    // Minion — Lucide "bug" (legs+antenna) or "egg" fallback
    private static Geometry? Minion() =>
        Combine(
            TryP("M12 20v-9"),
            TryP("M14 7a4 4 0 0 1 4 4v3a6 6 0 0 1-12 0v-3a4 4 0 0 1 4-4z"),
            TryP("M14.12 3.88L16 2"),
            TryP("M21 21a4 4 0 0 0-3.81-4"),
            TryP("M21 5a4 4 0 0 1-3.55 3.97"),
            TryP("M22 13h-4"),
            TryP("M3 21a4 4 0 0 1 3.81-4"),
            TryP("M3 5a4 4 0 0 0 3.55 3.97"),
            TryP("M6 13H2"),
            TryP("M8 2l1.88 1.88"),
            TryP("M9 7.13V6a3 3 0 1 1 6 0v1.13")) ?? TryP("M12 2C8 2 4 8 4 14a8 8 0 0 0 16 0c0-6-4-12-8-12");

    // blocked terrain: grid
    private static Geometry? GridTerrain() => TryP("M3 9L21 9 M3 15L21 15 M9 3L9 21 M15 3L15 21");

    // compass rose inside circle
    private const string LucideCompassNeedle =
        "m16.24 7.76-1.804 5.411a2 2 0 0 1-1.265 1.265L7.76 16.24l1.804-5.411a2 2 0 0 1 1.265-1.265z";

    // wand + sparkles
    private const string Wand0 =
        "m21.64 3.64-1.28-1.28a1.21 1.21 0 0 0-1.72 0L2.36 18.64a1.21 1.21 0 0 0 0 1.72l1.28 1.28a1.2 1.2 0 0 0 1.72 0L21.64 5.36a1.2 1.2 0 0 0 0-1.72";
    private const string Wand1 = "m14 7 3 3";
    private const string Wand2 = "M5 6v4";
    private const string Wand3 = "M19 14v4";
    private const string Wand4 = "M10 2v2";
    private const string Wand5 = "M7 8H3";
    private const string Wand6 = "M21 16h-4";
    private const string Wand7 = "M11 3H9";

    private static Geometry? Compass() =>
        Combine(
            C(12, 12, 10),
            TryP(LucideCompassNeedle));

    private static Geometry? WandSparkles() =>
        Combine(
            TryP(Wand0),
            TryP(Wand1),
            TryP(Wand2),
            TryP(Wand3),
            TryP(Wand4),
            TryP(Wand5),
            TryP(Wand6),
            TryP(Wand7));

    // targeting / artillery
    private static Geometry? Crosshair() =>
        Combine(
            C(12, 12, 10),
            TryP("M22 12L18 12 M6 12L2 12 M12 6L12 2 M12 18L12 22"));

    private static EllipseGeometry C(double cx, double cy, double r) =>
        new(new Rect(cx - r, cy - r, 2 * r, 2 * r));

    private static Geometry? Combine(params Geometry?[] parts)
    {
        var list = new List<Geometry>();
        foreach (var p in parts)
        {
            if (p is { } g) list.Add(g);
        }
        if (list.Count == 0) return null;
        if (list.Count == 1) return list[0];
        var group = new GeometryGroup();
        foreach (var p in list) group.Children.Add(p);
        return group;
    }

    private static Geometry? TryP(string? data)
    {
        if (string.IsNullOrWhiteSpace(data)) return null;
        try
        {
            return StreamGeometry.Parse(data);
        }
        catch
        {
            return null;
        }
    }
}

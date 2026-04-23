using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YesHybrid.Engine.Game;

/// <summary>
/// Atomic description of "a playable version of YES Hybrid" that the
/// tournament harness can evaluate.  A rule set is the unit the search
/// layers (match / sweep / tune) iterate over - each file is one candidate.
///
/// On disk (JSON):
/// {
///   "name"        : "baseline",
///   "variantsIni" : "variants/yeshybrid.ini",
///   "startFen"    : "4abtba3/...",            // optional, overrides the one in the ini
///   "flags"       : { "bloodied": false },
///   "overrides"   : { "customPiece9": "m:W" } // key/value replacements in [theyeshybrid:chess]
/// }
/// </summary>
public sealed class RuleSet
{
    [JsonPropertyName("name")]         public string Name { get; init; } = "unnamed";
    [JsonPropertyName("variantsIni")]  public string VariantsIni { get; init; } = "variants/yeshybrid.ini";
    [JsonPropertyName("startFen")]     public string? StartFen { get; init; }
    [JsonPropertyName("flags")]        public Dictionary<string, bool> Flags { get; init; } = new();
    [JsonPropertyName("overrides")]    public Dictionary<string, string> Overrides { get; init; } = new();

    /// <summary>
    /// Optional per-ruleset overrides of the global harness search params.
    /// When present these replace the sweep-wide <c>--depth</c> / <c>--max-plies</c>
    /// for this rule set only, so a single sweep can mix e.g. a depth-6
    /// baseline and a depth-8 probe and still share opening book + workers.
    /// </summary>
    [JsonPropertyName("searchDepth")]  public int? SearchDepth { get; init; }
    [JsonPropertyName("maxPlies")]     public int? MaxPlies { get; init; }

    [JsonIgnore] public bool Bloodied => Flags.TryGetValue("bloodied", out var v) && v;

    [JsonIgnore]
    public string EffectiveStartFen =>
        StartFen                             // explicit override in the ruleset JSON
        ?? ReadStartFenFromIni(VariantsIni)  // whatever the base INI declares
        ?? Variant.DefaultStartFen;          // last-resort fallback

    private static string? ReadStartFenFromIni(string iniPath)
    {
        if (!File.Exists(iniPath)) return null;
        foreach (var rawLine in File.ReadLines(iniPath))
        {
            var line = rawLine.TrimStart();
            if (line.StartsWith(";") || line.StartsWith("#") || line.Length == 0) continue;
            if (!line.StartsWith("startFen", StringComparison.Ordinal)) continue;
            int eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var val = line[(eq + 1)..];
            int semi = val.IndexOf(';'); // strip trailing ini-style comment
            if (semi >= 0) val = val[..semi];
            return val.Trim();
        }
        return null;
    }

    public static RuleSet Load(string path)
    {
        var json = File.ReadAllText(path);
        var rs = JsonSerializer.Deserialize<RuleSet>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        }) ?? throw new FormatException($"Empty rule set in '{path}'.");
        if (rs.Name == "unnamed") rs.Flags["__file"] = true; // hack: dummy flag just so it's mutable
        return rs;
    }

    /// <summary>
    /// Apply this rule set's overrides to the base variants.ini and write
    /// the merged file to a temp path.  Returns the temp path, which the
    /// caller must pass to <c>UciEngine.StartAsync(variantPath: ...)</c>.
    /// The file is left on disk so it can be inspected post-run.
    /// </summary>
    public string MaterializeVariantsFile()
    {
        var lines = File.ReadAllLines(VariantsIni).ToList();
        var remaining = new Dictionary<string, string>(Overrides);

        // Find the first section header; overrides apply to its body.
        int sectionStart = lines.FindIndex(l => l.TrimStart().StartsWith("["));
        int sectionEnd = lines.Count;
        if (sectionStart >= 0)
        {
            int next = lines.FindIndex(sectionStart + 1, l => l.TrimStart().StartsWith("["));
            if (next > 0) sectionEnd = next;
        }
        else
        {
            sectionStart = 0;
        }

        for (int i = sectionStart; i < sectionEnd; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith(";") || trimmed.StartsWith("#") || trimmed.Length == 0)
                continue;
            int eq = trimmed.IndexOf('=');
            if (eq <= 0) continue;
            var key = trimmed[..eq].Trim();
            if (remaining.TryGetValue(key, out var newVal))
            {
                lines[i] = $"{key} = {newVal}    ; [override by {Name}]";
                remaining.Remove(key);
            }
        }

        // Any override that didn't match an existing key -> append at the end
        // of the section.  Keeps the original file layout intact above.
        if (remaining.Count > 0)
        {
            var extras = new List<string> { "", $"; ---- Appended by ruleset '{Name}' ----" };
            foreach (var (k, v) in remaining)
                extras.Add($"{k} = {v}");
            lines.InsertRange(sectionEnd, extras);
        }

        // If the rule set pins a start FEN and the base ini has one, replace it.
        if (StartFen is { } sf)
        {
            int sfIdx = lines.FindIndex(l => l.TrimStart().StartsWith("startFen"));
            if (sfIdx >= 0) lines[sfIdx] = $"startFen = {sf}    ; [override by {Name}]";
        }

        var tmpDir = Path.Combine(Path.GetTempPath(), "yes-hybrid");
        Directory.CreateDirectory(tmpDir);
        var tmpPath = Path.Combine(tmpDir,
            $"variants-{SafeName(Name)}-{Environment.ProcessId}-{Guid.NewGuid():N}.ini");
        File.WriteAllLines(tmpPath, lines);
        return tmpPath;
    }

    private static string SafeName(string s)
    {
        var sb = new StringBuilder();
        foreach (var ch in s)
            sb.Append(char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_');
        return sb.ToString();
    }
}

using System.Runtime.InteropServices;

namespace YesHybrid.Desktop;

/// <summary>Locate engine binary and repo-root files; supports publish-time output layout.</summary>
internal static class RuntimePaths
{
    public static string? FindEngine()
    {
        var baseDir = AppContext.BaseDirectory;
        foreach (var p in BaseDirectoryEngineCandidates(baseDir))
        {
            if (File.Exists(p)) return Path.GetFullPath(p);
        }

        var names = new[] { "fairy-stockfish", "fairy-stockfish.exe" };
        foreach (var root in BaseSearchRoots())
        {
            foreach (var n in names)
            {
                var p = Path.Combine(root, "engine", n);
                if (File.Exists(p)) return Path.GetFullPath(p);
            }
        }
        return null;
    }

    private static IEnumerable<string> BaseDirectoryEngineCandidates(string baseDir)
    {
        var n = EngineBinaryName();
        string rid = RoundedRid();
        yield return Path.Combine(baseDir, "runtimes", rid, "native", n);
        yield return Path.Combine(baseDir, n);
        yield return Path.Combine(baseDir, "engine", n);
    }

    private static string EngineBinaryName() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "fairy-stockfish.exe" : "fairy-stockfish";

    /// <summary>Maps the current OS/CPU to a runtimes/ folder name for bundled single-file layouts.</summary>
    private static string RoundedRid()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux-x64";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "win-x64";
        return RuntimeInformation.RuntimeIdentifier;
    }

    public static string? FindYeshybridIni()
    {
        foreach (var root in BaseSearchRoots())
        {
            var p = Path.Combine(root, "variants", "yeshybrid.ini");
            if (File.Exists(p)) return Path.GetFullPath(p);
        }
        return null;
    }

    public static string? FindRulesMd()
    {
        foreach (var root in BaseSearchRoots())
        {
            var p = Path.Combine(root, "RULES.md");
            if (File.Exists(p)) return Path.GetFullPath(p);
        }
        return null;
    }

    /// <summary>Repo root (folder that has <c>variants/yeshybrid.ini</c> or <c>YesHybrid.sln</c>), for writing <c>reports/</c> debug files.</summary>
    public static string? TryGetRepoRoot()
    {
        foreach (var root in BaseSearchRoots())
        {
            if (File.Exists(Path.Combine(root, "variants", "yeshybrid.ini"))) return root;
            if (File.Exists(Path.Combine(root, "YesHybrid.sln"))) return root;
        }
        return null;
    }

    private static IEnumerable<string> BaseSearchRoots()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Add(string? d)
        {
            if (string.IsNullOrEmpty(d)) return;
            var full = Path.GetFullPath(d);
            if (seen.Add(full)) { /* collected */ }
        }

        Add(Environment.CurrentDirectory);
        Add(AppContext.BaseDirectory);

        // Walk up from CWD in case the app runs from bin/Debug/...
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        for (int i = 0; i < 10 && dir != null; i++)
        {
            Add(dir.FullName);
            dir = dir.Parent;
        }

        // Same from BaseDirectory
        try
        {
            dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 10 && dir != null; i++)
            {
                Add(dir.FullName);
                dir = dir.Parent;
            }
        }
        catch
        { /* invalid path */ }

        return seen;
    }
}

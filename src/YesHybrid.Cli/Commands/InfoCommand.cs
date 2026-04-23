using YesHybrid.Engine.Game;

namespace YesHybrid.Cli.Commands;

internal static class InfoCommand
{
    public static int Run(string[] args)
    {
        var o = Options.Parse(args);

        Console.WriteLine($"Variant     : {Variant.Name}");
        Console.WriteLine($"Board       : {Variant.Files} files x {Variant.Ranks} ranks");
        Console.WriteLine($"Variants ini: {o.VariantsPath}");
        Console.WriteLine($"Engine      : {o.EnginePath}  ({(File.Exists(o.EnginePath) ? "found" : "MISSING - run scripts/download-engine.sh")})");
        Console.WriteLine();

        Console.WriteLine("Pieces:");
        Console.WriteLine("  ROLE         SYM  SIDE   BETZA");
        foreach (var p in PieceCatalog.All)
        {
            var sideStr = p.Side == PieceCatalog.Side.White ? "Party" : "Horde";
            Console.WriteLine($"  {p.Name,-12} {p.Letter}    {sideStr,-6} {p.Betza}");
        }
        Console.WriteLine();

        var pos = Position.Parse(o.Fen, Variant.Files, Variant.Ranks);
        Console.WriteLine($"Starting FEN: {o.Fen}");
        Console.WriteLine();
        Console.Write(BoardRenderer.Render(pos));
        return 0;
    }
}

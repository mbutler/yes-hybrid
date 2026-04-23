using YesHybrid.Engine.Game;
using YesHybrid.Engine.Uci;

namespace YesHybrid.Cli.Commands;

internal static class BestMoveCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        var o = Options.Parse(args);

        await using var engine = await UciEngine.StartAsync(o.EnginePath, o.VariantsPath, Variant.Name);
        engine.Verbose = o.Verbose;

        await engine.SetPositionAsync(o.Fen);
        var move = await engine.GoBestMoveAsync(o.Depth, TimeSpan.FromMinutes(2));

        Console.WriteLine(move);
        return 0;
    }
}

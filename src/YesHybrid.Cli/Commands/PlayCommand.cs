using YesHybrid.Engine.Game;
using YesHybrid.Engine.Uci;

namespace YesHybrid.Cli.Commands;

internal static class PlayCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        var o = Options.Parse(args);

        if (o.Mode is not ("engine-vs-engine" or "human-vs-engine"))
        {
            Console.Error.WriteLine($"unknown --mode: {o.Mode}");
            return 2;
        }

        if (o.Mode == "human-vs-engine")
            return await RunHumanGameAsync(o);
        return await RunEngineGameAsync(o);
    }

    // ------------------------------------------------------------------ //
    //  Engine vs. engine - delegates to the shared GameLoop so batch     //
    //  mode and interactive self-play share the exact same rule engine.  //
    // ------------------------------------------------------------------ //
    private static async Task<int> RunEngineGameAsync(Options o)
    {
        await using var engine = await UciEngine.StartAsync(o.EnginePath, o.VariantsPath, Variant.Name);
        engine.Verbose = o.Verbose;

        var loop = new GameLoop(engine)
        {
            Depth = o.Depth,
            MaxPlies = o.MaxPlies,
            BloodiedEnabled = o.Bloodied,
            OnPly = info =>
            {
                if (o.Quiet) { Console.WriteLine($"{info.PlyIndex,4}. {info.Move}{(info.BloodiedTriggered ? $"  [{info.BloodiedFrom} bloodied]" : "")}"); return; }
                var pos = Position.Parse(info.Fen, Variant.Files, Variant.Ranks);
                Console.Clear();
                Console.WriteLine($"=== YES Hybrid  -  ply {info.PlyIndex}  ({o.Mode}{(o.Bloodied ? " +bloodied" : "")}) ===");
                if (info.BloodiedTriggered)
                    Console.WriteLine($"  >> {info.BloodiedFrom} was bloodied by {info.Move}");
                else
                    Console.WriteLine($"  Last move: {info.Move}");
                Console.WriteLine();
                Console.Write(BoardRenderer.Render(pos));
                if (o.PauseMs > 0) Thread.Sleep(o.PauseMs);
            },
        };

        var outcome = await loop.RunAsync(o.Fen);

        if (!o.Quiet)
        {
            Console.WriteLine();
            Console.WriteLine($"GAME OVER: {Describe(outcome.Result)} ({outcome.Termination}) in {outcome.Plies} plies.");
        }
        else
        {
            Console.WriteLine($"-> {GameLoop.ResultTag(outcome.Result)}  ({outcome.Termination}, {outcome.Plies} plies)");
        }

        if (o.PgnPath != null)
            WritePgn(o, outcome);

        return 0;
    }

    // ------------------------------------------------------------------ //
    //  Human vs. engine - the interactive path.  Keeps its own loop      //
    //  because GameLoop is fully autonomous (engine-driven).             //
    // ------------------------------------------------------------------ //
    private static async Task<int> RunHumanGameAsync(Options o)
    {
        await using var engine = await UciEngine.StartAsync(o.EnginePath, o.VariantsPath, Variant.Name);
        engine.Verbose = o.Verbose;

        var fen = o.Fen;
        var prevPos = Position.Parse(fen, Variant.Files, Variant.Ranks);
        char humanColor = o.HumanSide == "black" ? 'b' : 'w';
        var moves = new List<string>();

        for (int ply = 0; ply < o.MaxPlies; ply++)
        {
            await engine.SetPositionAsync(fen);

            var pos = Position.Parse(fen, Variant.Files, Variant.Ranks);
            Console.Clear();
            Console.WriteLine($"=== YES Hybrid  -  ply {ply + 1}  (human-vs-engine{(o.Bloodied ? " +bloodied" : "")}) ===\n");
            Console.Write(BoardRenderer.Render(pos));

            var outcome = pos.EvaluateOutcome();
            if (outcome != GameOutcome.Ongoing)
            {
                Console.WriteLine();
                Console.WriteLine($"GAME OVER: {DescribeOutcome(outcome)}");
                return 0;
            }

            string move;
            if (pos.SideToMove == humanColor)
            {
                move = await PromptHumanMoveAsync(engine);
                if (move is "quit" or "resign") { Console.WriteLine("Resigned."); return 0; }
            }
            else
            {
                Console.WriteLine($"\n  Engine thinking (depth {o.Depth})...");
                move = await engine.GoBestMoveAsync(o.Depth, TimeSpan.FromMinutes(2));
                if (move is "(none)" or "0000")
                {
                    var winner = pos.SideToMove == 'w'
                        ? "Horde (Black) wins  -  Party has no legal response."
                        : "Party (White) wins  -  Treasure is checkmated (or captured).";
                    Console.WriteLine($"GAME OVER: {winner}");
                    return 0;
                }
                Console.WriteLine($"  -> {move}");
                if (o.PauseMs > 0) await Task.Delay(o.PauseMs);
            }

            await engine.SetPositionAsync(fen, new[] { move });
            var newFen = await ReadFenAsync(engine) ?? fen;
            var afterPos = Position.Parse(newFen, Variant.Files, Variant.Ranks);

            if (o.Bloodied)
            {
                var r = BloodiedRule.Apply(prevPos, afterPos, move);
                if (r.Triggered)
                {
                    afterPos = r.NewPosition;
                    newFen = afterPos.ToFen();
                }
            }

            moves.Add(move);
            fen = newFen;
            prevPos = afterPos;
        }

        Console.WriteLine($"\nReached --max-plies={o.MaxPlies}; aborting.");
        return 0;
    }

    private static void WritePgn(Options o, GameLoop.Outcome outcome)
    {
        using var writer = PgnWriter.AppendTo(o.PgnPath!);
        var rec = new PgnWriter.GameRecord(
            StartFen: o.Fen,
            Moves: outcome.Moves,
            Result: GameLoop.ResultTag(outcome.Result),
            Termination: outcome.Termination,
            Tags: new Dictionary<string, string>
            {
                ["Depth"] = o.Depth.ToString(),
                ["Rules"] = o.Bloodied ? "bloodied" : "baseline",
            });
        writer.Write(rec);
    }

    private static async Task<string> PromptHumanMoveAsync(UciEngine engine)
    {
        var legal = await engine.GetLegalMovesAsync();
        Console.WriteLine();
        if (legal.Count == 0)
            Console.WriteLine("  (engine reports no legal moves)");
        else
        {
            Console.Write("  Legal moves: ");
            Console.WriteLine(string.Join(' ', legal.Take(40)));
            if (legal.Count > 40) Console.WriteLine($"  ...and {legal.Count - 40} more");
        }
        while (true)
        {
            Console.Write("  Your move (e.g. d1d3, or 'quit'): ");
            var input = Console.ReadLine()?.Trim() ?? "";
            if (input.Length == 0) continue;
            if (input is "quit" or "resign") return input;
            if (legal.Count == 0 || legal.Contains(input)) return input;
            Console.WriteLine($"  '{input}' is not a legal move.  Try again.");
        }
    }

    private static async Task<string?> ReadFenAsync(UciEngine engine)
    {
        await engine.SendAsync("d");
        await engine.SendAsync("isready");
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(3);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var line = await engine.WaitForAsync("Fen:", TimeSpan.FromMilliseconds(300));
                return line["Fen:".Length..].Trim();
            }
            catch (TimeoutException) { }
        }
        return null;
    }

    private static string Describe(GameLoop.GameResult r) => r switch
    {
        GameLoop.GameResult.PartyWins => "Party (White) wins",
        GameLoop.GameResult.HordeWins => "Horde (Black) wins",
        GameLoop.GameResult.Draw      => "draw",
        _                             => "unfinished",
    };

    private static string DescribeOutcome(GameOutcome o) => o switch
    {
        GameOutcome.PartyWinsByCapturingTreasure => "Party (White) wins  -  Treasure captured.",
        GameOutcome.HordeWinsByExtinction        => "Horde (Black) wins  -  Party wiped out.",
        _ => "Ongoing.",
    };
}

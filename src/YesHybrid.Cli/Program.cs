using YesHybrid.Cli.Commands;

namespace YesHybrid.Cli;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintHelp();
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        var rest = args.Skip(1).ToArray();

        try
        {
            return command switch
            {
                "play"     => await PlayCommand.RunAsync(rest),
                "batch"    => await BatchCommand.RunAsync(rest),
                "match"    => await MatchCommand.RunAsync(rest),
                "bestmove" => await BestMoveCommand.RunAsync(rest),
                "info"     => InfoCommand.Run(rest),
                _          => Unknown(command),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            if (Environment.GetEnvironmentVariable("YH_TRACE") == "1")
                Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static bool IsHelp(string s) =>
        s is "-h" or "--help" or "help";

    private static int Unknown(string cmd)
    {
        Console.Error.WriteLine($"unknown command: {cmd}");
        PrintHelp();
        return 2;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            yes-hybrid  -  Tactical wargame on top of Fairy-Stockfish

            USAGE:
              yes-hybrid <command> [options]

            COMMANDS:
              play         Run a single game in the terminal (engine vs engine, or human vs engine)
              batch        Run N quiet self-play games and append to a PGN file
              match        Evaluate one rule set (JSON) over N randomized-opening games
              bestmove     Print the engine's best move for a single position, then exit
              info         Print the variant definition (pieces, FEN, board layout)
              help         Show this help

            COMMON OPTIONS:
              --engine PATH       Path to fairy-stockfish binary  (default: ./engine/fairy-stockfish)
              --variants PATH     Path to variants.ini            (default: ./variants/yeshybrid.ini)
              --fen "FEN"         Override the starting position
              --depth N           Search depth                    (default: 8)
              --verbose           Echo all UCI traffic to stderr

            RULE OVERLAYS:
              --bloodied          Enable Section 6.1 Multi-Capture (Party pieces take two hits)

            'play' OPTIONS:
              --mode MODE         engine-vs-engine | human-vs-engine   (default: engine-vs-engine)
              --side {white|black}  Which side the human plays         (default: white)
              --max-plies N       Cap total plies                      (default: 400)
              --pause MS          Pause between engine moves           (default: 250)
              --quiet             Suppress board rendering (one line per ply)
              --pgn PATH          Append the game to a PGN file

            'batch' OPTIONS:
              --games N           How many games to run                (default: 1)
              --pgn PATH          Output PGN path (default: games/yeshybrid-<rules>-<stamp>.pgn)
              --max-plies N       Per-game ply cap                     (default: 400)

            'match' OPTIONS:
              --rules PATH        Rule-set JSON file (required)
              --games N           Games per match                      (default: 1; use 50+ for stats)
              --openings N        Distinct starting FENs to sample     (default: same as --games)
              --opening-plies K   Random plies from start per opening  (default: 6)
              --seed N            RNG seed for the opening book        (default: 0xC0FFEE)
              --pgn PATH          Append every game to this PGN file

            EXAMPLES:
              yes-hybrid info
              yes-hybrid play
              yes-hybrid play --mode human-vs-engine --side white --depth 10
              yes-hybrid play --bloodied --pgn games/sample.pgn
              yes-hybrid batch --games 20 --depth 6 --bloodied
              yes-hybrid match --rules rulesets/baseline.json --games 50 --depth 6
              yes-hybrid bestmove --depth 12
            """);
    }
}

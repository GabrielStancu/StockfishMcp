namespace Stockfish.McpServer;

public class McpToolsService
{
    public static IEnumerable<McpTool> Tools => new[]
    {
        new McpTool(
            Name: "evaluate_position",
            Description: "Evaluate a chess position using Stockfish",
            InputSchema: new
            {
                type = "object",
                properties = new
                {
                    fen = new { type = "string" },
                    depth = new { type = "integer", defaultValue = 15 }
                },
                required = new[] { "fen" }
            }
        ),
        new McpTool(
            Name: "best_move",
            Description: "Find the best move in a given position using Stockfish",
            InputSchema: new
            {
                type = "object",
                properties = new
                {
                    fen = new { type = "string" },
                    depth = new { type = "integer", defaultValue = 15 }
                },
                required = new[] { "fen" }
            }
        ),
        new McpTool(
            Name: "evaluate_move",
            Description: "Evaluate a specific move in a given position",
            InputSchema: new
            {
                type = "object",
                properties = new
                {
                    fen = new { type = "string" },
                    move = new { type = "string" },
                    depth = new { type = "integer", defaultValue = 15 }
                },
                required = new[] { "fen", "move" }
            }
        )
    };

    public static string RequireString(Dictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value))
            throw new ArgumentException($"Missing or invalid argument: {key}");

        return value?.ToString() ?? string.Empty;
    }

    public static int GetIntOrDefault(Dictionary<string, object> args, string key, int defaultValue)
    {
        if (!args.TryGetValue(key, out var value))
            return defaultValue;

        return value switch
        {
            int i => i,
            long l => (int)l,
            _ => defaultValue
        };
    }

    public static object HandleEvaluatePosition(Dictionary<string, object> args, 
        StockfishClient stockfish, int defaultDepth)
    {
        var fen = RequireString(args, "fen");
        var depth = GetIntOrDefault(args, "depth", defaultDepth);
        var eval = stockfish.EvaluatePosition(fen, depth);

        return new
        {
            fen,
            depth,
            evaluation = new
            {
                centipawns = eval.Centipawns,
                mateIn = eval.MateIn
            },
            bestMove = eval.BestMove
        };
    }


    public static object HandleBestMove(Dictionary<string, object> args,
        StockfishClient stockfish, int defaultDepth)
    {
        var fen = RequireString(args, "fen");
        var depth = GetIntOrDefault(args, "depth", defaultDepth);
        var bestMove = stockfish.GetBestMove(fen, depth);

        return new
        {
            fen,
            depth,
            bestMove
        };
    }


    public static object HandleEvaluateMove(Dictionary<string, object> args,
        StockfishClient stockfish, int defaultDepth)
    {
        var fen = RequireString(args, "fen");
        var move = RequireString(args, "move");
        var depth = GetIntOrDefault(args, "depth", defaultDepth);
        var eval = stockfish.EvaluateMove(fen, move, depth);

        return new
        {
            fen,
            move,
            depth,
            evaluation = new
            {
                centipawns = eval.Centipawns,
                mateIn = eval.MateIn
            },
            bestMove = eval.BestMove
        };
    }

}

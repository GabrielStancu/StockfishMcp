using System.Diagnostics;

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

    public static async Task<object> HandleEvaluatePositionAsync(Dictionary<string, object> args,
        StockfishPool pool, int defaultDepth, int defaultMoveTimeMs, ILogger logger)
    {
        var fen = RequireString(args, "fen");
        var depth = GetOptionalInt(args, "depth");
        var moveTimeMs = GetOptionalInt(args, "moveTimeMs") ?? defaultMoveTimeMs;

        logger.LogInformation("Received evaluate_position request: {Fen} Depth={Depth} MoveTime={MoveTime}",
            fen, depth, moveTimeMs);

        var sw = Stopwatch.StartNew();
        var result = await pool.UseAsync(async engine =>
        {
            var eval = await engine.EvaluatePositionAsync(fen, depth, moveTimeMs);
            return new
            {
                fen,
                depth,
                moveTimeMs,
                evaluation = new
                {
                    centipawns = eval.Centipawns,
                    mateIn = eval.MateIn
                },
                bestMove = eval.BestMove
            };
        });

        sw.Stop();
        logger.LogInformation("evaluate_position completed in {ElapsedMs}ms: BestMove={BestMove} Centipawns={Centipawns}",
            sw.ElapsedMilliseconds, result.bestMove, result.evaluation.centipawns);

        return result;
    }


    public static async Task<object> HandleBestMoveAsync(Dictionary<string, object> args,
        StockfishPool pool, int defaultDepth, int defaultMoveTimeMs, ILogger logger)
    {
        var fen = RequireString(args, "fen");
        var depth = GetOptionalInt(args, "depth") ?? defaultDepth;
        var moveTimeMs = GetOptionalInt(args, "moveTimeMs") ?? defaultMoveTimeMs;

        logger.LogInformation("Received best_move request: {Fen} Depth={Depth} MoveTime={MoveTime}",
            fen, depth, moveTimeMs);

        var sw = Stopwatch.StartNew();
        var result = await pool.UseAsync(async engine =>
        {
            var bestMove = await engine.GetBestMoveAsync(fen, depth, moveTimeMs);
            return new
            {
                fen,
                depth,
                moveTimeMs,
                bestMove
            };
        });

        sw.Stop();
        logger.LogInformation("best_move completed in {ElapsedMs}ms: BestMove={BestMove}",
            sw.ElapsedMilliseconds, result);

        return result;
    }


    public static async Task<object> HandleEvaluateMoveAsync(Dictionary<string, object> args,
        StockfishPool pool, int defaultDepth, int defaultMoveTimeMs, ILogger logger)
    {
        var fen = RequireString(args, "fen");
        var move = RequireString(args, "move");
        var depth = GetOptionalInt(args, "depth") ?? defaultDepth;
        var moveTimeMs = GetOptionalInt(args, "moveTimeMs") ?? defaultMoveTimeMs;

        logger.LogInformation("Received evaluate_move request: {Fen} Move={Move} Depth={Depth} MoveTime={MoveTime}",
            fen, move, depth, moveTimeMs);

        var sw = Stopwatch.StartNew();
        var result = await pool.UseAsync(async engine =>
        {
            var eval = await engine.EvaluateMoveAsync(fen, move, depth, moveTimeMs);
            return new
            {
                fen,
                move,
                depth,
                moveTimeMs,
                evaluation = new
                {
                    centipawns = eval.Centipawns,
                    mateIn = eval.MateIn
                },
                bestMove = eval.BestMove
            };
        });

        sw.Stop();
        logger.LogInformation("evaluate_move completed in {ElapsedMs}ms: Move={Move} Centipawns={Centipawns} MateIn={MateIn} BestMove={BestMove}",
            sw.ElapsedMilliseconds, move, result.evaluation.centipawns, result.evaluation.mateIn, result.bestMove);

        return result;
    }

    private static string RequireString(Dictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value))
            throw new ArgumentException($"Missing or invalid argument: {key}");

        return value?.ToString() ?? string.Empty;
    }

    private static int? GetOptionalInt(Dictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value))
            return null;

        var parsed = int.TryParse(value.ToString(), out var intValue);

        return parsed ? intValue : null;
    }

}

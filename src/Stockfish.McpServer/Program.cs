using Stockfish.McpServer;

var builder = WebApplication.CreateBuilder(args);

// Add structured logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole(); // logs to console in structured format
builder.Logging.AddDebug();   // optional
builder.Logging.SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

var stockfishPath = builder.Configuration["Stockfish:Path"]
                    ?? throw new InvalidOperationException("Stockfish path not configured");
var defaultDepth = int.Parse(builder.Configuration["Stockfish:DefaultDepth"] ?? "15");
var defaultMoveTimeMs = int.Parse(builder.Configuration["Stockfish:DefaultMoveTimeMs"] ?? "1000");
var poolSize = int.Parse(builder.Configuration["Stockfish:PoolSize"] ?? "2");
var stockfishPool = new StockfishPool(stockfishPath, poolSize);

app.MapGet("/tools", () => Results.Ok(McpToolsService.Tools));
app.MapPost("/call", async (HttpContext context, ILogger<Program> logger) =>
{
    try
    {
        var request = await context.Request.ReadFromJsonAsync<McpCallRequest>();
        if (request is null)
            return Results.BadRequest(new { error = "Invalid request body" });

        var result = request.Tool switch
        {
            "evaluate_position" => await
                McpToolsService.HandleEvaluatePositionAsync(request.Arguments, stockfishPool, defaultDepth, defaultMoveTimeMs, logger),

            "best_move" => await
                McpToolsService.HandleBestMoveAsync(request.Arguments, stockfishPool, defaultDepth, defaultMoveTimeMs, logger),

            "evaluate_move" => await 
                McpToolsService.HandleEvaluateMoveAsync(request.Arguments, stockfishPool, defaultDepth, defaultMoveTimeMs, logger),

            _ => throw new ArgumentException($"Unknown tool: {request.Tool}")
        };


        return Results.Ok(new McpCallResponse(result));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new
        {
            error = ex.Message
        });
    }
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    stockfishPool.Dispose();
});


app.Run();

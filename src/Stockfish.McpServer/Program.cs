using Stockfish.McpServer;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var stockfishPath = builder.Configuration["Stockfish:Path"]
                    ?? throw new InvalidOperationException("Stockfish path not configured");
var defaultDepth = int.Parse(builder.Configuration["Stockfish:DefaultDepth"] ?? "15");
var stockfish = new StockfishClient(stockfishPath);

app.MapGet("/tools", () => Results.Ok(McpToolsService.Tools));
app.MapPost("/call", async (HttpContext context) =>
{
    try
    {
        var request = await context.Request.ReadFromJsonAsync<McpCallRequest>();
        if (request is null)
            return Results.BadRequest(new { error = "Invalid request body" });

        var result = request.Tool switch
        {
            "evaluate_position" =>
                McpToolsService.HandleEvaluatePosition(request.Arguments, stockfish, defaultDepth),

            "best_move" =>
                McpToolsService.HandleBestMove(request.Arguments, stockfish, defaultDepth),

            "evaluate_move" =>
                McpToolsService.HandleEvaluateMove(request.Arguments, stockfish, defaultDepth),

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
    stockfish.Dispose();
});


app.Run();

namespace Stockfish.McpServer;

public record McpCallRequest(string Tool, Dictionary<string, object> Arguments);

public record McpCallResponse(object Result);
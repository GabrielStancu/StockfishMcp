namespace Stockfish.McpServer;

public sealed record StockfishEvaluation(int? Centipawns, int? MateIn, string? BestMove);
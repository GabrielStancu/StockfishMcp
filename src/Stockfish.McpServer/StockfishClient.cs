using System.Diagnostics;

namespace Stockfish.McpServer;

public sealed class StockfishClient : IDisposable
{
    private readonly Process _process;
    private readonly StreamWriter _input;
    private readonly StreamReader _output;

    public StockfishClient(string path)
    {
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = path,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _process.Start();

        _input = _process.StandardInput;
        _output = _process.StandardOutput;

        Initialize();
    }

    private void Initialize()
    {
        Send("uci");
        WaitFor("uciok");
        Send("isready");
        WaitFor("readyok");
    }

    public StockfishEvaluation EvaluatePosition(string fen, int depth)
    {
        Send("ucinewgame");
        Send($"position fen {fen}");
        Send($"go depth {depth}");

        return ReadEvaluation();
    }

    public StockfishEvaluation EvaluateMove(string fen, string move, int depth)
    {
        Send("ucinewgame");
        Send($"position fen {fen} moves {move}");
        Send($"go depth {depth}");

        return ReadEvaluation();
    }

    public string GetBestMove(string fen, int depth)
    {
        Send("ucinewgame");
        Send($"position fen {fen}");
        Send($"go depth {depth}");

        return ReadEvaluation().BestMove;
    }

    private StockfishEvaluation ReadEvaluation()
    {
        string? line;
        int? scoreCp = null;
        int? mate = null;
        string? bestMove = null;

        while ((line = _output.ReadLine()) != null)
        {
            if (line.StartsWith("info"))
            {
                if (line.Contains("score cp"))
                {
                    var parts = line.Split(' ');
                    var idx = Array.IndexOf(parts, "cp");
                    if (idx >= 0 && int.TryParse(parts[idx + 1], out var cp))
                        scoreCp = cp;
                }

                if (line.Contains("score mate"))
                {
                    var parts = line.Split(' ');
                    var idx = Array.IndexOf(parts, "mate");
                    if (idx >= 0 && int.TryParse(parts[idx + 1], out var m))
                        mate = m;
                }
            }

            if (line.StartsWith("bestmove"))
            {
                bestMove = line.Split(' ')[1];
                break;
            }
        }

        return new StockfishEvaluation(scoreCp, mate, bestMove);
    }

    private void Send(string command)
    {
        _input.WriteLine(command);
        _input.Flush();
    }

    private void WaitFor(string expected)
    {
        string? line;
        while ((line = _output.ReadLine()) != null)
        {
            if (line.Contains(expected))
                return;
        }
    }

    public void Dispose()
    {
        try
        {
            Send("quit");
            _process.Kill();
        }
        catch { }
    }
}
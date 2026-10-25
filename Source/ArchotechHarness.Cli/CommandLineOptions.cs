namespace ArchotechHarness.Cli;

/// <summary>Splits archo arguments into positionals, --flags and --name value options.</summary>
public sealed class CommandLineOptions
{
    private static readonly HashSet<string> Flags = ["--keep-open", "--problems", "--attach", "--wait"];

    private readonly List<string> _positionals = [];
    private readonly List<(string Name, string Value)> _values = [];
    private readonly HashSet<string> _flags = [];

    public CommandLineOptions(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (Flags.Contains(args[i]))
            {
                _flags.Add(args[i]);
            }
            else if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                if (i + 1 >= args.Length)
                {
                    throw new ScenarioException($"{args[i]} needs a value.");
                }

                _values.Add((args[i], args[++i]));
            }
            else
            {
                _positionals.Add(args[i]);
            }
        }
    }

    public bool Flag(string name) => _flags.Contains(name);

    public string? Value(string name) => _values.LastOrDefault(v => v.Name == name).Value;

    public IReadOnlyList<string> All(string name) => _values.Where(v => v.Name == name).Select(v => v.Value).ToList();

    public string Positional(int index, string what) =>
        PositionalOrNull(index) ?? throw new ScenarioException($"Missing {what}.");

    public IReadOnlyList<string> Positionals => _positionals;

    public string? PositionalOrNull(int index) => index < _positionals.Count ? _positionals[index] : null;

    public int Port() => Value("--port") is { } port ? int.Parse(port) : LaunchOptions.DefaultPort;

    public WindowSize Window()
    {
        var parts = (Value("--window") ?? "1600x900").Split('x');
        return new WindowSize(int.Parse(parts[0]), int.Parse(parts[1]));
    }
}

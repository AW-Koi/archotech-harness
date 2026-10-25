namespace DevHarness.Cli;

/// <summary>A compiled script assembly, its portable PDB and the type holding the entry method.</summary>
public sealed record CompiledScript(string AssemblyName, string EntryType, byte[] Assembly, byte[] Symbols);

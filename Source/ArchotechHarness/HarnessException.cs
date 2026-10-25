using System;

namespace ArchotechHarness;

/// <summary>A command the harness rejects because of its arguments or the game state.</summary>
public class HarnessException(string message) : Exception(message);

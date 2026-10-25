using System;

namespace DevHarness.Scripting;

/// <summary>A Require check that failed and stopped the script.</summary>
public sealed class ScriptCheckFailedException(string check) : Exception($"Required check failed: {check}");

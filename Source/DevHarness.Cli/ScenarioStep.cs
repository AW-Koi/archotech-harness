using System.Text.Json.Nodes;

namespace DevHarness.Cli;

/// <summary>One command of a scenario, with the runner options split from the command arguments.</summary>
/// <remarks>Steps marked Always still run after an earlier step fails.</remarks>
public sealed record ScenarioStep(
    string Command,
    JsonObject Arguments,
    string? SaveAs,
    StepExpectation? Expectation,
    bool Optional,
    bool Always);

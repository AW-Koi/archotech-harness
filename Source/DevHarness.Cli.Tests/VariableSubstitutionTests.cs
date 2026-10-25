using System.Text.Json.Nodes;

namespace DevHarness.Cli.Tests;

public class VariableSubstitutionTests
{
    private const string BeforeResult = """{"count":12,"pawn":"Mopper"}""";

    [Fact]
    public void Substitution_WholeStringReference_KeepsTheValueType()
    {
        var variables = new Dictionary<string, JsonNode?> { ["before"] = JsonNode.Parse(BeforeResult) };
        var args = JsonNode.Parse("""{"value":"${before.count}"}""")!;

        var substituted = VariableSubstitution.Apply(args, variables);

        Assert.Equal(12, substituted!["value"]!.GetValue<int>());
    }

    [Fact]
    public void Substitution_EmbeddedReference_InterpolatesText()
    {
        var variables = new Dictionary<string, JsonNode?> { ["before"] = JsonNode.Parse(BeforeResult) };
        var args = JsonNode.Parse("""{"path":"shots/${before.pawn}-before.png"}""")!;

        var substituted = VariableSubstitution.Apply(args, variables);

        Assert.Equal("shots/Mopper-before.png", substituted!["path"]!.GetValue<string>());
    }

    [Fact]
    public void Substitution_UnknownVariable_Throws()
    {
        var args = JsonNode.Parse("""{"value":"${missing.count}"}""")!;

        Assert.Throws<ScenarioException>(() => VariableSubstitution.Apply(args, new Dictionary<string, JsonNode?>()));
    }
}

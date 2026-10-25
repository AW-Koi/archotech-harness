using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;

namespace ArchotechHarness.Cli;

/// <summary>Sends one JSON command line to the harness mod and reads the response line.</summary>
public sealed class HarnessClient(int port)
{
    private int _nextId = 1;

    public JsonObject Send(string command, JsonObject? arguments = null)
    {
        var request = arguments != null ? (JsonObject)arguments.DeepClone() : new JsonObject();
        request["cmd"] = command;
        request["id"] = _nextId++;
        var timeout = request["timeoutSeconds"]?.GetValue<double>() ?? 30;

        using var client = new TcpClient();
        client.ReceiveTimeout = (int)TimeSpan.FromSeconds(timeout + 30).TotalMilliseconds;
        client.Connect("127.0.0.1", port);
        using var stream = client.GetStream();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        writer.NewLine = "\n";
        writer.AutoFlush = true;
        using var reader = new StreamReader(stream, new UTF8Encoding(false));
        writer.WriteLine(request.ToJsonString());
        var line = reader.ReadLine() ?? throw new IOException("The harness closed the connection without a response.");
        return JsonNode.Parse(line)!.AsObject();
    }

    public bool TryPing()
    {
        try
        {
            return Send("ping")["ok"]?.GetValue<bool>() == true;
        }
        catch (Exception e) when (e is SocketException or IOException)
        {
            return false;
        }
    }
}

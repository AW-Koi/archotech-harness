using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ArchotechHarness.Server;

/// <summary>Accepts newline-delimited JSON commands on a loopback TCP port.</summary>
/// <remarks>Each request line gets one response line carrying the same "id".</remarks>
public static class CommandServer
{
    private const double DefaultTimeoutSeconds = 30;

    public static void Start(int port)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        _ = Task.Run(() => AcceptLoop(listener));
    }

    private static async Task AcceptLoop(TcpListener listener)
    {
        while (true)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => Serve(client));
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception e)
            {
                Verse.Log.Warning($"[ArchotechHarness] Accept failed: {e.Message}");
            }
        }
    }

    private static async Task Serve(TcpClient client)
    {
        using (client)
        {
            var stream = client.GetStream();
            using var reader = new StreamReader(stream, new UTF8Encoding(false));
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.AutoFlush = true;
            writer.NewLine = "\n";
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.Trim().Length == 0)
                {
                    continue;
                }

                var response = await Handle(line);
                await writer.WriteLineAsync(response.ToJsonString());
            }
        }
    }

    private static async Task<JsonObject> Handle(string line)
    {
        JsonObject request;
        try
        {
            request = JsonNode.Parse(line) as JsonObject ?? throw new JsonException("The request must be a JSON object.");
        }
        catch (JsonException e)
        {
            return new JsonObject { ["ok"] = false, ["error"] = e.Message, ["errorType"] = "BadRequest" };
        }

        var id = request["id"]?.DeepClone();
        var name = request["cmd"]?.GetValue<string>();
        JsonObject response;
        if (string.IsNullOrEmpty(name))
        {
            response = new JsonObject { ["ok"] = false, ["error"] = "Missing \"cmd\".", ["errorType"] = "BadRequest" };
        }
        else if (name == "ping")
        {
            response = new JsonObject { ["ok"] = true, ["result"] = new JsonObject { ["pid"] = Environment.ProcessId } };
        }
        else
        {
            var timeout = request["timeoutSeconds"] is { } requested ? Commands.Arguments.Number(requested) : DefaultTimeoutSeconds;
            var command = new PendingCommand(name, request, DateTime.UtcNow.AddSeconds(timeout + 5));
            MainThreadDispatcher.Enqueue(command);
            var finished = await Task.WhenAny(command.Response.Task, Task.Delay(TimeSpan.FromSeconds(timeout + 10)));
            response = finished == command.Response.Task
                ? command.Response.Task.Result
                : new JsonObject { ["ok"] = false, ["error"] = $"'{name}' got no answer from the main thread.", ["errorType"] = "Timeout" };
        }

        response["id"] = id;
        return response;
    }
}

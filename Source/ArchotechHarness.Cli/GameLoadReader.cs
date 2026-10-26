using System.Net.Sockets;

namespace ArchotechHarness.Cli;

/// <summary>Reads the errors a game logged while it loaded and set up its first colony, and blames each on a mod under test where it can.</summary>
public sealed class GameLoadReader(SessionManager sessions, Action<string> progress)
{
    public GameLoad Read(string group, IEnumerable<ScenarioMod> modsUnderTest, SessionState session)
    {
        var fingerprints = modsUnderTest.Select(m => ModFingerprint.Read(m.Directory, sessions.InstalledDirectory(m.Directory))).ToList();
        return GameLoad.FromLog(group, ReadLoggedErrors(session), fingerprints);
    }

    private IReadOnlyList<string> ReadLoggedErrors(SessionState session)
    {
        try
        {
            var response = new HarnessClient(session.Port).Send("loadErrors");
            if (response["ok"]?.GetValue<bool>() == true)
            {
                return response["result"]?["errors"]?.AsArray().Select(e => e?.ToString()).OfType<string>().ToList() ?? [];
            }

            progress($"Could not read the game's load errors: {response["error"]}");
        }
        catch (Exception e) when (e is IOException or SocketException)
        {
            progress($"Could not read the game's load errors: {e.Message}");
        }

        return [];
    }
}

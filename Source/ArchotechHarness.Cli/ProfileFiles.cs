using System.Xml.Linq;

namespace ArchotechHarness.Cli;

/// <summary>Writes the ModsConfig.xml and Prefs.xml of the isolated harness profile.</summary>
public static class ProfileFiles
{
    public static void WriteModsConfig(string path, IReadOnlyList<string> activeMods, string gameVersion)
    {
        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("ModsConfigData",
                new XElement("version", gameVersion),
                new XElement("activeMods", activeMods.Select(id => new XElement("li", id))),
                new XElement("knownExpansions", Dlc.All.Select(d => new XElement("li", d.PackageId)))));
        Save(document, path);
    }

    public static void WritePrefs(string path, WindowSize window)
    {
        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("PrefsData",
                new XElement("volumeMaster", 0),
                new XElement("screenWidth", window.Width),
                new XElement("screenHeight", window.Height),
                new XElement("fullscreen", "False"),
                new XElement("runInBackground", "True"),
                new XElement("edgeScreenScroll", "False"),
                new XElement("pauseOnLoad", "False"),
                new XElement("automaticPauseMode", "Never"),
                new XElement("autosaveIntervalDays", 60),
                new XElement("devMode", "True"),
                new XElement("logVerbose", "False"),
                new XElement("openLogOnWarnings", "False"),
                new XElement("resetModsConfigOnCrash", "False"),
                new XElement("disableQuickStartCryptoSickness", "True"),
                new XElement("adaptiveTrainingEnabled", "False"),
                new XElement("langFolderName", "English")));
        Save(document, path);
    }

    private static void Save(XDocument document, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        document.Save(path);
    }
}

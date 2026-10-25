using ArchotechHarness.Commands;
using ArchotechHarness.Scripting;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using UnityEngine;
using Verse;

namespace ArchotechHarness.Debugger;

/// <summary>Draws the scenario debugger, a Dear ImGui window over the game listing the steps with Pause, Resume and Step.</summary>
/// <remarks>The game's ImGui host feeds ImGui no input, so the panel feeds it the mouse itself.</remarks>
public static class DebugPanel
{
    private const float Width = 440;
    private const float Margin = 12;
    private const float TopOffset = 80;

    private static readonly float4 PassedColour = new(0.35f, 0.85f, 0.45f, 1f);
    private static readonly float4 FailedColour = new(0.95f, 0.35f, 0.3f, 1f);
    private static readonly float4 QuietColour = new(0.65f, 0.65f, 0.65f, 1f);
    private static readonly float4 PausedColour = new(1f, 0.75f, 0.25f, 1f);

    private static int _stepsShown;

    /// <summary>The panel's area on screen in UI units, or an empty rect when it is hidden.</summary>
    public static Rect ScreenRect { get; private set; }

    /// <summary>Draws the panel in the repaint pass while a scenario that asked for it runs and no screenshot is pending.</summary>
    public static void OnGUI()
    {
        var session = ScriptSession.Current;
        if (session is not { ShowPanel: true } || ScreenshotCommand.Pending > 0)
        {
            ScreenRect = Rect.zero;
            return;
        }

        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        var io = ImGui.GetIO();
        FeedMouse(io);
        var display = io.DisplaySize;
        var position = new float2(Margin, TopOffset);
        var size = new float2(Width, display.Y * 0.6f);
        ImGui.SetNextWindowPos(in position, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(in size, ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Scenario debugger", ImGuiWindowFlags.NoCollapse))
        {
            DrawHeader(session);
            DrawSteps(session);
        }

        var at = ImGui.GetWindowPos();
        var extent = ImGui.GetWindowSize();
        ImGui.End();
        var scale = Prefs.UIScale;
        ScreenRect = new Rect(at.X / scale, at.Y / scale, extent.X / scale, extent.Y / scale);
        PanelBlocker.Follow(ScreenRect);
    }

    private static void FeedMouse(ImGuiIOPtr io)
    {
        var mouse = Input.mousePosition;
        io.AddMousePosEvent(mouse.x, io.DisplaySize.Y - mouse.y);
        io.AddMouseButtonEvent(0, Input.GetMouseButton(0));
        io.AddMouseWheelEvent(0, Input.mouseScrollDelta.y);
    }

    private static void DrawHeader(ScriptSession session)
    {
        ImGui.Text(session.ScenarioName ?? "Scenario");
        var control = session.Control;
        if (control.Paused)
        {
            ImGui.TextColored(in PausedColour, "Paused: you have the game");
        }
        else
        {
            ImGui.Text(control.Stepping ? "Stepping to the next await" : "Running");
        }

        ImGui.BeginDisabled(control.Paused);
        if (ImGui.Button("Pause"))
        {
            session.PauseScript();
        }

        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.BeginDisabled(!control.Paused);
        if (ImGui.Button("Resume"))
        {
            session.ResumeScript();
        }

        ImGui.SameLine();
        if (ImGui.Button("Step"))
        {
            session.StepScript();
        }

        ImGui.EndDisabled();
        ImGui.Separator();
    }

    private static void DrawSteps(ScriptSession session)
    {
        if (!ImGui.BeginChild("steps"))
        {
            ImGui.EndChild();
            return;
        }

        var steps = session.Timeline.Steps;
        var waiting = session.Waiting;
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var current = waiting.Count == 0 && i == steps.Count - 1;
            if (current)
            {
                ImGui.PushID(i);
                ImGui.Selectable(step.Label, true);
                ImGui.PopID();
            }
            else if (step.Passed is { } passed)
            {
                var colour = passed ? PassedColour : FailedColour;
                ImGui.TextColored(in colour, step.Label);
            }
            else
            {
                ImGui.TextColored(in QuietColour, step.Label);
            }
        }

        for (var i = 0; i < waiting.Count; i++)
        {
            ImGui.PushID(-1 - i);
            ImGui.Selectable($"waiting: {waiting[i]}", true);
            ImGui.PopID();
        }

        var shown = steps.Count + waiting.Count;
        if (shown != _stepsShown && ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 4)
        {
            ImGui.SetScrollHereY(1f);
        }

        _stepsShown = shown;
        ImGui.EndChild();
    }
}

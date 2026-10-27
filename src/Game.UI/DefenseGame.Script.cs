using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TowerGame.Rendering;

namespace TowerGame.UI;

/// <summary>
/// Scripted UI test runner (--script=file). Feeds taps and keys into the same input path widgets
/// use, so it exercises real layout, hit testing, dialogs and persistence inside the native window
/// without depending on OS focus. Results go to "&lt;script&gt;.log"; the exit code is 1 on failure.
/// Commands: wait N | tap X Y | cell X Y | key Name | cancel | back | zoom X Y F | background |
/// shot FILE | expect WHAT VALUE | quit. Coordinates are logical (720 units high).
/// </summary>
public sealed partial class DefenseGame
{
    readonly List<string[]> script = [];
    readonly List<string> scriptLog = [];
    int scriptLine, updates, waitUntil;
    bool scriptFailed, scriptLoaded;
    string? pendingShot;
    string? waitingState;
    int waitStateDeadline;

    void RunScript()
    {
        if (options.Script == null || screen == Screen.Loading) return;
        if (!scriptLoaded)
        {
            scriptLoaded = true;
            foreach (var raw in File.ReadAllLines(options.Script))
            {
                var line = raw.Trim();
                if (line.Length > 0 && !line.StartsWith('#')) script.Add(line.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }
            waitUntil = 3;
        }
        updates++;
        while (updates >= waitUntil)
        {
            if (scriptLine >= script.Count) { FinishScript(); return; }
            var c = script[scriptLine++];
            float Num(int i) => float.Parse(c[i], CultureInfo.InvariantCulture);
            switch (c[0])
            {
                case "wait": waitUntil = updates + int.Parse(c[1]); return;
                case "wait_state":
                    if (session?.State.ToString() == c[1]) { waitingState = null; break; }
                    if (waitingState != c[1]) { waitingState = c[1]; waitStateDeadline = updates + int.Parse(c[2]); }
                    if (updates >= waitStateDeadline)
                    {
                        Log($"FAIL line {scriptLine}: timed out waiting for state {c[1]}, got {session?.State.ToString() ?? "none"}");
                        scriptFailed = true; waitingState = null; break;
                    }
                    scriptLine--; waitUntil = updates + 1; return;
                case "tap": input.InjectTap(new Vector2(Num(1), Num(2))); waitUntil = updates + 3; return;
                case "cell":
                    if (session is { } s)
                    {
                        ComputeBattleLayout();
                        var (size, origin) = Battlefield.Layout(s.Map, board, zoom, pan);
                        input.InjectTap(origin + new Vector2(Num(1) + .5f, Num(2) + .5f) * size);
                    }
                    waitUntil = updates + 3; return;
                case "press":
                    var name = string.Join(' ', c.Skip(1));
                    if (name.StartsWith('@')) name = Strings.T(name[1..]);
                    if (widgets.TryGetValue(name, out var rect)) input.InjectTap(new Vector2(rect.Center.X, rect.Center.Y));
                    else { scriptFailed = true; Log($"FAIL line {scriptLine}: no widget '{name}' on screen {screen}"); }
                    waitUntil = updates + 3; return;
                case "key": input.InjectKey(Enum.Parse<Keys>(c[1])); waitUntil = updates + 3; return;
                case "cancel": input.InjectCancel(); waitUntil = updates + 3; return;
                case "back": input.InjectBack(); waitUntil = updates + 3; return;
                case "zoom": input.InjectZoom(new Vector2(Num(1), Num(2)), Num(3)); waitUntil = updates + 3; return;
                case "background": OnBackground(force: true); waitUntil = updates + 3; return;
                case "shot": pendingShot = c[1]; waitUntil = updates + 3; return;
                case "expect" when c[1] == "key": Check("key " + c[2], Settings.Keys.GetValueOrDefault(c[2], ""), c[3]); break;
                case "expect": Expect(c[1], string.Join(' ', c.Skip(2))); break;
                case "quit": FinishScript(); return;
                default: Log($"FAIL line {scriptLine}: unknown command {c[0]}"); scriptFailed = true; break;
            }
        }
    }

    void Expect(string what, string expected)
    {
        if (expected.StartsWith('@')) expected = Strings.T(expected[1..]);
        var tower = session?.Towers.Find(t => t.Id == selectedTower);
        var actual = what switch
        {
            "screen" => screen.ToString(),
            "focus" => focusedWidget ?? "none",
            "touch_targets" => !host.IsTouch ? "not-touch" : focusWidgets.Values.All(r => r.Width >= hit && r.Height >= hit).ToString(),
            "dialog" => dialog.ToString(),
            "state" => session?.State.ToString() ?? "none",
            "towers" => (session?.Towers.Count ?? -1).ToString(),
            "money" => (session?.Money ?? -1).ToString(),
            "lives" => (session?.Lives ?? -1).ToString(),
            "wave" => (session?.Wave ?? -1).ToString(),
            "level" => tower?.Level.ToString() ?? "none",
            "priority" => tower?.Priority.ToString() ?? "none",
            "build" => buildKind?.ToString() ?? "none",
            "selected" => (tower != null).ToString(),
            "fast" => fast.ToString(),
            "saved" => store.HasSavedBattle.ToString(),
            "language" => Settings.Language,
            "path" => Settings.ShowPath.ToString(),
            "zoomed" => (zoom > 1.01f).ToString(),
            "toast" => toastTime > 0 ? toast : "",
            _ => $"<unknown {what}>"
        };
        Check(what, actual, expected);
    }

    void Check(string what, string actual, string expected)
    {
        if (actual == expected) Log($"ok   {what} = {expected}");
        else { scriptFailed = true; Log($"FAIL line {scriptLine}: expect {what} '{expected}', got '{actual}'"); }
    }

    void Log(string line) { scriptLog.Add(line); Console.WriteLine(line); }

    void FinishScript()
    {
        Log(scriptFailed ? "SCRIPT FAIL" : "SCRIPT PASS");
        try { File.WriteAllLines(options.Script + ".log", scriptLog); } catch (IOException) { }
        Environment.ExitCode = scriptFailed ? 1 : 0;
        Exit();
    }
}

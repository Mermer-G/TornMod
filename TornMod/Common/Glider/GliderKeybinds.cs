using Terraria.GameInput;
using Terraria.ModLoader;

namespace TornMod.Common.Glider;

public class GliderKeybinds : ModSystem
{
    internal const string ToggleKeyName = "TornMod/ToggleGliderMode";
    public static ModKeybind ToggleMode { get; private set; }
    public override void Load() => ToggleMode = KeybindLoader.RegisterKeybind(Mod, "ToggleGliderMode", "W");
    public override void Unload() => ToggleMode = null;
    internal static bool Pressed()
    {
        if (ToggleMode == null) return false;
        var pressed = PlayerInput.Triggers.JustPressed.KeyStatus;
        bool edge = pressed.TryGetValue(ToggleKeyName, out bool value) && value;
        // Existing profiles can lack a binding for a key introduced during a mod reload.
        // Keep the advertised W usable until it is assigned in the Controls menu.
        var keys = PlayerInput.CurrentProfile.InputModes[InputMode.Keyboard].KeyStatus;
        keys.TryGetValue(ToggleKeyName, out var bindings);
        bool defaultEdge = PlayerInput.Triggers.JustPressed.Up && keys.TryGetValue("Up", out var up) && up.Contains("W");
        return ResolvePressed(edge, defaultEdge, bindings);
    }
    internal static bool ResolvePressed(bool registeredEdge, bool defaultWEdge, System.Collections.Generic.List<string> bindings) =>
        registeredEdge || (defaultWEdge && (bindings == null || bindings.Count == 0 || bindings.Contains("W")));
    public override void OnWorldLoad()
    {
        if (Terraria.Main.dedServ) return;
        var keys = PlayerInput.CurrentProfile.InputModes[InputMode.Keyboard].KeyStatus;
        Mod.Logger.Info("Glider toggle binding: " + (keys.TryGetValue(ToggleKeyName, out var bindings) ? string.Join(",", bindings) : "missing; W fallback"));
    }
}


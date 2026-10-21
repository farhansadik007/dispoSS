using System.Windows.Input;
using DisposableScreenshots.Interop;

namespace DisposableScreenshots.Models;

public sealed record HotkeyDefinition(ModifierKeys Modifiers, Key Key)
{
    public static HotkeyDefinition Default { get; } = new(ModifierKeys.Control | ModifierKeys.Alt, Key.S);

    public static HotkeyDefinition Parse(string? text) =>
        TryParse(text, out var hotkey) ? hotkey : Default;

    public static bool TryParse(string? text, out HotkeyDefinition hotkey)
    {
        hotkey = Default;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var modifiers = ModifierKeys.None;
        Key? key = null;

        foreach (var raw in text!.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var rawPart = raw.Trim();
            if (rawPart.Length == 0)
            {
                continue;
            }

            switch (rawPart.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= ModifierKeys.Control;
                    continue;
                case "alt":
                    modifiers |= ModifierKeys.Alt;
                    continue;
                case "shift":
                    modifiers |= ModifierKeys.Shift;
                    continue;
                case "win":
                case "windows":
                    modifiers |= ModifierKeys.Windows;
                    continue;
            }

            if (key is not null || !Enum.TryParse<Key>(rawPart, true, out var parsedKey))
            {
                return false;
            }

            if (parsedKey is Key.LeftCtrl or Key.RightCtrl
                or Key.LeftAlt or Key.RightAlt
                or Key.LeftShift or Key.RightShift
                or Key.LWin or Key.RWin
                or Key.None)
            {
                return false;
            }

            key = parsedKey;
        }

        if (key is null)
        {
            return false;
        }

        // A bare letter would hijack normal typing globally. Function keys are safe enough.
        if (modifiers == ModifierKeys.None && (key < Key.F1 || key > Key.F24))
        {
            return false;
        }

        hotkey = new HotkeyDefinition(modifiers, key.Value);
        return true;
    }

    public uint ToWin32Modifiers()
    {
        uint value = NativeMethods.MOD_NOREPEAT;

        if (Modifiers.HasFlag(ModifierKeys.Alt))
        {
            value |= NativeMethods.MOD_ALT;
        }

        if (Modifiers.HasFlag(ModifierKeys.Control))
        {
            value |= NativeMethods.MOD_CONTROL;
        }

        if (Modifiers.HasFlag(ModifierKeys.Shift))
        {
            value |= NativeMethods.MOD_SHIFT;
        }

        if (Modifiers.HasFlag(ModifierKeys.Windows))
        {
            value |= NativeMethods.MOD_WIN;
        }

        return value;
    }

    public override string ToString()
    {
        var parts = new List<string>(4);

        if (Modifiers.HasFlag(ModifierKeys.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(ModifierKeys.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(ModifierKeys.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(ModifierKeys.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }
}

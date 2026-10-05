using JoksterCube.PingMark.Domain.Enums;

namespace JoksterCube.PingMark.Domain;

internal sealed class ShortcutHold
{
    private float? _started;
    private bool _consumed;

    internal ShortcutAction Update(bool pressed, bool held, bool allowed, float now, float duration)
    {
        if (!allowed)
        {
            _started = null;
            _consumed = true;
            return ShortcutAction.None;
        }
        if (pressed) { _started = now; _consumed = false; }
        if (!_started.HasValue) return ShortcutAction.None;
        if (!held)
        {
            _started = null;
            return _consumed ? ShortcutAction.None : ShortcutAction.Toggle;
        }
        if (!_consumed && now - _started.Value >= duration)
        {
            _consumed = true;
            return ShortcutAction.OpenEditor;
        }
        return ShortcutAction.None;
    }
}
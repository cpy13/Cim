using System;
using System.Collections.Generic;

namespace EQModeChangeSimulator
{
    internal static class CimUiThread
    {
        // Dispatch a WHOLE command/transaction, never a read and write separately.
        internal static void Run(System.Windows.Forms.Control owner, Action action)
        {
            if (owner.IsDisposed || owner.Disposing || !owner.IsHandleCreated)
                throw new InvalidOperationException("CIM owner UI is unavailable");
            if (owner.InvokeRequired) owner.Invoke(action);
            else action();
        }
    }

    // Runs entirely on the Form1/Compolet owner thread, including read-modify-write.
    // This is shared by the manual and remote entry points; it has no UI dependency.
    internal sealed class LocalCimSignalReset
    {
        private readonly Func<string, int[]> _read;
        private readonly Func<string, int[], bool> _write;
        private readonly Action<string> _log;
        private readonly Func<string, object, bool> _notify;
        private readonly Dictionary<string, List<SignalMonitorForm.SignalDef>> _targets = new();
        private readonly List<string> _tagOrder = new();

        internal LocalCimSignalReset(IEnumerable<SignalMonitorForm.SignalDef> signals,
            Func<string, int[]> read, Func<string, int[], bool> write,
            Action<string> log, Func<string, object, bool> notify)
        {
            _read = read; _write = write; _log = log; _notify = notify;
            foreach (var s in signals)
            {
                // Exactly the original button's selection, regardless of Writable.
                if (s.UiSubGroup != "本机 → 上游" && s.UiSubGroup != "本机 → 下游") continue;
                if (s.DisplayName.Contains("In line")) continue;
                if (!_targets.ContainsKey(s.Tag))
                {
                    _targets[s.Tag] = new List<SignalMonitorForm.SignalDef>();
                    _tagOrder.Add(s.Tag);
                }
                _targets[s.Tag].Add(s);
            }
        }

        internal void Clear(string origin)
        {
            // Like the original manual button: only clear local outputs; leave input edge history alone.
            bool writesOk = true;
            foreach (var tag in _tagOrder)
            {
                var group = new KeyValuePair<string, List<SignalMonitorForm.SignalDef>>(tag, _targets[tag]);
                _log($"[CIM][RESET] Clear local signals invoked Tag={group.Key} Origin={origin}");
                try
                {
                    var words = _read(group.Key);
                    if (words == null) throw new InvalidOperationException("Tag read returned null");
                    // Validate the whole tag before mutating anything.
                    foreach (var s in group.Value)
                        if (s.Word < 0 || s.Word >= words.Length)
                            throw new InvalidOperationException("Tag word array is too short");
                    words = (int[])words.Clone();
                    bool changed = false;
                    foreach (var s in group.Value)
                    {
                        changed |= (words[s.Word] & (1 << s.Bit)) != 0;
                        words[s.Word] &= ~(1 << s.Bit);
                    }
                    // Repeated commands with already-zero outputs cause no redundant tag write.
                    if (changed && !_write(group.Key, words))
                        throw new InvalidOperationException("Tag write returned false");
                }
                catch (Exception ex)
                {
                    writesOk = false;
                    _log($"[CIM][RESET] Clear failed Tag={group.Key}: {ex.Message}");
                }
            }
            _log($"[CIM][RESET] Clear routine finished; writeCallsOk={writesOk}; readback=not_performed");
            // Preserve the existing notification exactly once per invocation, including partial failure.
            // EQResetCommand clears only the old AOI upstream-request flag; it is NOT a success reply.
            try
            {
                bool sent = _notify("EQResetCommand", new
                {
                    reason = origin,
                    time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
                _log(sent ? "[CIM][RESET] EQResetCommand sent" : "[CIM][RESET] EQResetCommand send failed; no retry");
            }
            catch (Exception ex) { _log("[CIM][RESET] EQResetCommand send failed: " + ex.Message); }
        }

    }
}

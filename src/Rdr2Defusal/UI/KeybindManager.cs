using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Rdr2Defusal.UI
{
    /// <summary>
    /// Tracks key -> action mapping for debug shortcuts.
    /// Roadmap:
    /// - Persist bindings to disk per-user.
    /// - Add conflict resolution UI (warn when same key is bound twice).
    /// - Allow contextual bindings (only while menu hidden).
    /// </summary>
    public sealed class KeybindManager
    {
        private readonly Dictionary<string, Keys> _actionToKey = new Dictionary<string, Keys>();
        private readonly Dictionary<Keys, string> _keyToAction = new Dictionary<Keys, string>();

        public void SetBinding(string actionId, Keys key)
        {
            // Remove previous binding on this action.
            if (_actionToKey.TryGetValue(actionId, out Keys previous))
            {
                _actionToKey.Remove(actionId);
                _keyToAction.Remove(previous);
            }

            // Remove conflicting binding on the new key.
            if (_keyToAction.TryGetValue(key, out string otherAction))
            {
                _keyToAction.Remove(key);
                _actionToKey.Remove(otherAction);
            }

            _actionToKey[actionId] = key;
            _keyToAction[key] = actionId;
        }

        public void ClearBinding(string actionId)
        {
            if (_actionToKey.TryGetValue(actionId, out Keys key))
            {
                _actionToKey.Remove(actionId);
                _keyToAction.Remove(key);
            }
        }

        public Keys? GetBinding(string actionId)
        {
            if (_actionToKey.TryGetValue(actionId, out Keys key))
                return key;
            return null;
        }

        public bool TryHandle(Keys key, System.Func<string, bool> invoker)
        {
            if (_keyToAction.TryGetValue(key, out string actionId))
                return invoker(actionId);

            return false;
        }

        public IEnumerable<KeybindEntry> AllBindings(IEnumerable<KeybindEntry> actions)
        {
            // Merge known actions with current bindings.
            foreach (KeybindEntry action in actions)
            {
                Keys? bound = GetBinding(action.Id);
                yield return new KeybindEntry
                {
                    Id = action.Id,
                    Label = action.Label,
                    Key = bound
                };
            }
        }
    }

    public sealed class KeybindEntry
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public Keys? Key { get; set; }

        /// <summary>
        /// Callback to commit a new binding.
        /// </summary>
        public System.Action<Keys> OnRebind { get; set; }
    }
}

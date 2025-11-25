using System.Collections.Generic;

using RDR2;
using RDR2.UI;

using RScreen = RDR2.UI.Screen;

namespace Rdr2Defusal.World
{
    /// <summary>
    /// Runtime-owned registry of bomb sites.
    /// - Allows adding/updating sites from dev hotkeys.
    /// - Keeps defaults from Sites.cs so existing flow still works.
    /// - Avoids Vector3 to keep compatibility with this ScriptHook build.
    ///
    /// Roadmap:
    /// - Persist to disk (JSON) so dumped sites survive reloads.
    /// - Support multiple arenas/maps with named profiles.
    /// - Draw debug markers/blips/volumes to visualize plant zones.
    /// - Validate plant/defuse radius, add per-site cover points for bots.
    /// - Mirror CT/T spawn points alongside sites.
    /// </summary>
    public sealed class SiteRegistry
    {
        private readonly Dictionary<string, SiteDefinition> _sites = new Dictionary<string, SiteDefinition>();

        public SiteRegistry()
        {
            SeedDefaults();
        }

        public SiteDefinition Get(string label)
        {
            if (label == null) return null;
            _sites.TryGetValue(label, out SiteDefinition site);
            return site;
        }

        public SiteDefinition Copy(string label)
        {
            SiteDefinition src = Get(label);
            if (src == null) return null;
            return new SiteDefinition
            {
                Label = src.Label,
                X = src.X,
                Y = src.Y,
                Z = src.Z,
                H = src.H
            };
        }

        public SpawnPoint CreateSpawnFromPed(Ped ped)
        {
            if (ped == null || !ped.Exists()) return null;
            return new SpawnPoint
            {
                X = ped.Position.X,
                Y = ped.Position.Y,
                Z = ped.Position.Z,
                H = ped.Heading
            };
        }

        public void SetSites(SiteDefinition a, SiteDefinition b)
        {
            if (a != null)
                _sites["A"] = a;
            if (b != null)
                _sites["B"] = b;

            RScreen.DisplaySubtitle($"[Sites] Active map -> A:{Describe(a)} B:{Describe(b)}");
        }

        public void SetFromPlayer(string label, Ped ped)
        {
            if (ped == null || !ped.Exists()) return;
            if (string.IsNullOrEmpty(label)) return;

            _sites[label] = new SiteDefinition
            {
                Label = label,
                X = ped.Position.X,
                Y = ped.Position.Y,
                Z = ped.Position.Z,
                H = ped.Heading
            };

            RScreen.DisplaySubtitle($"[Sites] Set {label} -> {ped.Position.X:0.00}, {ped.Position.Y:0.00}, {ped.Position.Z:0.00} (H {ped.Heading:0.0})");
        }

        public string DumpSummary()
        {
            // Compact string for HUD/log spam; future work will serialize to disk.
            string a = _sites.ContainsKey("A") ? _sites["A"].Describe() : "A: missing";
            string b = _sites.ContainsKey("B") ? _sites["B"].Describe() : "B: missing";
            return $"{a} | {b}";
        }

        private string Describe(SiteDefinition def)
        {
            if (def == null) return "missing";
            return $"{def.X:0.00},{def.Y:0.00},{def.Z:0.00} H {def.H:0.0}";
        }

        private void SeedDefaults()
        {
            _sites["A"] = new SiteDefinition
            {
                Label = "A",
                X = Sites.SiteAX,
                Y = Sites.SiteAY,
                Z = Sites.SiteAZ,
                H = Sites.ArenaH
            };

            _sites["B"] = new SiteDefinition
            {
                Label = "B",
                X = Sites.SiteBX,
                Y = Sites.SiteBY,
                Z = Sites.SiteBZ,
                H = Sites.ArenaH
            };
        }
    }
}

using System.Collections.Generic;

using RDR2;
using RDR2.UI;

using Rdr2Defusal.Core;

using RScreen = RDR2.UI.Screen;

namespace Rdr2Defusal.Bots
{
    /// <summary>
    /// Tracks all active bots in the world.
    /// Future roadmap (kept close to code for clarity):
    /// - Relationship groups per team to stop friendly fire.
    /// - Behavioral states (Idle, RoamToSite, Hold, Planting, Defusing, PostRound freeze).
    /// - Persistent identity per bot so we can attribute kills/assists/economy later.
    /// - Pooling/despawning logic per round to avoid over-spawning.
    /// - Event callbacks (OnSpawned, OnKilled, OnPlantStart, OnDefuseStart, etc).
    /// - Possibility to remote-control a bot as "player camera" for replay later.
    /// </summary>
    public sealed class BotManager
    {
        private readonly List<BotAgent> _agents = new List<BotAgent>();
        private readonly BotFactory _factory = new BotFactory();

        public IReadOnlyList<BotAgent> Agents => _agents;

        public BotAgent GetAgent(Ped ped)
        {
            for (int i = 0; i < _agents.Count; i++)
            {
                if (_agents[i].Ped == ped) return _agents[i];
            }
            return null;
        }

        public BotAgent Spawn(BotSpawnRequest request)
        {
            Ped ped = _factory.Spawn(request);
            if (ped == null || !ped.Exists()) return null;

            var agent = new BotAgent
            {
                Ped = ped,
                Request = request,
                Team = request.Team
            };

            _agents.Add(agent);
            return agent;
        }

        public int DeleteAll()
        {
            int deleted = 0;
            for (int i = 0; i < _agents.Count; i++)
            {
                Ped ped = _agents[i].Ped;
                if (ped != null && ped.Exists())
                {
                    ped.Delete();
                    deleted++;
                }
            }

            _agents.Clear();
            return deleted;
        }

        public void Tick(float dt, RoundState roundState)
        {
            // Skeleton only: no per-frame AI yet.
            // Additions planned:
            // - When roundState == Warmup: freeze bots, queue next round spawns.
            // - When Live: enable navigation packages toward Site A/B, react to combat.
            // - When PostRound: halt combat, play surrender/celebrate anims, and despawn.
            // - Health/bleed-out checks to delete dead handles and respawn on next round.
            // - Optional debug overlays (lines to targets, site labels, nav grid).
        }

        public int CountAlive(string team)
        {
            int count = 0;
            for (int i = 0; i < _agents.Count; i++)
            {
                if (!string.Equals(_agents[i].Team, team, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                Ped ped = _agents[i].Ped;
                if (ped != null && ped.Exists() && !ped.IsDead)
                    count++;
            }

            return count;
        }

        public List<Ped> GetTeamPeds(string team)
        {
            var list = new List<Ped>();
            for (int i = 0; i < _agents.Count; i++)
            {
                if (!string.Equals(_agents[i].Team, team, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                Ped ped = _agents[i].Ped;
                if (ped != null && ped.Exists() && !ped.IsDead)
                    list.Add(ped);
            }
            return list;
        }
    }

    public sealed class BotAgent
    {
        public Ped Ped { get; set; }
        public BotSpawnRequest Request { get; set; }
        public string Team { get; set; }
        public int TargetHandle { get; set; }
        public int LastOrderGameTime { get; set; }
    }
}

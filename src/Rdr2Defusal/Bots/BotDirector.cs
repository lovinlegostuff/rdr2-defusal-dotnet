using RDR2;
using RDR2.Native;
using RDR2.UI;

using Rdr2Defusal.Core;
using Rdr2Defusal.World;
using RWorld = RDR2.World;

using RScreen = RDR2.UI.Screen;

namespace Rdr2Defusal.Bots
{
    /// <summary>
    /// High-level bot coordinator.
    /// - Owns the BotManager (spawn/delete bookkeeping).
    /// - Bridges round states to bot behavior.
    /// </summary>
    public sealed class BotDirector
    {
        private readonly BotManager _manager = new BotManager();
        private readonly SiteRegistry _sites;

        private RoundState _roundState = RoundState.Idle;
        private string _teamCommand = "idle";
        private const ulong TASK_COMBAT_PED = 0xF166E48407BAC484;
        private const ulong CLEAR_PED_TASKS = 0xE1EF3C1216AFF2CD; // CLEAR_PED_TASKS
        private const ulong SET_PED_RELATIONSHIP_GROUP_HASH = 0xC80A74AC829DDD92;
        private const ulong SET_RELATIONSHIP_BETWEEN_GROUPS = 0xBF25EB89375A37AD;
        private const ulong GET_HASH_KEY = 0xFD340785ADF8CFB7;
        private const ulong SET_PED_COMBAT_MOVEMENT = 0x4D9CA1009AFBD057;
        private const ulong SET_PED_COMBAT_RANGE = 0x3C606747B23E497B;
        private const ulong SET_PED_COMBAT_ATTRIBUTES = 0x9F7794730795E019;
        private const ulong SET_BLOCKING_OF_NON_TEMPORARY_EVENTS = 0x9F8AA94D6D97DBF4;
        private const ulong CLEAR_PED_TASKS_IMMEDIATELY = 0xAAA34F8A7CB32098;
        private const ulong TASK_COMBAT_HATED_TARGETS_AROUND_PED = 0x7BF835BB9E2698C8;
        private const float AGGRO_RADIUS = 80f;
        private readonly int _relA;
        private readonly int _relB;
        public BotDirector(SiteRegistry sites)
        {
            _sites = sites;

            _relA = RWorld.AddRelationshipGroup("TEAM_A");
            _relB = RWorld.AddRelationshipGroup("TEAM_B");

            RWorld.SetRelationshipBetweenGroups(Relationship.Hate, _relA, _relB);
            RWorld.SetRelationshipBetweenGroups(Relationship.Hate, _relB, _relA);
            RWorld.SetRelationshipBetweenGroups(Relationship.Respect, _relA, _relA);
            RWorld.SetRelationshipBetweenGroups(Relationship.Respect, _relB, _relB);
        }

        public void OnRoundStateChanged(RoundState state)
        {
            _roundState = state;

            if (state == RoundState.PostRound)
                _manager.DeleteAll();

            // Warmup: future behavior - spawn both teams frozen/ghosted for briefing.
            // Live: unlock AI, set destinations.
            // Idle: keep empty unless dev tools request persistent bots.
        }

        public void Tick(float dt)
        {
            _manager.Tick(dt, _roundState);
            UpdateCombatTargets();
        }

        public void SpawnDebugGuardAtPlayer()
        {
            var request = new BotSpawnRequest
            {
                DebugName = "dbg_guard",
                Team = "Neutral",
                AttackPlayer = true,
                UseExplicitLocation = false
            };

            _manager.Spawn(request);
        }

        public void SpawnDebugGuardAtSite(string siteLabel)
        {
            SiteDefinition site = _sites.Get(siteLabel);
            if (site == null)
            {
                RScreen.DisplaySubtitle($"[BotDirector] Site {siteLabel} missing.");
                return;
            }

            var request = new BotSpawnRequest
            {
                DebugName = $"dbg_{siteLabel}",
                Team = siteLabel,
                AttackPlayer = true,
                UseExplicitLocation = true,
                SpawnX = site.X,
                SpawnY = site.Y,
                SpawnZ = site.Z,
                SpawnHeading = site.H
            };

            _manager.Spawn(request);
        }

        public void DeleteAll()
        {
            _manager.DeleteAll();
        }

        public void SpawnTeams(MapDefinition map, int teamACount, int teamBCount, string teamAWeapon, string teamBWeapon)
        {
            if (map == null) return;

            var teamAPeds = new System.Collections.Generic.List<Ped>();
            var teamBPeds = new System.Collections.Generic.List<Ped>();

            // Team A spawns
            for (int i = 0; i < teamACount; i++)
            {
                var sp = ResolveSpawn(map.TeamASpawns, map.SiteA, Game.Player.Character);
                var req = new BotSpawnRequest
                {
                    DebugName = $"A_{i + 1}",
                    Team = "A",
                    AttackPlayer = false,
                    UseExplicitLocation = true,
                    SpawnX = sp.X,
                    SpawnY = sp.Y,
                    SpawnZ = sp.Z,
                    SpawnHeading = sp.H,
                    WeaponName = teamAWeapon
                };
                var agent = _manager.Spawn(req);
                if (agent != null && agent.Ped != null && agent.Ped.Exists())
                {
                    ApplyRelationships(agent.Ped, "TEAM_A");
                    SetupCombatDefaults(agent.Ped);
                    teamAPeds.Add(agent.Ped);
                }
            }

            // Team B spawns
            for (int i = 0; i < teamBCount; i++)
            {
                var sp = ResolveSpawn(map.TeamBSpawns, map.SiteB, Game.Player.Character);
                var req = new BotSpawnRequest
                {
                    DebugName = $"B_{i + 1}",
                    Team = "B",
                    AttackPlayer = false,
                    UseExplicitLocation = true,
                    SpawnX = sp.X,
                    SpawnY = sp.Y,
                    SpawnZ = sp.Z,
                    SpawnHeading = sp.H,
                    WeaponName = teamBWeapon
                };
                var agent = _manager.Spawn(req);
                if (agent != null && agent.Ped != null && agent.Ped.Exists())
                {
                    ApplyRelationships(agent.Ped, "TEAM_B");
                    SetupCombatDefaults(agent.Ped);
                    teamBPeds.Add(agent.Ped);
                }
            }

            AssignTargets(teamAPeds, teamBPeds);
        }

        private void ApplyRelationships(Ped ped, string groupName)
        {
            uint groupHash = Function.Call<uint>(GET_HASH_KEY, new InputArgument(groupName));
            Function.Call(SET_PED_RELATIONSHIP_GROUP_HASH,
                new InputArgument(ped.Handle),
                new InputArgument(groupHash));

            uint groupA = Function.Call<uint>(GET_HASH_KEY, new InputArgument("TEAM_A"));
            uint groupB = Function.Call<uint>(GET_HASH_KEY, new InputArgument("TEAM_B"));

            // hostile between groups
            Function.Call(SET_RELATIONSHIP_BETWEEN_GROUPS,
                new InputArgument(5),
                new InputArgument(groupA),
                new InputArgument(groupB));
            Function.Call(SET_RELATIONSHIP_BETWEEN_GROUPS,
                new InputArgument(5),
                new InputArgument(groupB),
                new InputArgument(groupA));

            // friendly within same
            Function.Call(SET_RELATIONSHIP_BETWEEN_GROUPS,
                new InputArgument(0),
                new InputArgument(groupA),
                new InputArgument(groupA));
            Function.Call(SET_RELATIONSHIP_BETWEEN_GROUPS,
                new InputArgument(0),
                new InputArgument(groupB),
                new InputArgument(groupB));

            ped.RelationshipGroup = (int)groupHash;
        }

        private void SetupCombatDefaults(Ped ped)
        {
            if (ped == null || !ped.Exists()) return;
            ped.BlockPermanentEvents = true;
            ped.AlwaysKeepTask = true;
            Function.Call(SET_BLOCKING_OF_NON_TEMPORARY_EVENTS,
                new InputArgument(ped.Handle),
                new InputArgument(true));

            // Will advance on target
            Function.Call(SET_PED_COMBAT_MOVEMENT,
                new InputArgument(ped.Handle),
                new InputArgument(2)); // CPED_COMBAT_MOVEMENT_WillAdvance

            // Medium range
            Function.Call(SET_PED_COMBAT_RANGE,
                new InputArgument(ped.Handle),
                new InputArgument(2)); // Medium

            // Prevent fleeing
            Function.Call(SET_PED_COMBAT_ATTRIBUTES,
                new InputArgument(ped.Handle),
                new InputArgument(46), // BF_CanFightArmedPedsWhenNotArmed in GTA; here used to keep engaged
                new InputArgument(true));
        }

        private void AssignTargets(System.Collections.Generic.List<Ped> teamA, System.Collections.Generic.List<Ped> teamB)
        {
            // no-op; handled in UpdateCombatTargets
        }

        private SpawnPoint ResolveSpawn(System.Collections.Generic.List<SpawnPoint> list, SiteDefinition site, Ped fallbackPed)
        {
            if (list != null && list.Count > 0)
            {
                var idx = Game.GameTime % list.Count;
                return list[(int)idx];
            }

            if (site != null)
            {
                return new SpawnPoint { X = site.X, Y = site.Y, Z = site.Z, H = site.H };
            }

            return new SpawnPoint { X = 0, Y = 0, Z = 0, H = 0 };
        }

        public int GetAliveCount(string team) => _manager.CountAlive(team);

        public void SetTeamCommand(string command)
        {
            _teamCommand = command;
        }

        private void UpdateCombatTargets()
        {
            var teamA = _manager.GetTeamPeds("A");
            var teamB = _manager.GetTeamPeds("B");
            if (teamA.Count == 0 && teamB.Count == 0) return;

            ApplyCommand(teamA, teamB, "A");
            ApplyCommand(teamB, teamA, "B");
        }

        private void ApplyCommand(System.Collections.Generic.List<Ped> friendly, System.Collections.Generic.List<Ped> enemy, string teamLabel)
        {
            if (friendly.Count == 0) return;

            for (int i = 0; i < friendly.Count; i++)
            {
                Ped ped = friendly[i];
                if (ped == null || !ped.Exists() || ped.IsDead) continue;
                Ped target = FindNearest(ped, enemy);
                if (target == null || !target.Exists() || target.IsDead) continue;

                if (_teamCommand == "hold")
                {
                    continue;
                }

                ped.AlwaysKeepTask = true;
                Function.Call(SET_BLOCKING_OF_NON_TEMPORARY_EVENTS,
                    new InputArgument(ped.Handle),
                    new InputArgument(true));

                // If already fighting this target, leave as-is.
                if (ped.IsInCombatAgainst(target)) continue;

                Function.Call(CLEAR_PED_TASKS_IMMEDIATELY,
                    new InputArgument(ped.Handle));
                ped.Task.FightAgainstHatedTargets(AGGRO_RADIUS);
                Function.Call(TASK_COMBAT_PED,
                    new InputArgument(ped.Handle),
                    new InputArgument(target.Handle),
                    new InputArgument(0),
                    new InputArgument(16));

                var agent = _manager.GetAgent(ped);
                if (agent != null)
                {
                    agent.TargetHandle = target.Handle;
                    agent.LastOrderGameTime = Game.GameTime;
                }
            }
        }

        private Ped FindNearest(Ped ped, System.Collections.Generic.List<Ped> enemies)
        {
            Ped closest = null;
            float best = float.MaxValue;
            var pos = ped.Position;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || !e.Exists() || e.IsDead) continue;
                var diff = e.Position - pos;
                float d2 = diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z;
                if (d2 < best)
                {
                    best = d2;
                    closest = e;
                }
            }
            return closest;
        }
    }
}

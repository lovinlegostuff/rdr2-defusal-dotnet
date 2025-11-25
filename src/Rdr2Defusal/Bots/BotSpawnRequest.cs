using RDR2;

namespace Rdr2Defusal.Bots
{
    /// <summary>
    /// Describes how a bot should be created. This is intentionally verbose so we can
    /// grow into team loadouts, behaviors, and spawn rules without rewriting call sites.
    /// </summary>
    public sealed class BotSpawnRequest
    {
        /// <summary>
        /// Optional debug label for HUD/logging.
        /// </summary>
        public string DebugName { get; set; } = "bot";

        /// <summary>
        /// Story-safe model name. Default keeps the old sandbox behavior.
        /// </summary>
        public string ModelName { get; set; } = "A_M_M_RANCHER_01";

        /// <summary>
        /// Weapon string passed to GET_HASH_KEY; we avoid WeaponHash enum.
        /// </summary>
        public string WeaponName { get; set; } = "WEAPON_REPEATER_CARBINE";

        /// <summary>
        /// Team bucket for later macro-logic (T/CT, spectators, neutrals).
        /// </summary>
        public string Team { get; set; } = "Neutral";

        /// <summary>
        /// When false we spawn relative to the player; when true we teleport to SpawnX/Y/Z/H.
        /// </summary>
        public bool UseExplicitLocation { get; set; }

        public float SpawnX { get; set; }
        public float SpawnY { get; set; }
        public float SpawnZ { get; set; }
        public float SpawnHeading { get; set; }

        /// <summary>
        /// Forward offset from the player for quick debugging spawns.
        /// </summary>
        public float ForwardOffset { get; set; } = 6.0f;

        /// <summary>
        /// If set we issue TASK_COMBAT_PED; later this will accept squads/teams as targets.
        /// </summary>
        public bool AttackPlayer { get; set; } = true;

        /// <summary>
        /// Optional target; defaults to Game.Player.Character when null.
        /// </summary>
        public Ped Target { get; set; }
    }
}

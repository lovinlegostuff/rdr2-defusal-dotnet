using RDR2;
using RDR2.Native;
using RDR2.UI;

using RScreen = RDR2.UI.Screen;
using RWorld = RDR2.World;

namespace Rdr2Defusal.Bots
{
    /// <summary>
    /// Low-level spawn helper that stays ScriptHook-safe (native hashes + InputArgument).
    /// Future work (documented inline) will add:
    /// - Pool-aware reuse to avoid piling up handles every round.
    /// - Async/spawn batching to avoid frame hitches when spawning 6+ peds.
    /// - Loadout tables keyed by team/role (rifler, AWPer, eco pistol, knife-only, etc).
    /// - Behavior hooks (set relationship groups, accuracy, combat attributes, invincibility for dev cheats).
    /// - Event bus hooks so the round system can listen for deaths/defuses planted on a bot.
    /// - Dedicated despawn pipeline to clean on PostRound or player death.
    /// </summary>
    public sealed class BotFactory
    {
        // RDR2 native hashes
        private const ulong GET_HASH_KEY = 0xFD340785ADF8CFB7;
        private const ulong SET_RANDOM_OUTFIT_VARIATION = 0x283978A15512B2FE;
        private const ulong GIVE_WEAPON_TO_PED = 0x5E3BDDBCB83F3D84;
        private const ulong TASK_COMBAT_PED = 0xF166E48407BAC484;
        private const ulong SET_ENTITY_COORDS = 0x06843DA7060A026B;
        private const ulong SET_ENTITY_HEADING = 0xCF2B9C0645C4651B;

        public Ped Spawn(BotSpawnRequest request)
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
            {
                RScreen.DisplaySubtitle("[BotFactory] No player; cannot spawn.");
                return null;
            }

            var model = new Model(request.ModelName);
            model.Request(5000);

            if (!model.IsLoaded)
            {
                RScreen.DisplaySubtitle("[BotFactory] Model failed to load.");
                return null;
            }

            // Spawn relative to player to avoid explicit Vector3 construction (safer across API versions).
            var spawnPos = player.Position + player.ForwardVector * request.ForwardOffset;

            Ped ped = RWorld.CreatePed(model, spawnPos);
            if (ped == null || !ped.Exists())
            {
                RScreen.DisplaySubtitle("[BotFactory] CreatePed failed.");
                return null;
            }

            ApplyOutfit(ped);
            GiveLoadout(ped, request.WeaponName);

            if (request.UseExplicitLocation)
                Teleport(ped, request.SpawnX, request.SpawnY, request.SpawnZ, request.SpawnHeading);

            if (request.AttackPlayer)
                TaskAgainstTarget(ped, request.Target ?? player);

            RScreen.DisplaySubtitle($"[BotFactory] Spawned {request.DebugName} ({request.Team}).");
            return ped;
        }

        public void ApplyOutfit(Ped ped)
        {
            if (ped == null || !ped.Exists()) return;

            Function.Call(SET_RANDOM_OUTFIT_VARIATION,
                new InputArgument(ped.Handle),
                new InputArgument(true)
            );
        }

        public void Teleport(Ped ped, float x, float y, float z, float heading)
        {
            if (ped == null || !ped.Exists()) return;

            Function.Call(SET_ENTITY_COORDS,
                new InputArgument(ped.Handle),
                new InputArgument(x),
                new InputArgument(y),
                new InputArgument(z),
                new InputArgument(false),
                new InputArgument(false),
                new InputArgument(false),
                new InputArgument(true)
            );

            Function.Call(SET_ENTITY_HEADING,
                new InputArgument(ped.Handle),
                new InputArgument(heading)
            );
        }

        public void GiveLoadout(Ped ped, string weaponName)
        {
            if (ped == null || !ped.Exists()) return;

            uint weaponHash = Function.Call<uint>(GET_HASH_KEY, new InputArgument(weaponName));

            Function.Call(GIVE_WEAPON_TO_PED,
                new InputArgument(ped.Handle),
                new InputArgument(weaponHash),
                new InputArgument(200),   // ammo
                new InputArgument(true),  // equip now
                new InputArgument(true),  // p4 (force)
                new InputArgument(0),     // group
                new InputArgument(true),  // p6
                new InputArgument(0),     // p7
                new InputArgument(0),     // p8
                new InputArgument(false)  // leftHanded
            );
        }

        public void TaskAgainstTarget(Ped ped, Ped target)
        {
            if (ped == null || !ped.Exists()) return;
            if (target == null || !target.Exists()) return;

            Function.Call(TASK_COMBAT_PED,
                new InputArgument(ped.Handle),
                new InputArgument(target.Handle),
                new InputArgument(0),
                new InputArgument(16)
            );
        }
    }
}

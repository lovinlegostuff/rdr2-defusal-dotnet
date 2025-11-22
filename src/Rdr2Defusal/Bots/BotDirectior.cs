using System;
using System.Reflection;
using RDR2;
using RDR2.Native;

namespace Rdr2Defusal.Bots
{
    /// <summary>
    /// Bot spawner/controller scaffold.
    /// Uses reflection to avoid compile breaks across API versions.
    /// We'll flesh this out once you confirm spawn method works on your build.
    /// </summary>
    public sealed class BotDirector
    {
        private bool _enabled;

        public void Enable()  => _enabled = true;
        public void Disable() => _enabled = false;

        public void ResetBots()
        {
            // For now: no-op. Later we’ll delete/respawn.
        }

        public void Tick(float dt)
        {
            if (!_enabled) return;
            // later: macro-orders, site push, etc.
        }

        /// <summary>
        /// Try to spawn a ped using World.CreatePed if your API has it.
        /// If not found, this safely does nothing (no crash, no compile error).
        /// </summary>
        public Ped TrySpawnBot(uint modelHash, float x, float y, float z, float heading)
        {
            Type worldType = Type.GetType("RDR2.World, ScriptHookRDRNetAPI");
            if (worldType == null) return null; // API mismatch

            // Look for any CreatePed overload
            MethodInfo mi = worldType.GetMethod("CreatePed",
                BindingFlags.Public | BindingFlags.Static);

            if (mi == null) return null;

            try
            {
                object pedObj = mi.Invoke(null, new object[]
                {
                    modelHash,
                    x, y, z,
                    heading
                });

                return pedObj as Ped;
            }
            catch
            {
                return null;
            }
        }

        // If reflection spawn doesn't work, we’ll swap to a native-hash spawn
        // once you tell me the model you want for each team.
    }
}

using System;
using RDR2;
using RDR2.Native;
using RDR2.UI;

using Keys = System.Windows.Forms.Keys;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using RScreen = RDR2.UI.Screen;

public class BotSandbox : Script
{
    private Ped _bot;

    // RDR2 native hashes
    private const ulong GET_HASH_KEY = 0xFD340785ADF8CFB7;
    private const ulong SET_RANDOM_OUTFIT_VARIATION = 0x283978A15512B2FE;
    private const ulong GIVE_WEAPON_TO_PED = 0x5E3BDDBCB83F3D84;   // WEAPON namespace :contentReference[oaicite:2]{index=2}
    private const ulong TASK_COMBAT_PED = 0xF166E48407BAC484;

    public BotSandbox()
    {
        KeyDown += OnKeyDown;
        Interval = 0;

        // your build: subtitle takes ONLY string
        RScreen.DisplaySubtitle("[BotSandbox] F10 spawn hostile | F11 delete hostile");
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F10)
            SpawnHostileBot();

        if (e.KeyCode == Keys.F11)
            DeleteBot();
    }

    private void SpawnHostileBot()
    {
        var player = Game.Player.Character;
        if (player == null || !player.Exists()) return;

        // if one exists already, clean it first
        DeleteBot();

        // story-mode safe model (no mp_/online-only)
        var model = new Model("A_M_M_RANCHER_01");
        model.Request(5000);

        if (!model.IsLoaded)
        {
            RScreen.DisplaySubtitle("[BotSandbox] Model failed to load.");
            return;
        }

        // don't type Vector3 explicitly — your build complains
        var spawnPos = player.Position + player.ForwardVector * 6.0f;

        _bot = World.CreatePed(model, spawnPos);
        if (_bot == null || !_bot.Exists())
        {
            RScreen.DisplaySubtitle("[BotSandbox] CreatePed failed.");
            return;
        }

        // FORCE outfit so we don't get invisible-but-real bots
        Function.Call(SET_RANDOM_OUTFIT_VARIATION,
            new InputArgument(_bot.Handle),
            new InputArgument(true)
        );

        // give repeater carbine via native (no WeaponHash enum on your build)
        uint weaponHash = Function.Call<uint>(GET_HASH_KEY, new InputArgument("WEAPON_REPEATER_CARBINE"));

        Function.Call(GIVE_WEAPON_TO_PED,
            new InputArgument(_bot.Handle),
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

        // make hostile toward player
        Function.Call(TASK_COMBAT_PED,
            new InputArgument(_bot.Handle),
            new InputArgument(player.Handle),
            new InputArgument(0),
            new InputArgument(16)
        );

        RScreen.DisplaySubtitle("[BotSandbox] Hostile spawned.");
    }

    private void DeleteBot()
    {
        if (_bot != null && _bot.Exists())
        {
            _bot.Delete();
            _bot = null;
            RScreen.DisplaySubtitle("[BotSandbox] Hostile deleted.");
        }
    }
}

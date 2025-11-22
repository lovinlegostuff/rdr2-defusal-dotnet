using System;
using System.Diagnostics;

using RDR2;
using RDR2.Native;
using RDR2.UI;

using Keys = System.Windows.Forms.Keys;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using RScreen = RDR2.UI.Screen;

using Rdr2Defusal.Core;
using Rdr2Defusal.Bots;
using Rdr2Defusal.World;

public class DefusalDevHarness : Script
{
    private readonly DefusalCore _core = new DefusalCore();
    private readonly BotDirector _bots = new BotDirector();

    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private long _lastMs;

    public DefusalDevHarness()
    {
        Tick += OnTick;
        Interval = 0;
        KeyDown += OnKeyDown;

        _lastMs = _sw.ElapsedMilliseconds;

        RScreen.DisplaySubtitle("[DefusalDevHarness] Loaded. F5 start/stop | F7 arena | F6 coords | F9 ragdoll");
    }

    private void OnTick(object sender, EventArgs e)
    {
        long now = _sw.ElapsedMilliseconds;
        float dt = (now - _lastMs) / 1000f;
        _lastMs = now;

        // clamp so pauses/alt-tab don't nuke the round timer
        if (dt < 0f) dt = 0f;
        if (dt > 0.2f) dt = 0.2f;

        _core.Tick(dt);
        _bots.Tick(dt);
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F9) { RagdollPlayer(); return; }
        if (e.KeyCode == Keys.F6) { DumpPlayerCoords(); return; }
        if (e.KeyCode == Keys.F7) { TeleportToArena(); return; }

        if (e.KeyCode == Keys.F5)
        {
            if (_core.IsRunning) _core.Stop();
            else _core.Start();
            return;
        }
    }

    private void TeleportToArena()
    {
        Ped ped = Game.Player.Character;
        if (ped == null || !ped.Exists()) return;

        const ulong SET_ENTITY_COORDS  = 0x06843DA7060A026B;
        const ulong SET_ENTITY_HEADING = 0xCF2B9C0645C4651B;

        Function.Call(SET_ENTITY_COORDS,
            new InputArgument(ped.Handle),
            new InputArgument(Sites.ArenaX),
            new InputArgument(Sites.ArenaY),
            new InputArgument(Sites.ArenaZ),
            new InputArgument(false),
            new InputArgument(false),
            new InputArgument(false),
            new InputArgument(true)
        );

        Function.Call(SET_ENTITY_HEADING,
            new InputArgument(ped.Handle),
            new InputArgument(Sites.ArenaH)
        );

        RScreen.DisplaySubtitle("[Defusal] Teleported to arena.");
    }

    private void RagdollPlayer()
    {
        Ped ped = Game.Player.Character;
        if (ped == null || !ped.Exists()) return;

        const ulong SET_PED_TO_RAGDOLL = 0xAE99FB955581844A;

        Function.Call(SET_PED_TO_RAGDOLL,
            new InputArgument(ped.Handle),
            new InputArgument(2000),
            new InputArgument(2000),
            new InputArgument(0),
            new InputArgument(false),
            new InputArgument(false),
            new InputArgument(false)
        );

        RScreen.DisplaySubtitle("yeehaw. you are now limp.");
    }

    private void DumpPlayerCoords()
    {
        Ped ped = Game.Player.Character;
        if (ped == null || !ped.Exists()) return;

        var p = ped.Position;
        float h = ped.Heading;

        RScreen.DisplaySubtitle($"POS: {p.X:0.00}, {p.Y:0.00}, {p.Z:0.00} | H: {h:0.0}");
    }
}

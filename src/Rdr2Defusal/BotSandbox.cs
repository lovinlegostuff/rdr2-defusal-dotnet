using RDR2;
using RDR2.UI;

using Keys = System.Windows.Forms.Keys;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using RScreen = RDR2.UI.Screen;

using Rdr2Defusal.Bots;

public class BotSandbox : Script
{
    private readonly BotManager _manager = new BotManager();
    private BotAgent _current;

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
        Ped player = Game.Player.Character;
        if (player == null || !player.Exists()) return;

        // if one exists already, clean it first
        DeleteBot();

        _current = _manager.Spawn(new BotSpawnRequest
        {
            DebugName = "sandbox",
            Team = "Neutral",
            AttackPlayer = true,
            UseExplicitLocation = false,
            Target = player
        });
    }

    private void DeleteBot()
    {
        _manager.DeleteAll();
        _current = null;
    }
}

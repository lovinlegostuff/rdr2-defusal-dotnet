using System;
using System.Diagnostics;
using System.Collections.Generic;

using RDR2;
using RDR2.Native;
using RDR2.UI;

using Keys = System.Windows.Forms.Keys;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using RScreen = RDR2.UI.Screen;

using Rdr2Defusal.Core;
using Rdr2Defusal.Bots;
using Rdr2Defusal.World;
using Rdr2Defusal.UI;
using Rdr2Defusal;

public class DefusalDevHarness : Script
{
    private readonly DefusalCore _core = new DefusalCore();
    private readonly SiteRegistry _siteRegistry = new SiteRegistry();
    private readonly BotDirector _bots;

    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private long _lastMs;

    private string _activeSiteLabel = "A";

    private readonly KeybindManager _keybinds = new KeybindManager();
    private readonly DebugMenu _menu = new DebugMenu();
    private readonly System.Collections.Generic.Dictionary<string, System.Action> _actions = new System.Collections.Generic.Dictionary<string, System.Action>();
    private readonly NotificationManager _notify = new NotificationManager();
    private readonly BuyMenu _buyMenu = new BuyMenu();
    private readonly ConfigStore _configStore = new ConfigStore();
    private PersistedConfig _loadedConfig;
    private readonly CalloutMenu _calloutMenu = new CalloutMenu();

    // Match meta
    private readonly System.Collections.Generic.List<MapDefinition> _maps = new System.Collections.Generic.List<MapDefinition>();
    private int _mapIndex;
    private readonly string[] _difficulties = new[] { "Casual", "Normal", "Hard", "Extreme" };
    private int _difficultyIndex = 1;
    private int _teamABots = 3;
    private int _teamBBots = 3;
    private int _playerMoney = 800;
    private string _playerLoadoutWeapon = "WEAPON_REVOLVER_CATTLEMAN";

    // Loadout
    private readonly string[] _weaponOptions = new[] { "WEAPON_REVOLVER_CATTLEMAN", "WEAPON_REPEATER_CARBINE", "WEAPON_REPEATER_EVANS", "WEAPON_REPEATER_LITCHFIELD", "WEAPON_RIFLE_BOLTACTION", "WEAPON_SHOTGUN_PUMP", "WEAPON_BOW" };
    private int _playerWeaponIndex = 1;
    private int _botWeaponIndex = 1;
    private readonly System.Collections.Generic.Dictionary<string, bool> _botAllowedWeapons = new System.Collections.Generic.Dictionary<string, bool>();
    private readonly System.Collections.Generic.Dictionary<string, bool> _playerAllowedWeapons = new System.Collections.Generic.Dictionary<string, bool>();
    private int _startingMoney = 800;

    // Settings
    private readonly ThemeColors[] _themes = new[] { ThemeColors.Dark(), ThemeColors.HighContrast(), ThemeColors.Light(), ThemeColors.RockstarMuted(), ThemeColors.RockstarLight() };
    private int _themeIndex;
    private bool _autosaveMatches;
    private MatchPreset _savedPreset;
    private bool _matchRunning;
    private int _aliveA;
    private int _aliveB;
    private bool _bombPlanted;
    private float _bombTimer = 45f;
    private string _teamCommand = "Idle";

    public DefusalDevHarness()
    {
        Tick += OnTick;
        Interval = 0;
        KeyDown += OnKeyDown;

        _bots = new BotDirector(_siteRegistry);
        _core.RoundStateChanged += _bots.OnRoundStateChanged;

        LoadConfig();
        InitMaps();
        InitLoadoutAllowlists();
        InitCallouts();
        RegisterActions();
        BuildMenuPages();
        _menu.ApplyTheme(_themes[_themeIndex]);

        _lastMs = _sw.ElapsedMilliseconds;

        RScreen.DisplaySubtitle("[DefusalDevHarness] Loaded. F7 to open menu (arrows + enter)");
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
        UpdateAliveCounts();
        _menu.Tick(dt);
        _notify.Tick(dt);
        DrawOverlays();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F7)
        {
            _menu.Toggle();
            return;
        }

        if (e.KeyCode == Keys.F3)
        {
            if (_calloutMenu.Visible) _calloutMenu.Hide();
            else _calloutMenu.Show();
            return;
        }

        if (_calloutMenu.HandleKey(e.KeyCode))
            return;

        if (_buyMenu.HandleKey(e.KeyCode))
            return;

        if (_menu.HandleKey(e.KeyCode))
            return;

        if (_keybinds.TryHandle(e.KeyCode, InvokeAction))
            return;
    }

    private void DrawOverlays()
    {
        _buyMenu.SetPlayerMoney(_playerMoney);
        _buyMenu.Draw();
        _calloutMenu.Draw();
        _notify.Draw();
        DrawMatchHud();
    }

    private void DrawMatchHud()
    {
        // Compact HUD bottom center
        float x = 0.5f;
        float y = 0.92f;
        NativeDraw.Rect(x, y + 0.01f, 0.32f, 0.05f, 0, 0, 0, 140);
        string bomb = _bombPlanted ? $"Bomb: {_bombTimer:0.0}s" : "Bomb: idle";
        string text = $"Match: {(_matchRunning ? "Live" : "Idle")} | A Alive: {_aliveA} | B Alive: {_aliveB} | {bomb} | Cmd: {_teamCommand}";
        NativeDraw.Text(text, x - 0.15f, y, 0.36f, 0.36f, 245, 235, 210, 255);

        // Money top-right
        NativeDraw.Rect(0.92f, 0.06f, 0.12f, 0.04f, 0, 0, 0, 120);
        NativeDraw.Text($"${_playerMoney}", 0.86f, 0.04f, 0.38f, 0.38f, 245, 235, 210, 255);
    }

    private void TeleportToArena()
    {
        Ped ped = Game.Player.Character;
        if (ped == null || !ped.Exists()) return;

        const ulong SET_ENTITY_COORDS  = 0x06843DA7060A026B;
        const ulong SET_ENTITY_HEADING = 0xCF2B9C0645C4651B;

        var map = ActiveMap();
        SpawnPoint dest = null;
        if (map != null && map.TeamASpawns.Count > 0) dest = map.TeamASpawns[0];
        if (dest == null && map != null && map.SiteA != null)
        {
            dest = new SpawnPoint { X = map.SiteA.X, Y = map.SiteA.Y, Z = map.SiteA.Z, H = map.SiteA.H };
        }
        if (dest == null)
        {
            dest = new SpawnPoint { X = Sites.ArenaX, Y = Sites.ArenaY, Z = Sites.ArenaZ, H = Sites.ArenaH };
        }

        Function.Call(SET_ENTITY_COORDS,
            new InputArgument(ped.Handle),
            new InputArgument(dest.X),
            new InputArgument(dest.Y),
            new InputArgument(dest.Z),
            new InputArgument(false),
            new InputArgument(false),
            new InputArgument(false),
            new InputArgument(true)
        );

        Function.Call(SET_ENTITY_HEADING,
            new InputArgument(ped.Handle),
            new InputArgument(dest.H)
        );

        _notify.Enqueue("[Defusal] Teleported.");
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

        _notify.Enqueue("Ragdoll triggered");
    }

    private void DumpPlayerCoords()
    {
        Ped ped = Game.Player.Character;
        if (ped == null || !ped.Exists()) return;

        var p = ped.Position;
        float h = ped.Heading;

        _notify.Enqueue($"POS: {p.X:0.00}, {p.Y:0.00}, {p.Z:0.00} | H: {h:0.0}", 4f);
    }

    private void SetActiveSite(string label)
    {
        _activeSiteLabel = label;
        _notify.Enqueue($"[Sites] Active slot -> {label}");
    }

    private void DumpSites()
    {
        _notify.Enqueue("[Sites] " + _siteRegistry.DumpSummary(), 4f);
    }

    private void CaptureSiteFromPlayer()
    {
        Ped ped = Game.Player.Character;
        if (ped == null || !ped.Exists()) return;

        _siteRegistry.SetFromPlayer(_activeSiteLabel, ped);
    }

    private void SpawnBotAtPlayer()
    {
        _bots.SpawnDebugGuardAtPlayer();
    }

    private void SpawnBotAtActiveSite()
    {
        _bots.SpawnDebugGuardAtSite(_activeSiteLabel);
    }

    private void StartMatch()
    {
        ClearWorldPeds();
        ClearPlayerWeapons();
        GivePlayerWeapon("WEAPON_REVOLVER_CATTLEMAN");
        _playerLoadoutWeapon = "WEAPON_REVOLVER_CATTLEMAN";
        _playerMoney = _startingMoney > 0 ? _startingMoney : 800;

        _bots.DeleteAll();
        MapDefinition map = ActiveMap();
        if (map == null)
        {
            _notify.Enqueue("[Match] No active map.");
            return;
        }

        string teamAWeapon = "WEAPON_REVOLVER_CATTLEMAN";
        string teamBWeapon = "WEAPON_REVOLVER_CATTLEMAN";

        _bots.SpawnTeams(map, _teamABots, _teamBBots, teamAWeapon, teamBWeapon);
        TeleportToArena();
        _core.Start();
        _matchRunning = true;
        _buyMenu.Show();
        _buyMenu.SetItems(BuildBuyCategories(), OnBuySelection);
        _buyMenu.SetPlayerMoney(_playerMoney);
        UpdateAliveCounts();
        SaveConfig();
    }

    private void StopMatch()
    {
        _core.Stop();
        _bots.DeleteAll();
        _matchRunning = false;
        _bombPlanted = false;
        _buyMenu.Hide();
    }

    private void UpdateAliveCounts()
    {
        _aliveA = _bots.GetAliveCount("A");
        _aliveB = _bots.GetAliveCount("B");
    }

    private void ClearWorldPeds()
    {
        try
        {
            var peds = World.GetAllPeds();
            if (peds == null) return;
            for (int i = 0; i < peds.Length; i++)
            {
                Ped ped = peds[i];
                if (ped == null || !ped.Exists()) continue;
                if (ped == Game.Player.Character) continue;
                ped.Delete();
            }
        }
        catch
        {
            // ignore if API missing
        }
    }

    private void ClearPlayerWeapons()
    {
        Ped ped = Game.Player.Character;
        if (ped == null || !ped.Exists()) return;
        const ulong REMOVE_ALL_PED_WEAPONS = 0xF25DF915FA38C5F3;
        Function.Call(REMOVE_ALL_PED_WEAPONS,
            new InputArgument(ped.Handle),
            new InputArgument(true));
    }

    private void GivePlayerWeapon(string weaponName)
    {
        Ped ped = Game.Player.Character;
        if (ped == null || !ped.Exists()) return;
        const ulong GET_HASH_KEY = 0xFD340785ADF8CFB7;
        const ulong GIVE_WEAPON_TO_PED = 0x5E3BDDBCB83F3D84;
        uint weaponHash = Function.Call<uint>(GET_HASH_KEY, new InputArgument(weaponName));

        Function.Call(GIVE_WEAPON_TO_PED,
            new InputArgument(ped.Handle),
            new InputArgument(weaponHash),
            new InputArgument(200),
            new InputArgument(true),
            new InputArgument(true),
            new InputArgument(0),
            new InputArgument(true),
            new InputArgument(0),
            new InputArgument(0),
            new InputArgument(false));
    }

    private void InitMaps()
    {
        _maps.Clear();
        if (_loadedConfig != null && _loadedConfig.Maps != null && _loadedConfig.Maps.Count > 0)
        {
            _maps.AddRange(_loadedConfig.Maps);
        }
        else
        {
            var def = new MapDefinition
            {
                Name = "DefaultArena",
                SiteA = _siteRegistry.Copy("A"),
                SiteB = _siteRegistry.Copy("B")
            };
            _maps.Add(def);
        }

        _mapIndex = 0;
        if (_maps.Count > 0)
        {
            _siteRegistry.SetSites(_maps[_mapIndex].SiteA, _maps[_mapIndex].SiteB);
        }
    }

    private void InitLoadoutAllowlists()
    {
        foreach (string weapon in _weaponOptions)
        {
            _botAllowedWeapons[weapon] = true;
            _playerAllowedWeapons[weapon] = true;
        }
    }

    private void InitCallouts()
    {
        var list = new List<CalloutItem>
        {
            new CalloutItem{ Label = "Follow Me", Command = "follow" },
            new CalloutItem{ Label = "Hold Here", Command = "hold" },
            new CalloutItem{ Label = "Charge", Command = "charge" },
            new CalloutItem{ Label = "Split Sites", Command = "split" },
            new CalloutItem{ Label = "Charge Site A", Command = "charge_a" },
            new CalloutItem{ Label = "Charge Site B", Command = "charge_b" }
        };
        _calloutMenu.SetItems(list, cmd => OnCalloutCommand(cmd));
    }

    private void CycleDifficulty()
    {
        _difficultyIndex = (_difficultyIndex + 1) % _difficulties.Length;
        SaveConfig();
    }

    private int CycleInt(int value, int min, int maxInclusive)
    {
        int next = value + 1;
        if (next > maxInclusive) next = min;
        return next;
    }

    private void NextMap()
    {
        if (_maps.Count == 0) return;
        _mapIndex = (_mapIndex + 1) % _maps.Count;
        MapDefinition map = _maps[_mapIndex];
        _siteRegistry.SetSites(map.SiteA, map.SiteB);
        SaveConfig();
    }

    private void AddMapFromSites()
    {
        var map = new MapDefinition
        {
            Name = $"CustomMap_{_maps.Count + 1}",
            SiteA = _siteRegistry.Copy("A"),
            SiteB = _siteRegistry.Copy("B")
        };
        _maps.Add(map);
        _mapIndex = _maps.Count - 1;
        _siteRegistry.SetSites(map.SiteA, map.SiteB);
        _notify.Enqueue($"[Match] Added {map.Name} from current sites.");
        SaveConfig();
    }

    private MapDefinition ActiveMap()
    {
        if (_mapIndex >= 0 && _mapIndex < _maps.Count) return _maps[_mapIndex];
        return null;
    }

    private void AddTeamASpawnFromPlayer()
    {
        Ped ped = Game.Player.Character;
        var map = ActiveMap();
        if (ped == null || !ped.Exists() || map == null) return;
        SpawnPoint sp = _siteRegistry.CreateSpawnFromPed(ped);
        map.TeamASpawns.Add(sp);
        _notify.Enqueue($"[Match] Added Team A spawn #{map.TeamASpawns.Count}");
        SaveConfig();
    }

    private void AddTeamBSpawnFromPlayer()
    {
        Ped ped = Game.Player.Character;
        var map = ActiveMap();
        if (ped == null || !ped.Exists() || map == null) return;
        SpawnPoint sp = _siteRegistry.CreateSpawnFromPed(ped);
        map.TeamBSpawns.Add(sp);
        _notify.Enqueue($"[Match] Added Team B spawn #{map.TeamBSpawns.Count}");
        SaveConfig();
    }

    private void CyclePlayerWeapon()
    {
        _playerWeaponIndex = (_playerWeaponIndex + 1) % _weaponOptions.Length;
        SaveConfig();
    }

    private void CycleBotWeapon()
    {
        _botWeaponIndex = (_botWeaponIndex + 1) % _weaponOptions.Length;
        SaveConfig();
    }

    private void CycleStartingMoney()
    {
        int[] options = new[] { 500, 800, 1200, 2000, 4000 };
        int idx = System.Array.IndexOf(options, _startingMoney);
        idx = (idx + 1) % options.Length;
        _startingMoney = options[idx];
        SaveConfig();
    }

    private void CycleTheme()
    {
        _themeIndex = (_themeIndex + 1) % _themes.Length;
        _menu.ApplyTheme(_themes[_themeIndex]);
        SaveConfig();
    }

    private List<BuyCategory> BuildBuyCategories()
    {
        var cats = new List<BuyCategory>();

        cats.Add(new BuyCategory
        {
            Name = "Repeaters",
            Items = new List<BuyItem>
            {
                new BuyItem{ Pretty="Repeater Carbine", Weapon="WEAPON_REPEATER_CARBINE", Price=600, Slot="Primary"},
                new BuyItem{ Pretty="Repeater Evans", Weapon="WEAPON_REPEATER_EVANS", Price=750, Slot="Primary"},
                new BuyItem{ Pretty="Repeater Litchfield", Weapon="WEAPON_REPEATER_LITCHFIELD", Price=850, Slot="Primary"},
            }
        });

        cats.Add(new BuyCategory
        {
            Name = "Shotguns",
            Items = new List<BuyItem>
            {
                new BuyItem{ Pretty="Pump Shotgun", Weapon="WEAPON_SHOTGUN_PUMP", Price=900, Slot="Primary"},
                new BuyItem{ Pretty="Semi Shotgun", Weapon="WEAPON_SHOTGUN_SEMIAUTO", Price=1100, Slot="Primary"},
            }
        });

        cats.Add(new BuyCategory
        {
            Name = "Light Pistols",
            Items = new List<BuyItem>
            {
                new BuyItem{ Pretty="Cattleman Revolver", Weapon="WEAPON_REVOLVER_CATTLEMAN", Price=300, Slot="Sidearm"},
                new BuyItem{ Pretty="Volcanic Pistol", Weapon="WEAPON_PISTOL_VOLCANIC", Price=500, Slot="Sidearm"},
            }
        });

        cats.Add(new BuyCategory
        {
            Name = "Heavy Pistols",
            Items = new List<BuyItem>
            {
                new BuyItem{ Pretty="Schofield Revolver", Weapon="WEAPON_REVOLVER_SCHOFIELD", Price=550, Slot="Sidearm"},
                new BuyItem{ Pretty="LeMat Revolver", Weapon="WEAPON_REVOLVER_LEMAT", Price=700, Slot="Sidearm"},
            }
        });

        cats.Add(new BuyCategory
        {
            Name = "Utility",
            Items = new List<BuyItem>
            {
                new BuyItem{ Pretty="Bow", Weapon="WEAPON_BOW", Price=400, Slot="Primary"},
                new BuyItem{ Pretty="Throwing Knives", Weapon="WEAPON_THROWN_THROWING_KNIVES", Price=200, Slot="Utility"}
            }
        });

        cats.Add(new BuyCategory
        {
            Name = "Armor",
            Items = new List<BuyItem>
            {
                new BuyItem{ Pretty="Light Armor", Weapon="ARMOR_LIGHT", Price=300, Slot="Armor"},
                new BuyItem{ Pretty="Heavy Armor", Weapon="ARMOR_HEAVY", Price=600, Slot="Armor"},
            }
        });

        cats.Add(new BuyCategory
        {
            Name = "Horses",
            Items = new List<BuyItem>
            {
                new BuyItem{ Pretty="Riding Horse", Weapon="HORSE_RIDING", Price=800, Slot="Mount"}
            }
        });

        return cats;
    }

    private void OnBuySelection(BuyItem item)
    {
        if (item == null) return;
        if (_playerMoney < item.Price)
        {
            _notify.Enqueue("Not enough money.");
            return;
        }

        _playerMoney -= item.Price;
        int idx = Array.IndexOf(_weaponOptions, item.Weapon);
        if (idx >= 0)
        {
            _playerWeaponIndex = idx;
            _playerLoadoutWeapon = item.Weapon;
            ClearPlayerWeapons();
            GivePlayerWeapon(item.Weapon);
            _notify.Enqueue($"Purchased {item.Pretty}.");
            _buyMenu.SetPlayerMoney(_playerMoney);
            SaveConfig();
        }
    }

    private string ThemeName(int idx)
    {
        switch (idx)
        {
            case 0: return "Dark";
            case 1: return "HighContrast";
            case 2: return "Light";
            case 3: return "RockstarMuted";
            case 4: return "RockstarLight";
            default: return "Dark";
        }
    }

    private void SavePreset()
    {
        _savedPreset = new MatchPreset
        {
            Name = "Preset_1",
            DifficultyIndex = _difficultyIndex,
            TeamABots = _teamABots,
            TeamBBots = _teamBBots,
            MapIndex = _mapIndex,
            StartingMoney = _startingMoney,
            PlayerWeaponIndex = _playerWeaponIndex,
            BotWeaponIndex = _botWeaponIndex
        };
        _notify.Enqueue("[Match] Preset saved.");
        SaveConfig();
    }

    private void LoadPreset()
    {
        if (_savedPreset == null)
        {
            _notify.Enqueue("[Match] No preset saved.");
            return;
        }

        _difficultyIndex = _savedPreset.DifficultyIndex;
        _teamABots = _savedPreset.TeamABots;
        _teamBBots = _savedPreset.TeamBBots;
        _startingMoney = _savedPreset.StartingMoney;
        _playerWeaponIndex = _savedPreset.PlayerWeaponIndex;
        _botWeaponIndex = _savedPreset.BotWeaponIndex;

        if (_savedPreset.MapIndex >= 0 && _savedPreset.MapIndex < _maps.Count)
        {
            _mapIndex = _savedPreset.MapIndex;
            MapDefinition map = _maps[_mapIndex];
            _siteRegistry.SetSites(map.SiteA, map.SiteB);
        }

        _notify.Enqueue("[Match] Preset loaded.");
        SaveConfig();
    }

    private void RegisterActions()
    {
        _actions["match_create"] = StartMatch;
        _actions["match_stop"] = StopMatch;
        _actions["match_cycle_difficulty"] = CycleDifficulty;
        _actions["match_team_a_bots"] = () => { _teamABots = CycleInt(_teamABots, 0, 10); SaveConfig(); };
        _actions["match_team_b_bots"] = () => { _teamBBots = CycleInt(_teamBBots, 0, 10); SaveConfig(); };
        _actions["match_next_map"] = NextMap;
        _actions["match_add_map_from_sites"] = AddMapFromSites;
        _actions["match_add_spawn_a"] = AddTeamASpawnFromPlayer;
        _actions["match_add_spawn_b"] = AddTeamBSpawnFromPlayer;

        _actions["teleport_arena"] = TeleportToArena;
        _actions["dump_coords"] = DumpPlayerCoords;
        _actions["ragdoll"] = RagdollPlayer;
        _actions["site_set_a"] = () => SetActiveSite("A");
        _actions["site_set_b"] = () => SetActiveSite("B");
        _actions["site_capture"] = CaptureSiteFromPlayer;
        _actions["site_dump"] = DumpSites;
        _actions["bot_spawn_player"] = SpawnBotAtPlayer;
        _actions["bot_spawn_site"] = SpawnBotAtActiveSite;
        _actions["bot_delete"] = _bots.DeleteAll;

        _actions["loadout_cycle_player_weapon"] = CyclePlayerWeapon;
        _actions["loadout_cycle_bot_weapon"] = CycleBotWeapon;
        _actions["loadout_cycle_money"] = CycleStartingMoney;
        _actions["settings_theme"] = CycleTheme;
        _actions["settings_autosave"] = () => { _autosaveMatches = !_autosaveMatches; SaveConfig(); };
        _actions["settings_save_preset"] = SavePreset;
        _actions["settings_load_preset"] = LoadPreset;
    }

    private void BuildMenuPages()
    {
        _menu.AddPage(BuildMatchPage());
        _menu.AddPage(BuildLoadoutPage());
        _menu.AddPage(BuildSettingsPage());
        _menu.AddPage(BuildDebugPage());
    }

    private DebugMenuPage BuildMatchPage()
    {
        var page = new DebugMenuPage { Title = "Match" };

        page.Items.Add(new DebugMenuItem
        {
            Label = "Create Match",
            GetValue = () => _core.IsRunning ? "Running" : "Stopped",
            OnActivate = () => InvokeAction("match_create")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Stop Match",
            OnActivate = () => InvokeAction("match_stop")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Match Difficulty",
            GetValue = () => _difficulties[_difficultyIndex],
            OnActivate = () => InvokeAction("match_cycle_difficulty")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Team A Bots",
            GetValue = () => _teamABots.ToString(),
            OnActivate = () => InvokeAction("match_team_a_bots")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Team B Bots",
            GetValue = () => _teamBBots.ToString(),
            OnActivate = () => InvokeAction("match_team_b_bots")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Map Selection",
            GetValue = () => _maps.Count > 0 ? _maps[_mapIndex].Name : "none",
            OnActivate = () => InvokeAction("match_next_map")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Add Map From Current Sites",
            OnActivate = () => InvokeAction("match_add_map_from_sites")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Add Team A Spawn (player pos)",
            OnActivate = () => InvokeAction("match_add_spawn_a")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Add Team B Spawn (player pos)",
            OnActivate = () => InvokeAction("match_add_spawn_b")
        });

        return page;
    }

    private DebugMenuPage BuildLoadoutPage()
    {
        var page = new DebugMenuPage { Title = "Loadout" };

        page.Items.Add(new DebugMenuItem
        {
            Label = "Starting Money",
            GetValue = () => $"${_startingMoney}",
            OnActivate = () => InvokeAction("loadout_cycle_money")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Player Spawn Weapon",
            GetValue = () => _weaponOptions[_playerWeaponIndex],
            OnActivate = () => InvokeAction("loadout_cycle_player_weapon")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Bot Spawn Weapon",
            GetValue = () => _weaponOptions[_botWeaponIndex],
            OnActivate = () => InvokeAction("loadout_cycle_bot_weapon")
        });

        foreach (string weapon in _weaponOptions)
        {
            page.Items.Add(new DebugMenuItem
            {
                Label = $"Bot Allow: {weapon}",
                GetValue = () => _botAllowedWeapons.ContainsKey(weapon) && _botAllowedWeapons[weapon] ? "Allowed" : "Blocked",
                OnActivate = () =>
                {
                    _botAllowedWeapons[weapon] = !_botAllowedWeapons[weapon];
                    SaveConfig();
                }
            });

            page.Items.Add(new DebugMenuItem
            {
                Label = $"Player Allow: {weapon}",
                GetValue = () => _playerAllowedWeapons.ContainsKey(weapon) && _playerAllowedWeapons[weapon] ? "Allowed" : "Blocked",
                OnActivate = () =>
                {
                    _playerAllowedWeapons[weapon] = !_playerAllowedWeapons[weapon];
                    SaveConfig();
                }
            });
        }

        return page;
    }

    private DebugMenuPage BuildSettingsPage()
    {
        var page = new DebugMenuPage { Title = "Settings" };

        page.Items.Add(new DebugMenuItem
        {
            Label = "UI Theme",
            GetValue = () => ThemeName(_themeIndex),
            OnActivate = () => InvokeAction("settings_theme")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Autosave Matches",
            GetValue = () => _autosaveMatches ? "On" : "Off",
            OnActivate = () => InvokeAction("settings_autosave")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Save Match Preset",
            GetValue = () => _savedPreset != null ? _savedPreset.Name : "Empty",
            OnActivate = () => InvokeAction("settings_save_preset")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Load Match Preset",
            OnActivate = () => InvokeAction("settings_load_preset")
        });

        // Keybinds
        foreach (KeybindEntry entry in GetActionEntries())
        {
            var bindEntry = new KeybindEntry
            {
                Id = entry.Id,
                Label = entry.Label,
                Key = _keybinds.GetBinding(entry.Id),
                OnRebind = key =>
                {
                    if (key == Keys.F7) { RScreen.DisplaySubtitle("[Menu] F7 reserved for menu"); return; }
                    _keybinds.SetBinding(entry.Id, key);
                    SaveConfig();
                }
            };

            page.Items.Add(new DebugMenuItem
            {
                Label = entry.Label,
                IsBinding = true,
                BindEntry = bindEntry,
                OnClearBinding = () => { _keybinds.ClearBinding(entry.Id); SaveConfig(); },
                GetValue = () => DescribeKey(entry.Id)
            });
        }

        return page;
    }

    private DebugMenuPage BuildDebugPage()
    {
        var page = new DebugMenuPage { Title = "Debug" };

        page.Items.Add(new DebugMenuItem
        {
            Label = "Teleport to Arena",
            OnActivate = () => InvokeAction("teleport_arena")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Dump Player Coords",
            OnActivate = () => InvokeAction("dump_coords")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Ragdoll Player",
            OnActivate = () => InvokeAction("ragdoll")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Active Site (A/B)",
            GetValue = () => _activeSiteLabel,
            OnActivate = () => SetActiveSite(_activeSiteLabel == "A" ? "B" : "A")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Capture Active Site (player pos)",
            OnActivate = () => InvokeAction("site_capture")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Dump Sites",
            OnActivate = () => InvokeAction("site_dump")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Add Map From Sites",
            OnActivate = () => InvokeAction("match_add_map_from_sites")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Spawn Bot at Player",
            OnActivate = () => InvokeAction("bot_spawn_player")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Spawn Bot at Active Site",
            OnActivate = () => InvokeAction("bot_spawn_site")
        });

        page.Items.Add(new DebugMenuItem
        {
            Label = "Delete All Bots",
            OnActivate = () => InvokeAction("bot_delete")
        });

        // Future debug tools could be appended here (nav overlays, perf HUD, etc.)

        return page;
    }

    private System.Collections.Generic.IEnumerable<KeybindEntry> GetActionEntries()
    {
        yield return new KeybindEntry { Id = "match_create", Label = "Create Match" };
        yield return new KeybindEntry { Id = "match_stop", Label = "Stop Match" };
        yield return new KeybindEntry { Id = "match_next_map", Label = "Next Map" };
        yield return new KeybindEntry { Id = "teleport_arena", Label = "Teleport to Arena" };
        yield return new KeybindEntry { Id = "dump_coords", Label = "Dump Player Coords" };
        yield return new KeybindEntry { Id = "ragdoll", Label = "Ragdoll Player" };
        yield return new KeybindEntry { Id = "site_set_a", Label = "Set Site Slot A" };
        yield return new KeybindEntry { Id = "site_set_b", Label = "Set Site Slot B" };
        yield return new KeybindEntry { Id = "site_capture", Label = "Capture Active Site" };
        yield return new KeybindEntry { Id = "site_dump", Label = "Dump Sites" };
        yield return new KeybindEntry { Id = "bot_spawn_player", Label = "Spawn Bot at Player" };
        yield return new KeybindEntry { Id = "bot_spawn_site", Label = "Spawn Bot at Active Site" };
        yield return new KeybindEntry { Id = "bot_delete", Label = "Delete All Bots" };
    }

    private string DescribeKey(string actionId)
    {
        var bound = _keybinds.GetBinding(actionId);
        return bound.HasValue ? bound.Value.ToString() : "Unbound";
    }

    private bool InvokeAction(string actionId)
    {
        if (_actions.TryGetValue(actionId, out System.Action action))
        {
            action();
            return true;
        }

        RScreen.DisplaySubtitle($"[Menu] Action {actionId} missing");
        return false;
    }

    private void LoadConfig()
    {
        _loadedConfig = _configStore.Load();
        if (_loadedConfig == null) _loadedConfig = new PersistedConfig();

        _themeIndex = ClampIndex(_loadedConfig.ThemeIndex, _themes.Length);
        _difficultyIndex = ClampIndex(_loadedConfig.DifficultyIndex, _difficulties.Length);
        _teamABots = _loadedConfig.TeamABots > 0 ? _loadedConfig.TeamABots : _teamABots;
        _teamBBots = _loadedConfig.TeamBBots > 0 ? _loadedConfig.TeamBBots : _teamBBots;
        _startingMoney = _loadedConfig.StartingMoney > 0 ? _loadedConfig.StartingMoney : _startingMoney;
        _playerMoney = _loadedConfig.PlayerMoney > 0 ? _loadedConfig.PlayerMoney : _startingMoney;
        _playerWeaponIndex = ClampIndex(_loadedConfig.PlayerWeaponIndex, _weaponOptions.Length);
        _botWeaponIndex = ClampIndex(_loadedConfig.BotWeaponIndex, _weaponOptions.Length);

        if (_loadedConfig.Keybinds != null)
        {
            foreach (var kv in _loadedConfig.Keybinds)
            {
                if (Enum.TryParse(kv.Value, out Keys key))
                {
                    _keybinds.SetBinding(kv.Key, key);
                }
            }
        }
    }

    private void SaveConfig()
    {
        var cfg = new PersistedConfig
        {
            ThemeIndex = _themeIndex,
            DifficultyIndex = _difficultyIndex,
            TeamABots = _teamABots,
            TeamBBots = _teamBBots,
            StartingMoney = _startingMoney,
            PlayerMoney = _playerMoney,
            PlayerWeaponIndex = _playerWeaponIndex,
            BotWeaponIndex = _botWeaponIndex,
            Maps = _maps,
            Keybinds = BuildKeybindMap()
        };

        _configStore.Save(cfg);
    }

    private Dictionary<string, string> BuildKeybindMap()
    {
        var dict = new Dictionary<string, string>();
        foreach (KeybindEntry entry in GetActionEntries())
        {
            var bound = _keybinds.GetBinding(entry.Id);
            if (bound.HasValue)
                dict[entry.Id] = bound.Value.ToString();
        }
        return dict;
    }

    private int ClampIndex(int idx, int len)
    {
        if (len <= 0) return 0;
        if (idx < 0) return 0;
        if (idx >= len) return len - 1;
        return idx;
    }

    private void OnCalloutCommand(string cmd)
    {
        _teamCommand = cmd;
        _notify.Enqueue($"[Callout] {cmd}");
        _bots.SetTeamCommand(cmd);
    }

    private sealed class MatchPreset
    {
        public string Name;
        public int DifficultyIndex;
        public int TeamABots;
        public int TeamBBots;
        public int MapIndex;
        public int StartingMoney;
        public int PlayerWeaponIndex;
        public int BotWeaponIndex;
    }
}

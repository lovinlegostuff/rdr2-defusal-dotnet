using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

using Rdr2Defusal.World;

namespace Rdr2Defusal
{
    /// <summary>
    /// Minimal JSON persistence for user settings (themes, keybinds, maps).
    /// </summary>
    public sealed class ConfigStore
    {
        private readonly string _filePath;
        private readonly DataContractJsonSerializer _serializer = new DataContractJsonSerializer(typeof(PersistedConfig));

        public ConfigStore()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Rdr2DefusalData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "config.json");
        }

        public PersistedConfig Load()
        {
            try
            {
                if (!File.Exists(_filePath)) return new PersistedConfig();
                using (var fs = File.OpenRead(_filePath))
                {
                    return (PersistedConfig)_serializer.ReadObject(fs);
                }
            }
            catch
            {
                return new PersistedConfig();
            }
        }

        public void Save(PersistedConfig cfg)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    _serializer.WriteObject(ms, cfg);
                    File.WriteAllBytes(_filePath, ms.ToArray());
                }
            }
            catch
            {
                // ignore disk errors for now
            }
        }
    }

    [System.Runtime.Serialization.DataContract]
    public sealed class PersistedConfig
    {
        [System.Runtime.Serialization.DataMember] public int ThemeIndex { get; set; }
        [System.Runtime.Serialization.DataMember] public int DifficultyIndex { get; set; }
        [System.Runtime.Serialization.DataMember] public int TeamABots { get; set; }
        [System.Runtime.Serialization.DataMember] public int TeamBBots { get; set; }
        [System.Runtime.Serialization.DataMember] public int StartingMoney { get; set; }
        [System.Runtime.Serialization.DataMember] public int PlayerMoney { get; set; }
        [System.Runtime.Serialization.DataMember] public int PlayerWeaponIndex { get; set; }
        [System.Runtime.Serialization.DataMember] public int BotWeaponIndex { get; set; }
        [System.Runtime.Serialization.DataMember] public List<MapDefinition> Maps { get; set; } = new List<MapDefinition>();
        [System.Runtime.Serialization.DataMember] public Dictionary<string, string> Keybinds { get; set; } = new Dictionary<string, string>();
    }
}

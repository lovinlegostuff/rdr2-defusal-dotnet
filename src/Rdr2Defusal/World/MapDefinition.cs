using System.Runtime.Serialization;

namespace Rdr2Defusal.World
{
    /// <summary>
    /// Simple map profile for storing site A/B coordinates and heading.
    /// This keeps things DTO-like so we can persist later.
    /// </summary>
    [DataContract]
    public sealed class MapDefinition
    {
        [DataMember] public string Name { get; set; }
        [DataMember] public SiteDefinition SiteA { get; set; }
        [DataMember] public SiteDefinition SiteB { get; set; }
        [DataMember] public System.Collections.Generic.List<SpawnPoint> TeamASpawns { get; set; } = new System.Collections.Generic.List<SpawnPoint>();
        [DataMember] public System.Collections.Generic.List<SpawnPoint> TeamBSpawns { get; set; } = new System.Collections.Generic.List<SpawnPoint>();
    }

    [DataContract]
    public sealed class SpawnPoint
    {
        [DataMember] public float X { get; set; }
        [DataMember] public float Y { get; set; }
        [DataMember] public float Z { get; set; }
        [DataMember] public float H { get; set; }
    }
}

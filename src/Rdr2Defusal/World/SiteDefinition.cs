using System.Runtime.Serialization;

namespace Rdr2Defusal.World
{
    /// <summary>
    /// Simple DTO for a site. We keep raw floats to avoid Vector3 compile issues.
    /// Roadmap:
    /// - Add bounding volumes (radius/height) for plant/defuse checks.
    /// - Attach nav markers/cover points.
    /// - Track "lane" metadata (long/short/connector) to guide rotations.
    /// - Add display names and per-map IDs for persistence across matches.
    /// </summary>
    [DataContract]
    public sealed class SiteDefinition
    {
        [DataMember] public string Label { get; set; }
        [DataMember] public float X { get; set; }
        [DataMember] public float Y { get; set; }
        [DataMember] public float Z { get; set; }
        [DataMember] public float H { get; set; }

        public string Describe()
        {
            return $"{Label}: {X:0.00}, {Y:0.00}, {Z:0.00} | H {H:0.0}";
        }
    }
}

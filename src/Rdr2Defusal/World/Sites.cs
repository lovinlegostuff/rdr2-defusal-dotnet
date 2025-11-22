namespace Rdr2Defusal.World
{
    /// <summary>
    /// Hard-coded arena / site coordinates.
    /// We store raw floats to avoid Vector3 type mismatch between API versions.
    /// </summary>
    public static class Sites
    {
        // Arena center you dumped with F6.
        public const float ArenaX = -1351.43f;
        public const float ArenaY =  2441.27f;
        public const float ArenaZ =   308.42f;
        public const float ArenaH =   310.5f;

        // TEMP default A/B sites (relative-ish). 
        // Once you send me your A/B F6 dumps, we’ll replace these exact numbers.
        public const float SiteAX = ArenaX + 12.0f;
        public const float SiteAY = ArenaY -  6.0f;
        public const float SiteAZ = ArenaZ;

        public const float SiteBX = ArenaX - 14.0f;
        public const float SiteBY = ArenaY +  8.0f;
        public const float SiteBZ = ArenaZ;

        // Simple radii you can tune later
        public const float SiteRadius = 6.5f;
        public const float ArenaRadius = 45.0f;
    }
}

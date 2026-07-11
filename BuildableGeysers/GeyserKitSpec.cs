namespace BuildableGeysers
{
    // Everything a kit building needs to know about the geyser it unpacks into,
    // captured from the live geyser prefab at discovery time (never hardcoded).
    public class GeyserKitSpec
    {
        public string KitId { get; }
        public Tag GeyserPrefabTag { get; }
        public string GeyserName { get; }
        public string AnimName { get; }
        public int Width { get; }
        public int Height { get; }
        public string[] RequiredDlcIds { get; }
        public string[] ForbiddenDlcIds { get; }

        public GeyserKitSpec(
            string kitId, Tag geyserPrefabTag, string geyserName, string animName,
            int width, int height, string[] requiredDlcIds, string[] forbiddenDlcIds)
        {
            KitId = kitId;
            GeyserPrefabTag = geyserPrefabTag;
            GeyserName = geyserName;
            AnimName = animName;
            Width = width;
            Height = height;
            RequiredDlcIds = requiredDlcIds;
            ForbiddenDlcIds = forbiddenDlcIds;
        }
    }
}

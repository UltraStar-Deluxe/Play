public static class PermissionUiUtils
{
    public static string GetPermissionName(HttpApiPermission permission)
    {
        switch (permission)
        {
            case HttpApiPermission.WriteSongQueue:
                return "Edit song queue";
            case HttpApiPermission.WriteConfig:
                return "Edit config";
            case HttpApiPermission.WriteInputSimulation:
                return "Simulate input";
            default:
                return StringUtils.ToTitleCase(permission.ToString());
        }
    }
}

public static class PermissionUiUtils
{
    public static string GetPermissionName(RestApiPermission permission)
    {
        switch (permission)
        {
            case RestApiPermission.WriteSongQueue:
                return "Edit song queue";
            case RestApiPermission.WriteConfig:
                return "Edit config";
            case RestApiPermission.WriteInputSimulation:
                return "Simulate input";
            default:
                return StringUtils.ToTitleCase(permission.ToString());
        }
    }
}

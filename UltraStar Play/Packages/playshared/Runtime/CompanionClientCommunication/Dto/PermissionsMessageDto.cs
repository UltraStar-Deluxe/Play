using System.Collections.Generic;

public class PermissionsMessageDto : CompanionAppMessageDto
{
    public List<RestApiPermission> Permissions { get; set; } = new();

    public PermissionsMessageDto() : base(CompanionAppMessageType.Permissions)
    {
    }
}

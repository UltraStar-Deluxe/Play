using System.Collections.Generic;

public class PermissionsMessageDto : CompanionAppMessageDto
{
    public List<HttpApiPermission> Permissions { get; set; } = new();

    public PermissionsMessageDto() : base(CompanionAppMessageType.Permissions)
    {
    }
}

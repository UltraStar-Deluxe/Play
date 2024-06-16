using System.Collections.Generic;
using UniInject;
using UnityEngine.UIElements;

public class CompanionClientListEntryControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private ICompanionClientHandler clientHandler;

    [Inject]
    private Settings settings;

    [Inject]
    private ServerSideCompanionClientManager serverSideCompanionClientManager;

    [Inject(UxmlName = R.UxmlNames.clientNameLabel)]
    private Label clientNameLabel;

    [Inject(UxmlName = R.UxmlNames.permissionsContainer)]
    private VisualElement permissionsContainer;

    public void OnInjectionFinished()
    {
        clientNameLabel.SetTranslatedText(Translation.Of(clientHandler.ClientName));
        UpdatePermissions();
    }

    private void UpdatePermissions()
    {
        permissionsContainer.Clear();
        List<HttpApiPermission> givenPermissions = SettingsUtils.GetPermissions(settings, clientHandler.ClientId);

        List<HttpApiPermission> permissions = new()
        {
            HttpApiPermission.WriteSongQueue,
            HttpApiPermission.WriteConfig,
            HttpApiPermission.WriteInputSimulation,
        };

        permissions.ForEach(permission =>
        {
            Toggle permissionToggle = new(GetPermissionName(permission));
            permissionToggle.value = givenPermissions.Contains(permission);
            permissionToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    SettingsUtils.AddPermission(settings, clientHandler.ClientId, permission);
                }
                else
                {
                    SettingsUtils.RemovePermission(settings, clientHandler.ClientId, permission);
                }

                List <HttpApiPermission> permissions = SettingsUtils.GetPermissions(settings, clientHandler.ClientId);
                clientHandler.SendMessageToClient(new PermissionsMessageDto()
                {
                    Permissions = permissions,
                });
            });

            permissionsContainer.Add(permissionToggle);
        });
    }

    private string GetPermissionName(HttpApiPermission permission)
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

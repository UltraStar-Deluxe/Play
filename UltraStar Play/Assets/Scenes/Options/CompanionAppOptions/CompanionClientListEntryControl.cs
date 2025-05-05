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

    public void UpdatePermissions()
    {
        permissionsContainer.Clear();
        List<RestApiPermission> givenPermissions = SettingsUtils.GetPermissions(settings, clientHandler.ClientId);

        List<RestApiPermission> permissions = new()
        {
            RestApiPermission.WriteSongQueue,
            RestApiPermission.WriteConfig,
            RestApiPermission.WriteInputSimulation,
        };

        permissions.ForEach(permission =>
        {
            Toggle permissionToggle = new(PermissionUiUtils.GetPermissionName(permission));
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

                List<RestApiPermission> newPermissions = SettingsUtils.GetPermissions(settings, clientHandler.ClientId);
                clientHandler.SendMessageToClient(new PermissionsMessageDto()
                {
                    Permissions = newPermissions,
                });
            });

            permissionsContainer.Add(permissionToggle);
        });
        permissionsContainer.SetVisibleByDisplay(settings.RequireCompanionClientPermission);
    }
}

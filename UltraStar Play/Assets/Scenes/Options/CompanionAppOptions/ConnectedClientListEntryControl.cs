using System.Collections.Generic;
using UniInject;
using UnityEngine.UIElements;

public class ConnectedClientListEntryControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private IConnectedClientHandler clientHandler;
    
    [Inject]
    private Settings settings;
    
    [Inject(UxmlName = R.UxmlNames.clientNameLabel)]
    private Label clientNameLabel;
    
    [Inject(UxmlName = R.UxmlNames.permissionsContainer)]
    private VisualElement permissionsContainer;
    
    public void OnInjectionFinished()
    {
        clientNameLabel.text = clientHandler.ClientName;
        UpdatePermissions();
    }

    private void UpdatePermissions()
    {
        permissionsContainer.Clear();
        List<HttpApiPermission> givenPermissions = SettingsUtils.GetPermissions(settings, clientHandler.ClientId);

        EnumUtils.GetValuesAsList<HttpApiPermission>().ForEach(permission =>
        {
            Toggle permissionToggle = new(StringUtils.ToTitleCase(permission.ToString()));
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
            });
            
            permissionsContainer.Add(permissionToggle);
        });
    }
}

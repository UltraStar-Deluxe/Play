using UniRx;

public class CompanionAppNetworkConfigControl : NetworkConfigControl
{
    private Settings Settings => base.settings as Settings;

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();

        // Only show controls when dev mode is enabled.
        UpdateNetworkConfigVisibility();
        Settings.ObserveEveryValueChanged(it => it.IsDevModeEnabled)
            .Subscribe(_ => UpdateNetworkConfigVisibility())
            .AddTo(gameObject);
    }

    private void UpdateNetworkConfigVisibility()
    {
        networkConfigContainer.SetVisibleByDisplay(Settings.IsDevModeEnabled);
    }
}

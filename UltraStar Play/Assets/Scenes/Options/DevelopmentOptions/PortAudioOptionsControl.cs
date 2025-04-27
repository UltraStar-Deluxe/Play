using System.Collections.Generic;
using System.Linq;
using System.Text;
using PortAudioForUnity;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class PortAudioOptionsControl : IInjectionFinishedListener, INeedInjection
{
    [Inject]
    private DialogManager dialogManager;

    [Inject]
    private Settings settings;

    [Inject]
    private GameObject gameObject;

    [Inject(UxmlName = R.UxmlNames.portAudioOutputDeviceChooser)]
    private Chooser portAudioOutputDeviceChooser;

    [Inject(UxmlName = R.UxmlNames.portAudioHostApiChooser)]
    private Chooser portAudioHostApiChooser;

    [Inject(UxmlName = R.UxmlNames.portAudioDeviceInfoButton)]
    private Button portAudioDeviceInfoButton;

    public void OnInjectionFinished()
    {
        // PortAudio device info
        portAudioDeviceInfoButton.RegisterCallbackButtonTriggered(_ => ShowPortAudioDeviceInfo());

        // PortAudio host API
        new EnumChooserControl<PortAudioHostApi>(portAudioHostApiChooser, GetAvailablePortAudioHostApis())
            .Bind(() => settings.PortAudioHostApi,
                newValue => settings.PortAudioHostApi = newValue);

        // PortAudio output device
        LabeledChooserControl<string> portAudioOutputDeviceChooserControl = new(portAudioOutputDeviceChooser,
            GetAvailablePortAudioOutputDeviceNames(),
            item => item.IsNullOrEmpty() ? Translation.Get(R.Messages.common_default) : Translation.Of(item));
        portAudioOutputDeviceChooserControl.Bind(
            () => settings.PortAudioOutputDeviceName,
            newValue => settings.PortAudioOutputDeviceName = newValue);

        settings.ObserveEveryValueChanged(it => it.PortAudioHostApi)
            .Subscribe(newValue => portAudioOutputDeviceChooserControl.Items = GetAvailablePortAudioOutputDeviceNames())
            .AddTo(gameObject);
    }

    private List<string> GetAvailablePortAudioOutputDeviceNames()
    {
        return new List<string>() { "", }
            .Union(PortAudioUtils.DeviceInfos
                .Where(deviceInfo => deviceInfo.MaxOutputChannels > 0
                                     && deviceInfo.HostApi == MicrophoneAdapter.GetHostApi())
                .Select(deviceInfo => deviceInfo.Name))
            .ToList();
    }

    private List<PortAudioHostApi> GetAvailablePortAudioHostApis()
    {
        return new List<PortAudioHostApi>() { PortAudioHostApi.Default }
            .Union(PortAudioUtils.HostApis
                .Select(portAudioHostApi => PortAudioConversionUtils.ConvertHostApi(portAudioHostApi))
                .ToList())
            .ToList();
    }

    private void ShowPortAudioDeviceInfo()
    {
        MessageDialogControl messageDialogControl =
            dialogManager.CreateDialogControl(Translation.Get(R.Messages.options_development_portAudioDialog_title));
        messageDialogControl.AddButton(Translation.Get(R.Messages.options_development_action_copyCsv),
            _ => CopyPortAudioDeviceListCsv());
        messageDialogControl.AddButton(Translation.Get(R.Messages.action_close),
            _ => messageDialogControl.CloseDialog());

        Label defaultHostApiLabel = new Label();
        defaultHostApiLabel.text = $"Default host API: {PortAudioConversionUtils.GetDefaultHostApi()}";
        messageDialogControl.AddVisualElement(defaultHostApiLabel);

        foreach (HostApiInfo hostApiInfo in PortAudioUtils.HostApiInfos)
        {
            // Add group for this host API
            AccordionItem accordionItem = new(StringUtils.EscapeLineBreaks(hostApiInfo.Name));
            messageDialogControl.AddVisualElement(accordionItem);

            // Add label for each device of this host API
            foreach (DeviceInfo deviceInfo in PortAudioUtils.DeviceInfos)
            {
                if (deviceInfo.HostApi != hostApiInfo.HostApi)
                {
                    continue;
                }

                Label deviceInfoLabel = new();
                deviceInfoLabel.name = $"deviceInfoLabel";
                deviceInfoLabel.AddToClassList("deviceInfoLabel");
                string inputOutputIcons = GetInputOutputIcons(deviceInfo);
                deviceInfoLabel.text = $"{inputOutputIcons} '{deviceInfo.Name}'," +
                                       $" max input channels: {deviceInfo.MaxInputChannels}," +
                                       $" max output channels: {deviceInfo.MaxOutputChannels}," +
                                       $" default sample rate: {deviceInfo.DefaultSampleRate.ToStringInvariantCulture("0")}," +
                                       $" default low input latency: {deviceInfo.DefaultLowInputLatency.ToStringInvariantCulture()}," +
                                       $" default high input latency: {deviceInfo.DefaultHighInputLatency.ToStringInvariantCulture()}," +
                                       $" default low output latency: {deviceInfo.DefaultLowOutputLatency.ToStringInvariantCulture()}," +
                                       $" default high output latency: {deviceInfo.DefaultHighOutputLatency.ToStringInvariantCulture()}," +
                                       $" host API device index: {deviceInfo.HostApiDeviceIndex}," +
                                       $" global device index: {deviceInfo.GlobalDeviceIndex}";
                accordionItem.Add(deviceInfoLabel);
            }

            // Add label for default input / output device
            DeviceInfo defaultInputDevice = PortAudioUtils.DeviceInfos.FirstOrDefault(it =>
                it.GlobalDeviceIndex == hostApiInfo.DefaultInputDeviceGlobalIndex);
            DeviceInfo defaultOutputDevice = PortAudioUtils.DeviceInfos.FirstOrDefault(it =>
                it.GlobalDeviceIndex == hostApiInfo.DefaultOutputDeviceGlobalIndex);
            Label defaultDeviceLabel = new();
            defaultDeviceLabel.text =
                $"Default input device: '{defaultInputDevice?.Name}', default output device: '{defaultOutputDevice?.Name}'";
            accordionItem.Add(defaultDeviceLabel);
        }
    }

    private void CopyPortAudioDeviceListCsv()
    {
        // TODO: use CSV lib with proper link between column header and values
        StringBuilder sb = new();

        // Add header
        List<string> headers = new()
        {
            "host API",
            "input/output",
            "device name",
            "max input channels",
            "max output channels",
            "default sample rate",
            "default low input latency",
            "default high input latency",
            "default low output latency",
            "default high output latency",
            "host API device index",
            "global device index",
        };
        string headerCsv = headers
            .Select(it => $"\"{it}\"")
            .JoinWith(", ");
        sb.Append(headerCsv);
        sb.Append("\n");

        // Add values
        foreach (DeviceInfo deviceInfo in PortAudioUtils.DeviceInfos)
        {
            string nameWithoutLineBreaks = StringUtils.EscapeLineBreaks(deviceInfo.Name);
            List<string> values = new() {
                deviceInfo.HostApi.ToString(),
                GetInputOutputIcons(deviceInfo),
                nameWithoutLineBreaks,
                deviceInfo.MaxInputChannels.ToString(),
                deviceInfo.MaxOutputChannels.ToString(),
                deviceInfo.DefaultSampleRate.ToStringInvariantCulture("0"),
                deviceInfo.DefaultLowInputLatency.ToStringInvariantCulture(),
                deviceInfo.DefaultHighInputLatency.ToStringInvariantCulture(),
                deviceInfo.DefaultLowOutputLatency.ToStringInvariantCulture(),
                deviceInfo.DefaultHighOutputLatency.ToStringInvariantCulture(),
                deviceInfo.HostApiDeviceIndex.ToString(),
                deviceInfo.GlobalDeviceIndex.ToString(),
            };
            string valuesCsv = values
                .Select(it => $"\"{it}\"")
                .JoinWith(", ");
            sb.Append(valuesCsv);
            sb.Append("\n");
        }

        ClipboardUtils.CopyToClipboard(sb.ToString());

        NotificationManager.CreateNotification(Translation.Get(R.Messages.common_copiedToClipboard));
    }

    private string GetInputOutputIcons(DeviceInfo deviceInfo)
    {
        string inputIcon = deviceInfo.MaxInputChannels > 0
            ? "🎤"
            : "";
        string outputIcon = deviceInfo.MaxOutputChannels > 0
            ? "🔈"
            : "";
        return $"{inputIcon}{outputIcon}";
    }
}

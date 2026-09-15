using System;
using UniInject;
using UnityEngine.UIElements;

public class SongPackageCardControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.title)]
    private Label title;
    
    [Inject(UxmlName = R.UxmlNames.description)]
    private Label description;
    
    [Inject(UxmlName = R.UxmlNames.url)]
    private Label url;
    
    [Inject(UxmlName = R.UxmlNames.actionButton)]
    private Button actionButton;

    private readonly SongArchiveEntry songArchiveEntry;
    private readonly Action onClick;

    public string Title
    {
        get => title.text;
        set => title.text = value;
    }

    public string ActionTitle
    {
        get => actionButton.text;
        set => actionButton.text = value;
    }

    public SongPackageCardControl(SongArchiveEntry songArchiveEntry, Action onClick)
    {
        this.songArchiveEntry = songArchiveEntry;
        this.onClick = onClick;
    }

    public void OnInjectionFinished()
    {
        title.text = songArchiveEntry.name;
        description.text = songArchiveEntry.description;
        url.text = songArchiveEntry.url;
        actionButton.RegisterCallbackButtonTriggered(_ => onClick());
    }
}


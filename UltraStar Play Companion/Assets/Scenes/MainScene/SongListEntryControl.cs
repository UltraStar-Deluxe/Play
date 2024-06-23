using UniInject;
using UnityEngine.UIElements;

public class SongListEntryControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }
    
    [Inject(UxmlName = R.UxmlNames.songListEntryTitleLabel)]
    private Label songListEntryTitleLabel;
    
    [Inject(UxmlName = R.UxmlNames.songListEntryArtistLabel)]
    private Label songListEntryArtistLabel;
    
    [Inject(UxmlName = R.UxmlNames.songListEntryButton)]
    private Button songListEntryButton;

    [Inject]
    private SongDetailsControl songDetailsControl;
    
    private SongDto songDto;
    public SongDto SongDto
    {
        get => songDto;
        set
        {
            songDto = value;
            UpdateLabels();
        }
    }

    private void UpdateLabels()
    {
        if (songDto == null)
        {
            songListEntryTitleLabel.text = "";
            songListEntryArtistLabel.text = "";
            return;
        }
        
        songListEntryTitleLabel.SetVisibleByDisplay(!songDto.Title.IsNullOrEmpty());
        songListEntryTitleLabel.text = ObjectUtils.NullableToString(songDto.Title, "");
        
        songListEntryArtistLabel.SetVisibleByDisplay(!songDto.Artist.IsNullOrEmpty());
        songListEntryArtistLabel.text = ObjectUtils.NullableToString(songDto.Artist, "");
    }

    public void OnInjectionFinished()
    {
        VisualElement.userData = this;
        songListEntryButton.RegisterCallbackButtonTriggered(_ => OpenSongDetails());
    }

    private void OpenSongDetails()
    {
        if (songDto == null)
        {
            return;
        }

        songDetailsControl.ShowSongDetails(songDto);
    }
}

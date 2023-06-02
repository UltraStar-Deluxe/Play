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
    private SongDto songDto;
    
    [Inject]
    private SongDetailsControl songDetailsControl;
    
    public void OnInjectionFinished()
    {
        songListEntryTitleLabel.SetVisibleByDisplay(!songDto.Title.IsNullOrEmpty());
        songListEntryTitleLabel.text = ObjectUtils.NullableToString(songDto.Title, "");
        
        songListEntryArtistLabel.SetVisibleByDisplay(!songDto.Artist.IsNullOrEmpty());
        songListEntryArtistLabel.text = ObjectUtils.NullableToString(songDto.Artist, "");
        
        songListEntryButton.RegisterCallbackButtonTriggered(_ => songDetailsControl.ShowSongDetails(songDto));
    }
}

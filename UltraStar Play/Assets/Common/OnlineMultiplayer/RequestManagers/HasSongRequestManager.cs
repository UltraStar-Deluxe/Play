using UniInject;
using UniRx;

namespace CommonOnlineMultiplayer
{
    public class HasSongRequestManager : AbstractOnlineMultiplayerRequestManager
    {
        public static HasSongRequestManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<HasSongRequestManager>();

        [Inject]
        private SongMetaManager songMetaManager;

        protected override object GetInstance()
        {
            return Instance;
        }

        protected override void InitOnlineMultiplayerRequestHandlers()
        {
            onlineMultiplayerManager.ObservableMessagingControl.RegisterObservedMessageHandler(
                nameof(HasSongRequestDto),
                observedMessage =>
                {
                    HasSongRequestDto requestDto = FastBufferReaderUtils.ReadJsonValuePacked<HasSongRequestDto>(observedMessage.MessagePayload);

                    bool hasSong = songMetaManager.GetSongMetaByGloballyUniqueId(requestDto.GloballyUniqueSongId) != null;
                    HasSongResponseDto responseDto = new HasSongResponseDto(requestDto.GloballyUniqueSongId, hasSong);
                    onlineMultiplayerManager.ObservableMessagingControl.SendResponseMessage(
                        observedMessage,
                        FastBufferWriterUtils.WriteJsonValuePacked(responseDto));
                })
                .AddTo(gameObject);
        }
    }
}

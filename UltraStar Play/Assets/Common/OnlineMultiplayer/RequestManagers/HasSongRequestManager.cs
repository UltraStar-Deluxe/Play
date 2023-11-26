using UniInject;
using UniRx;

namespace CommonOnlineMultiplayer
{
    public class HasSongRequestManager : AbstractSingletonBehaviour, INeedInjection
    {
        public static HasSongRequestManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<HasSongRequestManager>();

        [Inject]
        private OnlineMultiplayerManager onlineMultiplayerManager;

        [Inject]
        private SongMetaManager songMetaManager;

        protected override object GetInstance()
        {
            return Instance;
        }

        protected override void StartSingleton()
        {
            onlineMultiplayerManager.OwnNetcodeClientStartedEventStream
                .Subscribe(_ => InitOnlineMultiplayerRequestHandlers());
        }

        private void InitOnlineMultiplayerRequestHandlers()
        {
            onlineMultiplayerManager.MessagingControl.RegisterNamedMessageHandler(
                nameof(HasSongRequestDto),
                request =>
                {
                    HasSongRequestDto requestDto = FastBufferReaderUtils.ReadJsonValuePacked<HasSongRequestDto>(request.MessagePayload);

                    bool hasSong = songMetaManager.GetSongMetaByGloballyUniqueId(requestDto.GloballyUniqueSongId) != null;
                    HasSongResponseDto responseDto = new HasSongResponseDto(requestDto.GloballyUniqueSongId, hasSong);
                    onlineMultiplayerManager.MessagingControl.SendNamedMessageToClient(
                        onlineMultiplayerManager.MessagingControl.GetResponseMessageName(nameof(HasSongRequestDto)),
                        FastBufferWriterUtils.WriteJsonValuePacked(responseDto),
                        request.SenderNetcodeClientId);
                });
        }
    }
}

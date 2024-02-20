using System.Linq;

namespace CommonOnlineMultiplayer
{
    public static class NetcodeMessageDtoConverterUtils
    {
        public static SingSceneDataDto ToDto(SingSceneData singSceneData)
        {
            return new SingSceneDataDto()
            {
                GloballyUniqueSongMetaIds = singSceneData.SongMetas
                    .Select(songMeta => SongIdManager.GetAndCacheGloballyUniqueId(songMeta))
                    .ToList(),
                StartPaused = singSceneData.StartPaused,
            };
        }

        public static SingSceneData FromDto(SingSceneDataDto dto, SongMetaManager songMetaManager)
        {
            SingSceneData singSceneData = new()
            {
                SongMetas = dto.GloballyUniqueSongMetaIds
                    .Select(globallyUniqueSongId => songMetaManager.GetSongMetaByGloballyUniqueId(globallyUniqueSongId))
                    .ToList(),
                StartPaused = dto.StartPaused,
            };
            return singSceneData;
        }
    }
}

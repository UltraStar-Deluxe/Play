using System.IO;

public abstract class AbstractAvproVideoSupportProvider : AbstractVideoSupportProvider
{
    public override bool IsSupported(string videoUri, bool videoEqualsAudio)
    {
        return !WebRequestUtils.IsHttpOrHttpsUri(videoUri)
               && settings.AvProToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never
               && ApplicationUtils.IsAvproSupportedVideoFormat(Path.GetExtension(videoUri));
    }
}

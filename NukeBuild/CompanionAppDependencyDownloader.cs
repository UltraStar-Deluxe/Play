using Nuke.Common.IO;

namespace DefaultNamespace;

public class CompanionAppDependencyDownloader(
    AbsolutePath unityProjectDir,
    uint cloneDepth
) : BaseDependencyDownloader(unityProjectDir, cloneDepth)
{
}

using NUnit.Framework;

public class PathUtilsTest
{
    [Test]
    public void ShouldReplaceInvalidCharacters()
    {
        Assert.AreEqual("C:\\dummy-path/invalid_-folder_-name", PathUtils.ReplaceInvalidPathChars(
            "C:\\dummy-path/invalid*-folder|-name", '_'));

        Assert.AreEqual("invalid_-file_-name__", PathUtils.ReplaceInvalidFileNameChars(
            "invalid\\-file/-name*|", '_'));

        Assert.AreEqual("_N Sync - I Want You Back", PathUtils.ReplaceInvalidFileNameChars(
            "*N Sync - I Want You Back"));
    }
}

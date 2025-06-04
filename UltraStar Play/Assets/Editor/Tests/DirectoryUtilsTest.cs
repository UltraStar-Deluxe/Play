using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

public class DirectoryUtilsTest
{
#if UNITY_EDITOR_WIN
    [Test]
    public void ShouldReturnParentDirectories()
    {
        string path = "C:/FirstFolder/SecondFolder/ThirdFolder";
        List<DirectoryInfo> parentDirectoriesAndLastSegment = DirectoryUtils.GetParentDirectories(new DirectoryInfo(path), true);
        Assert.That(
            parentDirectoriesAndLastSegment,
            Is.EquivalentTo(new List<DirectoryInfo>()
            {
                new DirectoryInfo("C:/FirstFolder/SecondFolder/ThirdFolder"),
                new DirectoryInfo("C:/FirstFolder/SecondFolder"),
                new DirectoryInfo("C:/FirstFolder"),
                new DirectoryInfo("C:/"),
            }));

        List<DirectoryInfo> parentDirectoriesOnly = DirectoryUtils.GetParentDirectories(new DirectoryInfo(path));
        Assert.That(
            parentDirectoriesOnly,
            Is.EquivalentTo(new List<DirectoryInfo>()
            {
                new DirectoryInfo("C:/FirstFolder/SecondFolder"),
                new DirectoryInfo("C:/FirstFolder"),
                new DirectoryInfo("C:/"),
            }));
    }
#endif
}

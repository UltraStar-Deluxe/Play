using System.IO;
using System.Threading;

public static class FileUtils
{

    public static void MoveFileOverwriteIfExists(string sourceFile, string destinationFile)
    {
        if (File.Exists(destinationFile))
        {
            File.Delete(destinationFile);
        }

        if (File.Exists(destinationFile))
        {
            // Wait a moment such that the OS can delete the file
            Thread.Sleep(100);
        }
        File.Move(sourceFile, destinationFile);
    }
}

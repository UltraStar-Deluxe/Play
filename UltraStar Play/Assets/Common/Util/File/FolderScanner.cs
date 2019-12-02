using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

public class FolderScanner
{
    private readonly string fileExtensionPattern;

    public FolderScanner(string fileExtensionPattern)
    {
        this.fileExtensionPattern = fileExtensionPattern;

        // Checks
        if (this.fileExtensionPattern == null || this.fileExtensionPattern.Trim().Length < 3)
        {
            throw new UnityException("Can not scan for songs. Invalid file extension specified!");
        }
    }

    public List<string> GetFiles(string folder)
    {
        return GetFiles(folder, true);
    }

    public List<string> GetFiles(string folder, bool recursive)
    {
        List<string> result = new List<string>();
        DirectoryInfo dirInfo = new DirectoryInfo(folder);
        if (folder == null || !System.IO.Directory.Exists(folder))
        {
            Debug.LogError("Song folder '" + folder + "' does not exist or can not be read!");
            return result;
        }

        try
        {
            foreach (FileInfo file in dirInfo.GetFiles(fileExtensionPattern))
            {
                result.Add(file.FullName);
            }

            if (recursive)
            {
                foreach (DirectoryInfo innerDirInfo in dirInfo.GetDirectories())
                {
                    result.AddRange(GetFiles(innerDirInfo.FullName, true));
                }
            }
        }
        catch (Exception ex)
        {
            throw new UnityException("Scanning of a folder failed.", ex);
        }

        return result;
    }

}

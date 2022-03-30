﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FolderScanner
{
    private readonly string fileExtensionPattern;
    private readonly bool includeHiddenFolders;

    public FolderScanner(string fileExtensionPattern, bool includeHiddenFolders = false)
    {
        this.fileExtensionPattern = fileExtensionPattern;
        this.includeHiddenFolders = includeHiddenFolders;

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
        if (!includeHiddenFolders && IsHiddenFolder(folder))
        {
            return new List<string>();
        }

        List<string> result = new();
        DirectoryInfo dirInfo = new(folder);
        if (folder == null || !System.IO.Directory.Exists(folder))
        {
            Debug.LogError("Song folder '" + folder + "' does not exist or can not be read!");
            return result;
        }

        try
        {
            foreach (FileInfo file in dirInfo.GetFiles(fileExtensionPattern))
            {
                // Ignore hidden files (notably on MacOS) and licenses.
                string lowerFileName = file.Name.ToLowerInvariant();
                if (!lowerFileName.StartsWith(".")
                    && !lowerFileName.Equals("license.txt"))
                {
                    result.Add(file.FullName);
                }
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

    private static bool IsHiddenFolder(string folder)
    {
        // By convention, a hidden folder starts with a dot
        // (at least on Unix based operating systems such as Linux, MacOS, Android)
        string folderName = Path.GetFileName(folder);
        return folderName.StartsWith(".");
    }

}

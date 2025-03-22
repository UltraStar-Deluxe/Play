using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class WebViewUtils
{
    public const string WebViewScriptsFolderName = "WebViewScripts";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void StaticInit()
    {
        ClearCache();
    }
    private static bool hasScannedJavaScriptFiles;
    private static readonly Dictionary<string, string> hostToWebViewScript = new();
    private static readonly Dictionary<string, CachedWebViewScript> hostToCachedWebViewScript = new();
    private static readonly Dictionary<string, CachedWebViewScript> urlToCachedWebViewScript = new();

    public static List<string> AdditionalWebViewScriptsFolders { get; set; } = new();

    public static bool CanHandleWebViewUrl(string url)
    {
        try
        {
            // Try to parse the URL to make sure it's valid.
            string host = new Uri(url).Host;
        }
        catch (Exception e)
        {
            return false;
        }

        if (!hasScannedJavaScriptFiles)
        {
            ScanJavaScriptFiles();
        }

        string webViewScript = GetWebViewScript(url);
        return !webViewScript.IsNullOrEmpty();
    }

    public static string GetWebViewScript(string url)
    {
        // Try get cached template for the specific URL.
        if (urlToCachedWebViewScript.TryGetValue(url, out CachedWebViewScript cachedWebViewScript))
        {
            return cachedWebViewScript.Content;
        }

        // Try get cached template for the host.
        string urlHost = new Uri(url).Host;
        if (!hostToCachedWebViewScript.TryGetValue(urlHost, out cachedWebViewScript))
        {
            // Load and remember the template for the host.
            cachedWebViewScript = LoadAndCacheWebViewScriptForHost(urlHost);
        }

        if (cachedWebViewScript == null)
        {
            return "";
        }

        // Remember the template for the specific URL.
        urlToCachedWebViewScript[url] = cachedWebViewScript;

        return cachedWebViewScript.Content;
    }

    public static void ClearCache()
    {
        hostToWebViewScript.Clear();
        hostToCachedWebViewScript.Clear();
        urlToCachedWebViewScript.Clear();
        hasScannedJavaScriptFiles = false;
    }

    private static CachedWebViewScript LoadAndCacheWebViewScriptForHost(string urlHost)
    {
        List<KeyValuePair<string, string>> matches = hostToWebViewScript
            .Where(entry => HostsMatch(entry.Key, urlHost))
            .ToList();
        if (matches.IsNullOrEmpty())
        {
            return null;
        }

        string filePath = matches.FirstOrDefault().Value;
        string fileContent = File.ReadAllText(filePath);

        Debug.Log($"Found WebView script for host '{urlHost}' in file '{filePath}'");

        CachedWebViewScript cachedWebViewScript = new(fileContent);
        hostToCachedWebViewScript[urlHost] = cachedWebViewScript;
        return cachedWebViewScript;
    }

    private static bool HostsMatch(string a, string b)
    {
        string aWithoutWww = a.Replace("www.", "");
        string bWithoutWww = b.Replace("www.", "");
        return aWithoutWww.ToLowerInvariant() == bWithoutWww.ToLowerInvariant();
    }

    private static void ScanJavaScriptFiles()
    {
        if (hasScannedJavaScriptFiles)
        {
            return;
        }
        hasScannedJavaScriptFiles = true;

        List<string> webViewScriptsFolders = GetWebViewScriptsFolders();

        foreach (string scriptsFolder in webViewScriptsFolders)
        {
            if (!DirectoryUtils.Exists(scriptsFolder))
            {
                continue;
            }
            Debug.Log($"Loading WebView scripts from folder '{scriptsFolder}'");
            string[] webViewScriptPaths = Directory.GetFiles(scriptsFolder, "*.js");
            Debug.Log($"Found {webViewScriptPaths.Length} WebView scripts in folder '{scriptsFolder}'");
            foreach (string webViewScriptPath in webViewScriptPaths)
            {
                string host = Path.GetFileNameWithoutExtension(webViewScriptPath);
                hostToWebViewScript[host] = webViewScriptPath;
            }
        }
    }

    public static List<string> GetWebViewScriptsFolders()
    {
        return new List<string>()
        {
            GetDefaultWebViewScriptsAbsolutePath(),
            GetUserDefinedWebViewScriptsAbsolutePath(),
        }
        .Union(AdditionalWebViewScriptsFolders)
        .ToList();
    }

    public static string GetDefaultWebViewScriptsAbsolutePath()
    {
        return ApplicationUtils.GetStreamingAssetsPath(WebViewScriptsFolderName);
    }

    public static string GetUserDefinedWebViewScriptsAbsolutePath()
    {
        return ApplicationUtils.GetPersistentDataPath(WebViewScriptsFolderName);
    }

    private class CachedWebViewScript
    {
        public string Content { get; private set; }

        public CachedWebViewScript(string content)
        {
            this.Content = content;
        }
    }
}

using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

public static class WebRequestUtils
{
    public static async Awaitable SendWebRequestAsync(UnityWebRequest unityWebRequest, CancellationToken cancellationToken = default)
    {
        void LogSuccess()
        {
            Log.Verbose(() => $"{unityWebRequest.method} '{unityWebRequest.uri}' has completed. Status: {unityWebRequest.result}, response code: {unityWebRequest.responseCode}");
        }

        void LogError(Exception ex)
        {
            Debug.LogError($"{unityWebRequest.method} '{unityWebRequest.uri}' has failed. Status: {unityWebRequest.result}, response code: {unityWebRequest.responseCode}, error message: {ex.Message}");
            Debug.LogException(ex);
        }

        try
        {
            await SendWebRequestWithCancellationAsync(unityWebRequest, cancellationToken);

            if (unityWebRequest.result is UnityWebRequest.Result.Success)
            {
                LogSuccess();
            }
            else
            {
                string errorMessage = unityWebRequest.error ?? "Unknown error";
                Exception ex = new($"{unityWebRequest.result}: {errorMessage}");
                LogError(ex);
                throw new UnityWebRequestException(unityWebRequest);
            }
        }
        catch (Exception ex)
        {
            LogError(ex);
            throw ex;
        }
    }

    private static async Awaitable SendWebRequestWithCancellationAsync(UnityWebRequest request, CancellationToken cancellationToken)
    {
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();

        // Poll the WebRequest at intervals to support cancellation
        while (!operation.isDone)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Debug.Log($"Cancellation requested, aborting request {request.method} '{request.uri}'");
                request.Abort();
                cancellationToken.ThrowIfCancellationRequested();
            }

            // Do not use CancellationToken for NextFrameAsync. Otherwise, above handling of the cancellation will be skipped.
            await Awaitable.NextFrameAsync();
        }
    }

    public static async Awaitable<string> GetWebRequestResponseAsync(UnityWebRequest unityWebRequest)
    {
        await SendWebRequestAsync(unityWebRequest);
        return unityWebRequest.downloadHandler?.text;
    }

    public static bool IsFileUri(string uri)
    {
        return !uri.IsNullOrEmpty()
               && uri.StartsWith("file://");
    }

    public static bool IsHttpOrHttpsUri(string uri)
    {
        return !uri.IsNullOrEmpty()
                && (uri.StartsWith("http://")
                    || uri.StartsWith("https://"));
    }

    public static bool IsNetworkPath(string absolutePath)
    {
        return !absolutePath.IsNullOrEmpty()
               && (absolutePath.StartsWith(@"\\")
                   || absolutePath.StartsWith("//"));
    }

    public static string AbsoluteFilePathToUri(string absolutePath)
    {
        if (absolutePath.StartsWith(@"\\"))
        {
            // This is a Windows-like network path.
            // MUST prefix it with the file:// scheme AND an additional slash for Unity API to work.
            // See https://forum.unity.com/threads/unitywebrequest-and-local-area-network.714353/
            return "file:///" + absolutePath.Replace("/", "\\");
        }

        if (absolutePath.StartsWith("//"))
        {
            // This also is a Unix-like network path. But because forward slashes are used, MUST prefix it with the file:// scheme ONLY for Unity API to work.
            return "file://" + absolutePath;
        }

        // This is a local path. MUST NOT prefix it with the file:// scheme.
        // Otherwise some paths may not work, e.g., when it contains a space AND a plus character.
        // See https://forum.unity.com/threads/unitywebrequest-file-protocol-not-working-with-plus-character-in-path-how-to-escape-the-uri.1364499/#post-8655012
        return absolutePath;
    }

    private class UnityWebRequestException : Exception
    {
        public UnityWebRequest UnityWebRequest { get; private set; }

        public UnityWebRequestException(UnityWebRequest unityWebRequest)
            : base($"UnityWebRequest failed: method '{unityWebRequest.method}', url '{unityWebRequest.url}', result '{unityWebRequest.result}', error '{unityWebRequest.error}'")
        {
            UnityWebRequest = unityWebRequest;
        }
    }
}

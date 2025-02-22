using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

public class FileDownloadControl
{
    private readonly UnityWebRequest webRequest;
    private readonly CancellationTokenSource cancellationTokenSource = new();

    public ulong DownloadedBytes => webRequest.downloadedBytes;

    public FileDownloadControl(string url, string targetPath)
    {
        webRequest = CreateDownloadRequest(url, targetPath);
    }

    public async Awaitable DownloadAsync()
    {
        await WebRequestUtils.SendWebRequestAsync(webRequest, cancellationTokenSource.Token);
    }

    public void Cancel()
    {
        cancellationTokenSource.Cancel();
    }

    public async Awaitable<ulong> FetchFileSizeAsync()
    {
        Uri uri = webRequest.uri;
        using UnityWebRequest request = UnityWebRequest.Head(uri);
        await WebRequestUtils.SendWebRequestAsync(request);

        if (request.result is UnityWebRequest.Result.Success)
        {
            string contentLength = request.GetResponseHeader("Content-Length");
            if (contentLength.IsNullOrEmpty())
            {
                return 0;
            }

            return Convert.ToUInt64(contentLength);
        }

        return 0;
    }

    private static UnityWebRequest CreateDownloadRequest(string url, string targetPath)
    {
        DownloadHandlerFile downloadHandler = new(targetPath);
        downloadHandler.removeFileOnAbort = true;

        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        webRequest.downloadHandler = downloadHandler;
        return webRequest;
    }
}

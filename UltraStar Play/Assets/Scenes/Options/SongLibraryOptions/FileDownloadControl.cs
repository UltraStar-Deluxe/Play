using System;
using System.Collections;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

public class FileDownloadControl : MonoBehaviour
{
    public UnityWebRequest WebRequest { get; private set; }

    private readonly Subject<DownloadProgressEvent> progressEventStream = new();
    public IObservable<DownloadProgressEvent> ProgressEventStream => progressEventStream;

    public ReactiveProperty<ulong> FinalDownloadSizeInBytes { get; private set; } = new();
    public bool HasFinalDownloadSize => FinalDownloadSizeInBytes.Value > 0;

    private readonly Subject<VoidEvent> beforeDestroyEventStream = new();
    public IObservable<VoidEvent> BeforeDestroyEventStream => beforeDestroyEventStream;

    public ReactiveProperty<bool> IsDone { get; private set; } = new();
    public ReactiveProperty<string> ErrorMessage { get; private set; } = new();
    public IObservable<bool> HasError => ErrorMessage.Select(errorMessage => !errorMessage.IsNullOrEmpty());
    public IObservable<bool> IsDoneOrHasError => IsDone.CombineLatest(HasError, (isDone, hasError) => isDone || hasError);
    public IObservable<bool> IsDoneWithoutError => IsDone.CombineLatest(HasError, (isDone, hasError) => isDone && !hasError);

    private bool isInitialized;

    public static FileDownloadControl Create(UnityWebRequest webRequest, Transform parent)
    {
        string name = $"{nameof(FileDownloadControl)} {webRequest.url}";
        GameObject gameObject = new GameObject(name);
        if (parent != null)
        {
            gameObject.transform.parent = parent;
        }

        FileDownloadControl fileDownloadControl = gameObject.AddComponent<FileDownloadControl>();
        fileDownloadControl.WebRequest = webRequest;
        fileDownloadControl.Init();
        return fileDownloadControl;
    }

    public void SendWebRequest()
    {
        WebRequest.SendWebRequest();
    }

    public void Cancel()
    {
        WebRequest.Abort();
        Destroy(gameObject);
    }

    private void Init()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;

        FetchFileSize();
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        UpdateReactiveProperties();

        if (IsDone.Value || !ErrorMessage.Value.IsNullOrEmpty())
        {
            Destroy(gameObject);
        }
    }

    private void FetchFileSize()
    {
        StartCoroutine(FileSizeUpdateCoroutine(WebRequest.uri));
    }

    private IEnumerator FileSizeUpdateCoroutine(Uri uri)
    {
        using UnityWebRequest request = UnityWebRequest.Head(uri);
        yield return request.SendWebRequest();

        if (request.result
            is UnityWebRequest.Result.ConnectionError
            or UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Error fetching size of {uri}: {request.error}");
            FinalDownloadSizeInBytes.Value = 0;
            ErrorMessage.Value = request.error;
        }
        else
        {
            string contentLength = request.GetResponseHeader("Content-Length");
            if (contentLength.IsNullOrEmpty())
            {
                FinalDownloadSizeInBytes.Value = 0;
            }
            else
            {
                FinalDownloadSizeInBytes.Value = Convert.ToUInt64(contentLength);
            }
        }
    }

    private void UpdateReactiveProperties()
    {
        ErrorMessage.Value = WebRequest.error;
        IsDone.Value = WebRequest.isDone;

        if (ErrorMessage.Value.IsNullOrEmpty())
        {
            progressEventStream.OnNext(new DownloadProgressEvent(WebRequest.downloadedBytes, FinalDownloadSizeInBytes.Value));
        }
    }

    private void OnDestroy()
    {
        beforeDestroyEventStream.OnNext(VoidEvent.instance);
        WebRequest?.Dispose();

        // If the request was not finished yet, then it is now aborted and thus not successful.
        if (!IsDone.Value)
        {
            ErrorMessage.Value = "Canceled";
        }
        IsDone.Value = true;
    }

    public class DownloadProgressEvent
    {
        public ulong DownloadedByteCount { get; private set; }
        public ulong FinalDownloadSizeInBytes { get; private set; }
        public double DownloadProgressInPercent { get; private set; }

        public DownloadProgressEvent(ulong downloadedByteCount, ulong finalDownloadSizeInBytes)
        {
            DownloadedByteCount = downloadedByteCount;
            FinalDownloadSizeInBytes = finalDownloadSizeInBytes;
            DownloadProgressInPercent = finalDownloadSizeInBytes > 0
                ? 100.0 * ((double)downloadedByteCount / finalDownloadSizeInBytes)
                : 0.0;
        }
    }

    public static UnityWebRequest CreateDownloadRequest(string url, string targetPath)
    {
        DownloadHandler downloadHandler = CreateDownloadHandler(targetPath);
        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        webRequest.downloadHandler = downloadHandler;
        return webRequest;
    }

    public static DownloadHandler CreateDownloadHandler(string targetPath)
    {
        DownloadHandlerFile downloadHandler = new(targetPath);
        downloadHandler.removeFileOnAbort = true;
        return downloadHandler;
    }
}

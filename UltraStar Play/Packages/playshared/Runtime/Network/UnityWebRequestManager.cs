using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

public class UnityWebRequestManager : AbstractSingletonBehaviour
{
    public static UnityWebRequestManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<UnityWebRequestManager>();
    
    private readonly List<RequestData> runningRequestDatas = new();

    protected override object GetInstance()
    {
        return Instance;
    }

    protected void Update()
    {
        runningRequestDatas.ToList().ForEach(request =>
        {
            if (request.unityWebRequest.isDone)
            {
                runningRequestDatas.Remove(request);
            }
            
            if (request.unityWebRequest.result
                is UnityWebRequest.Result.ConnectionError
                or UnityWebRequest.Result.ProtocolError
                or UnityWebRequest.Result.DataProcessingError)
            {
                request.onError?.Invoke(new UnityWebRequestException(request.unityWebRequest));
            }
            else if (request.unityWebRequest.result is UnityWebRequest.Result.Success)
            {
                request.onSuccess?.Invoke(request.unityWebRequest.downloadHandler?.text);
            }
        });
    }

    public void AddUnityWebRequest(
        UnityWebRequest unityWebRequest,
        Action<string> onSuccess,
        Action<Exception> onError)
    {
        runningRequestDatas.Add(new RequestData()
        {
            unityWebRequest = unityWebRequest,
            onSuccess = onSuccess,
            onError = onError,
        });
    }
    
    private class RequestData
    {
        public UnityWebRequest unityWebRequest;
        public Action<string> onSuccess;
        public Action<Exception> onError;
    }
}

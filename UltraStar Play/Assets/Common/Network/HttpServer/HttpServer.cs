using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using UnityEngine;

public class HttpServer : MonoBehaviour
{
    public static HttpServer Instance { get; private set; }
    
    private const string Scheme = "http";
    private const string Host = "localhost";
    private const int Port = 6789;
    private HttpListener httpServer;
    private bool hasBeenDestroyed;

    private readonly Dictionary<string, EndpointHandler> idToEndpointHandlerMap = new Dictionary<string, EndpointHandler>();
    private readonly List<EndpointHandler> sortedEndpointHandlers = new List<EndpointHandler>();

    private readonly ConcurrentQueue<HttpListenerContext> requestContextQueue = new ConcurrentQueue<HttpListenerContext>();
    
    public Action<EndpointRequestData> NoEndpointFoundCallback { get; set; } = DefaultNoEndpointFoundCallback;
    
    private void Awake()
    {
        if (Instance != null)
        {
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (!HttpListener.IsSupported)
        {
            Debug.Log("HttpListener not supported on this platform");
            return;
        }
        Debug.Log("Starting HttpListener");
        
        httpServer = new HttpListener();
        httpServer.Prefixes.Add($"{Scheme}://{Host}:{Port}/");
        httpServer.Start();

        ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            // Serve Http Requests, while this gameObject has not been destroyed.
            while (!hasBeenDestroyed)
            {
                AcceptRequest(httpServer);
            }
        });
    }

    public void Update()
    {
        // Process the requests in Update. Update is called from Unity's main thread which allows access to all Unity API.
        while (requestContextQueue.TryDequeue(out HttpListenerContext context))
        {
            HandleRequest(context);
        }
    }
    
    public void RegisterEndpoint(EndpointHandler endpointHandler)
    {
        if (!HttpListener.IsSupported)
        {
            return;
        }

        if (idToEndpointHandlerMap.ContainsKey(GetEndpointId(endpointHandler.HttpMethod, endpointHandler.Pattern)))
        {
            this.RemoveEndpoint(endpointHandler);
        }

        idToEndpointHandlerMap[endpointHandler.Pattern] = endpointHandler;
        sortedEndpointHandlers.Add(endpointHandler);
        sortedEndpointHandlers.Sort(EndpointHandler.CompareDescendingByPlaceholderCount);
    }

    public void RemoveEndpoint(HttpMethod httpMethod, string pattern)
    {
        string endpointId = GetEndpointId(httpMethod, pattern);
        if (idToEndpointHandlerMap.TryGetValue(endpointId, out EndpointHandler endpointHandler))
        {
            idToEndpointHandlerMap.Remove(endpointId);
            sortedEndpointHandlers.Remove(endpointHandler);
        }
    }

    private void AcceptRequest(HttpListener listener)
    {
        HttpListenerContext context;
        try
        {
            // Note: The GetContext method blocks while waiting for a request.
            context = listener.GetContext();
            // The Request is enqueued and processed on the main thread (i.e. in Update).
            // This enables access to all Unity API in the callback.
            requestContextQueue.Enqueue(context);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        try
        {
            if (TryHandleRequestByMatchingEndpointHandler(context))
            {
                return;
            }
            NoEndpointFoundCallback(new EndpointRequestData(context, null));
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            // Close the output stream.
            try
            {
                if (context != null)
                {
                    context.Response.Close();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Exception while trying to close the HttpListenerContext.Response.OutputStream");
                Debug.LogException(e);
            }
        }
    }

    private bool TryHandleRequestByMatchingEndpointHandler(HttpListenerContext context)
    {
        // The list is already sorted. Thus, the first matching handler is the best matching handler.
        foreach (EndpointHandler handler in sortedEndpointHandlers)
        {
            if (handler.TryHandle(context))
            {
                return true;
            }
        }
        return false;
    }

    private static string GetEndpointId(HttpMethod method, string pattern)
    {
        return method.Method + "|" + pattern;
    }

    private static void DefaultNoEndpointFoundCallback(EndpointRequestData requestData)
    {
        requestData.Context.Response.SendResponse("", HttpStatusCode.NotFound);
    }
    
    private void OnDestroy()
    {
        hasBeenDestroyed = true;
        httpServer?.Close();
    }
}

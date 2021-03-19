using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using UnityEngine;

public class HttpServer : MonoBehaviour
{
    public static HttpServer Instance { get; private set; }
    
    private const string Scheme = "http";
    private const string Host = "localhost";
    private const int Port = 6789;
    private HttpListener httpServer;
    private bool hasBeenDestroyed;

    private readonly Dictionary<string, RouteHandler> patternToRouteHandlerMap = new Dictionary<string, RouteHandler>();
    private readonly List<RouteHandler> sortedRouteHandlers = new List<RouteHandler>();

    private readonly ConcurrentQueue<HttpListenerContext> requestContextQueue = new ConcurrentQueue<HttpListenerContext>();
    
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

        AddRouteHandler(HttpMethod.Get, "/api/rest/hello/{name}",
            routeRequestData => "Hello " + routeRequestData.PlaceholderValues["name"]);
        
        httpServer = new HttpListener();
        httpServer.Prefixes.Add($"{Scheme}://{Host}:{Port}/");
        httpServer.Start();

        ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            // Serve Http Requests, while this gameObject has not been destroyed.
            while (!hasBeenDestroyed)
            {
                ServerHttpRequest(httpServer);
            }
        });
    }

    public void Update()
    {
        if (!requestContextQueue.TryDequeue(out HttpListenerContext context))
        {
            return;
        }
        
        try
        {
            // Find best matching route handler.
            // The list is already sorted. Thus, the first matching handler is the best matching handler.
            foreach (RouteHandler routeHandler in sortedRouteHandlers)
            {
                if (routeHandler.TryHandle(context.Request, out string responseText))
                {
                    WriteResponseText(context.Response.OutputStream, responseText);
                    return;
                }
            }
            
            string errorMessage = $"Found no matching RouteHandler for '{context.Request.HttpMethod}' with URL: {context.Request.Url}";
            WriteJsonErrorResponse(context.Response, errorMessage);
            Debug.LogWarning(errorMessage);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            // You must close the output stream.
            try
            {
                if (context != null)
                {
                    context.Response.Close();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Exception while trying to close the OutputStream");
                Debug.LogException(e);
            }
        }
    }

    public void AddRouteHandler(HttpMethod httpMethod, string pattern, Func<RouteRequestData, string> responseTextProvider)
    {
        if (!HttpListener.IsSupported)
        {
            return;
        }

        if (patternToRouteHandlerMap.ContainsKey(pattern))
        {
            RemoveRouteHandler(pattern);
        }

        RouteHandler routeHandler = new RouteHandler(httpMethod, pattern, responseTextProvider);
        patternToRouteHandlerMap[pattern] = routeHandler;
        sortedRouteHandlers.Add(routeHandler);
        sortedRouteHandlers.Sort(RouteHandler.CompareDescendingByPlaceholderCount);
    }

    private void RemoveRouteHandler(string pattern)
    {
        if (patternToRouteHandlerMap.TryGetValue(pattern, out RouteHandler routeHandler))
        {
            patternToRouteHandlerMap.Remove(pattern);
            sortedRouteHandlers.Remove(routeHandler);
        }
    }

    private void ServerHttpRequest(HttpListener listener)
    {
        HttpListenerContext context = null;
        try
        {
            // Note: The GetContext method blocks while waiting for a request.
            context = listener.GetContext();
            requestContextQueue.Enqueue(context);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void WriteResponseText(Stream responseOutputStream, string response)
    {
        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
        responseOutputStream.Write(responseBytes, 0,responseBytes.Length);
    }

    private void WriteJsonErrorResponse(HttpListenerResponse response, string errorMessage)
    {
        string responseJsonString = "{\"errorMessage\":\"" + errorMessage + "\"}";
        // Casting enum to int to get the StatusCode
        response.StatusCode = (int)HttpStatusCode.NotFound;
        WriteResponseText(response.OutputStream, responseJsonString);
    }

    private void OnDestroy()
    {
        hasBeenDestroyed = true;
        httpServer?.Close();
    }
}

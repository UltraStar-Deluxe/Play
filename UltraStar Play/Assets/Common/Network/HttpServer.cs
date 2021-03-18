using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class HttpServer : MonoBehaviour
{
    private static HttpServer instance;
    
    private const string Scheme = "http";
    private const string Host = "localhost";
    private const int Port = 6789;
    private HttpListener httpServer;
    private bool hasBeenDestroyed;
    private float time;

    private List<RouteHandler> sortedRouteHandlers;
    
    private void Start()
    {
        if (instance != null)
        {
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (!HttpListener.IsSupported)
        {
            Debug.Log("HttpListener not supported on this platform");
            return;
        }

        InitRoutes();
        
        Debug.Log("Starting HttpListener");
        httpServer = new HttpListener();
        httpServer.Prefixes.Add($"{Scheme}://{Host}:{Port}/");
        httpServer.Start();

        ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            // Serve Http Requests, while this gameObject has not been destroyed yet.
            while (!hasBeenDestroyed)
            {
                ServerHttpRequest(httpServer);
            }
        });
    }

    private void InitRoutes()
    {
        List<RouteHandler> unsortedRouteHandlers = new List<RouteHandler>();

        AddRouteHandler(unsortedRouteHandlers, "http://" + Host + ":" + Port + "/hello/{name}",
            (placeholderValues, request) => "Hello " + placeholderValues["name"]);
        
        AddRouteHandler(unsortedRouteHandlers, "http://" + Host + ":" + Port + "/hello/{name}/{surname}",
            (placeholderValues, request) => "Hello " + placeholderValues["surname"] + ", " + placeholderValues["name"]);
        
        // Sort descending by complexity, i.e., the number of placeholders in the pattern.
        sortedRouteHandlers = unsortedRouteHandlers;
        sortedRouteHandlers.Sort((a,b) => b.GetPlaceholderCount().CompareTo(a.GetPlaceholderCount()));
    }

    private void AddRouteHandler(List<RouteHandler> routeHandlers, string pattern, Func<Dictionary<string, string>, HttpListenerRequest, string> responseTextProvider)
    {
        routeHandlers.Add(new RouteHandler(pattern, responseTextProvider));
    }

    private void Update()
    {
        time = Time.time;
    }
    
    private void ServerHttpRequest(HttpListener listener)
    {
        HttpListenerContext context = null;
        try
        {
            // Note: The GetContext method blocks while waiting for a request.
            context = listener.GetContext();
            Debug.Log("URL: " + context.Request.Url);
            Debug.Log("QueryString: " + context.Request.QueryString);
            
            // Find best matching route handler.
            // The list is already sorted. Thus, the first matching handler is the best matching handler.
            foreach (RouteHandler routeHandler in sortedRouteHandlers)
            {
                if (routeHandler.TryHandle(context.Request, out string responseText))
                {
                    byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(responseText);
                    context.Response.ContentLength64 = responseBytes.Length;
                    context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                    return;
                }
                
                Debug.LogWarning("Found no matching RouteHandler for URL: " + context.Request.Url);
            }
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

    private void OnDestroy()
    {
        hasBeenDestroyed = true;
        httpServer?.Close();
    }

    private class RouteHandler
    {
        public string RoutePattern { get; private set; }
        public Func<Dictionary<string, string>, HttpListenerRequest, string> ResponseTextProvider { get; private set; }
        private CurlyBracePlaceholderMatcher patternMatcher;

        public RouteHandler(string routePattern, Func<Dictionary<string, string>, HttpListenerRequest, string> responseTextProvider)
        {
            RoutePattern = routePattern;
            patternMatcher = new CurlyBracePlaceholderMatcher(routePattern);
            ResponseTextProvider = responseTextProvider;
        }

        public int GetPlaceholderCount()
        {
            return patternMatcher.GetPlaceholderCount();
        }

        public bool TryHandle(HttpListenerRequest request, out string responseString)
        {
            if (patternMatcher.TryMatch(request.Url.ToString(), out Dictionary<string, string> placeholderValues))
            {
                responseString = ResponseTextProvider(placeholderValues, request);
                return true;
            }

            responseString = null;
            return false;
        }
    }
}

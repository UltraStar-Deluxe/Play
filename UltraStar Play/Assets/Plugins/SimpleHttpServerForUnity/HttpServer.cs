using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using UnityEngine;

namespace SimpleHttpServerForUnity
{
    public class HttpServer : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            Instance = null;
        }
        
        public static HttpServer Instance { get; private set; }

        public string scheme = "http";

        // Note: IP address of the current device is available via IpAddressUtils.GetIpAddress(AddressFamily.IPv4)
        public string host = "localhost";
        public int port = 6789;

        public Action<EndpointRequestData> NoEndpointFoundCallback { get; set; } = DefaultNoEndpointFoundCallback;
        
        private HttpListener httpListener;
        private bool hasBeenDestroyed;

        private readonly Dictionary<string, EndpointHandler> idToEndpointHandlerMap = new Dictionary<string, EndpointHandler>();
        private readonly List<EndpointHandler> sortedEndpointHandlers = new List<EndpointHandler>();

        private readonly ConcurrentQueue<HttpListenerContext> requestQueue = new ConcurrentQueue<HttpListenerContext>();

        private void Awake()
        {
            if (Instance != null)
            {
                // This instance is not needed.
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // Move object to top level in scene hierarchy.
            // Otherwise this object will be destroyed with its parent, even when DontDestroyOnLoad is used. 
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            if (!HttpListener.IsSupported)
            {
                Debug.Log("HttpListener not supported on this platform");
                return;
            }

            Debug.Log("Starting HttpListener");

            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"{scheme}://{host}:{port}/");
            httpListener.Start();

            ThreadPool.QueueUserWorkItem(poolHandle =>
            {
                // Serve Http Requests, while this gameObject has not been destroyed.
                while (!hasBeenDestroyed)
                {
                    AcceptRequest(httpListener);
                }
            });
        }

        public void Update()
        {
            // Process the requests in Update. Update is called from Unity's main thread which allows access to all Unity API.
            while (requestQueue.TryDequeue(out HttpListenerContext context))
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

            string endpointId = GetEndpointId(endpointHandler.HttpMethod, endpointHandler.UrlPattern);
            if (idToEndpointHandlerMap.ContainsKey(endpointId))
            {
                this.RemoveEndpoint(endpointHandler);
            }

            idToEndpointHandlerMap.Add(endpointId, endpointHandler);
            sortedEndpointHandlers.Add(endpointHandler);
            sortedEndpointHandlers.Sort(EndpointHandler.CompareDescendingByPlaceholderCount);
        }

        public void RemoveEndpoint(HttpMethod httpMethod, string urlPattern)
        {
            string endpointId = GetEndpointId(httpMethod, urlPattern);
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
                requestQueue.Enqueue(context);
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
            httpListener?.Close();
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using UnityEngine;

namespace SimpleHttpServerForUnity
{
    public class HttpServer : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitOnLoad()
        {
            instance = null;
        }

        private static HttpServer instance;
        public static HttpServer Instance {
            get
            {
                if (instance == null)
                {
                    HttpServer instanceInScene = FindObjectOfType<HttpServer>();
                    if (instanceInScene != null)
                    {
                        instanceInScene.InitSingleInstance();
                    }
                }
                return instance;
            }
        }

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

        protected virtual void Awake()
        {
            InitSingleInstance();
        }

        protected virtual void OnDestroy()
        {
            hasBeenDestroyed = true;
            httpListener?.Close();
        }
        
        protected virtual void Update()
        {
            // Process the requests in Update. Update is called from Unity's main thread which allows access to all Unity API.
            while (requestQueue.TryDequeue(out HttpListenerContext context))
            {
                HandleRequest(context);
            }
        }

        public void StartHttpListener()
        {
            if (httpListener != null && httpListener.IsListening)
            {
                Debug.LogWarning("HttpServer already listening");
                return;
            }
            
            if (httpListener == null)
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add($"{scheme}://{host}:{port}/");                
            }
            
            Debug.Log($"Starting HttpListener on {host}:{port}");
            httpListener.Start();

            ThreadPool.QueueUserWorkItem(poolHandle =>
            {
                while (!hasBeenDestroyed && httpListener != null && httpListener.IsListening)
                {
                    AcceptRequest(httpListener);
                }
            });
        }

        public void StopHttpListener()
        {
            if (httpListener == null || !httpListener.IsListening)
            {
                Debug.LogWarning("HttpServer already not listening");
                return;
            }
            
            Debug.Log($"Stopping HttpListener on {host}:{port}");
            httpListener?.Close();
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

        public List<Endpoint> GetRegisteredEndpoints()
        {
            return sortedEndpointHandlers
                .Select(it => new Endpoint(it.HttpMethod, it.UrlPattern, it.Description))
                .ToList();
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
                if (e is HttpListenerException hle
                    && hle.ErrorCode == 500
                    && hasBeenDestroyed)
                {
                    // Dont log error when closing the HttpListener has interrupted a blocking call.
                    return;
                }
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

        protected void InitSingleInstance()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            
            if (instance != null)
            {
                // This instance is not needed.
                Destroy(gameObject);
                return;
            }
            instance = this;
            
            // Move object to top level in scene hierarchy.
            // Otherwise this object will be destroyed with its parent, even when DontDestroyOnLoad is used.
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            if (!HttpListener.IsSupported)
            {
                Debug.Log("HttpListener not supported on this platform");
                return;
            }
        }

        private static string GetEndpointId(HttpMethod method, string pattern)
        {
            return method.Method + "|" + pattern;
        }

        private static void DefaultNoEndpointFoundCallback(EndpointRequestData requestData)
        {
            requestData.Context.Response.SendResponse("", HttpStatusCode.NotFound);
        }
    }
}

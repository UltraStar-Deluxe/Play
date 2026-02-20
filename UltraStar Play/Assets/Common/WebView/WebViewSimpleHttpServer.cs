using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

// TODO: Use UltraStarPlayHttpServer with multiple Prefixes (add localhost)
public class WebViewSimpleHttpServer
{
    private readonly Func<string> htmlProvider;
    private readonly HttpListener listener;
    
    private readonly string url;
    public string Url => url;
    
    private Thread thread;
    private bool running;

    public WebViewSimpleHttpServer(Func<string> htmlProvider)
    {
        this.htmlProvider = htmlProvider;
        this.url = $"http://localhost:{GetFreeTcpPort()}/";
        listener = new HttpListener();
        listener.Prefixes.Add(url);
    }

    public void Start()
    {
        running = true;
        listener.Start();
        thread = new Thread(Listen);
        thread.IsBackground = true;
        thread.Start();
        Log.WithClassContext().Information(() => "Server running at " + string.Join(", ", listener.Prefixes));
    }

    public void Stop()
    {
        running = false;
        listener.Stop();
    }

    private void Listen()
    {
        while (running)
        {
            try
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerResponse response = context.Response;
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(htmlProvider());

                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (HttpListenerException e)
            {
                if (running)
                {
                    Debug.LogException(e);
                }
                else
                {
                    // Log warning instead of error to prevent failing tests.
                    Debug.LogWarning("Server already stopped: " + e.Message);
                }
                break;
            }
        }
    }

    private static int GetFreeTcpPort()
    {
        TcpListener tcpListener = new TcpListener(IPAddress.Loopback, 0);
        try
        {
            tcpListener.Start();
            return (tcpListener.LocalEndpoint as IPEndPoint)?.Port ?? 8080;
        }
        finally
        {
            try
            {
                tcpListener.Stop();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Log.WithClassContext().Error(() => $"Failed to stop TcpListener after getting free port. tcpListener: {tcpListener}, error: {e.Message}");
            }
        }
    }
}

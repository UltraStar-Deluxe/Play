using System;
using System.Net;
using System.Threading;
using UnityEngine;

// TODO: Use UltraStarPlayHttpServer with multiple Prefixes (add localhost)
public class WebViewSimpleHttpServer
{
    private readonly Func<string> htmlProvider;
    private readonly HttpListener listener;
    private Thread thread;
    private bool running;

    public WebViewSimpleHttpServer(Func<string> htmlProvider, string url)
    {
        this.htmlProvider = htmlProvider;
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
                Debug.LogException(e);
                break;
            }
        }
    }
}

using UnityEngine;
using UnityEditor;
using SimpleHttpServerForUnity;

[CustomEditor(typeof(HttpServer), true)]
public class HttpServerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        HttpServer httpServer = target as HttpServer;

        if (GUILayout.Button("Start Server"))
        {
            httpServer.StartHttpListener();
        }
        
        if (GUILayout.Button("Stop Server"))
        {
            httpServer.StopHttpListener();
        }
    }
}

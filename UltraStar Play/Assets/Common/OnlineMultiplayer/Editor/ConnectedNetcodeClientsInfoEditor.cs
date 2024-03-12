using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    [CustomEditor(typeof(ConnectedNetcodeClientsInfo))]
    public class ConnectedNetcodeClientsInfoEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ConnectedNetcodeClientsInfo connectedNetcodeClientsInfo = (ConnectedNetcodeClientsInfo)target;

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null
                && networkManager.IsServer)
            {
                DrawConnectedNetcodeClients(networkManager.ConnectedClients.Values.ToList());
            }

            base.OnInspectorGUI();
        }

        private void DrawConnectedNetcodeClients(List<NetworkClient> networkClients)
        {
            EditorGUI.BeginDisabledGroup(disabled: true);

            EditorGUILayout.LabelField($"Connected client count: {networkClients.Count}");

            EditorGUI.indentLevel++;
            foreach (NetworkClient networkClient in networkClients)
            {
                DrawConnectedNetcodeClient(networkClient);
            }

            EditorGUI.indentLevel--;

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();
        }

        private void DrawConnectedNetcodeClient(NetworkClient networkClient)
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.wordWrap = true;

            string text = $"UnityNetcodeClientId: {networkClient.ClientId}";
            EditorGUILayout.LabelField(text, labelStyle);

            EditorGUILayout.Space();
        }
    }
}

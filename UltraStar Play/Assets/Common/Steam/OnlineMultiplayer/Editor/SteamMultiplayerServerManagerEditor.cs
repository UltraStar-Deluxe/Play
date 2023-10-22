using System.Collections.Generic;
using Network.Framework;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace Network.Editor
{
    [CustomEditor(typeof(SteamMultiplayerServerManager))]
    public class SteamMultiplayerServerManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (NetworkManager.Singleton == null)
            {
                base.OnInspectorGUI();
                return;
            }

            var data = (SteamMultiplayerServerManager)target;

            EditorGUI.BeginDisabledGroup(disabled: true);

            EditorGUILayout.LabelField($"Current Members [{data.MemberLookup.Count}]");

            EditorGUI.indentLevel++;

            foreach (KeyValuePair<FixedString64Bytes, SteamMultiplayerNetworkExtensions.MemberData> member in data.MemberLookup)
                DrawMember(member);

            EditorGUI.indentLevel--;

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }

        private void DrawMember(KeyValuePair<FixedString64Bytes, SteamMultiplayerNetworkExtensions.MemberData> member)
        {
            var labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.wordWrap = true;
            var data = member.Value;

            EditorGUILayout.LabelField($"Key/Id: {member.Key} | {data.ClientId}", labelStyle);
            EditorGUILayout.LabelField($"Display Name: {data.DisplayName}", labelStyle);

            EditorGUILayout.Space();
        }
    }
}

using System.Collections.Generic;
using SteamOnlineMultiplayer;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SteamMultiplayerManager))]
public class SteamMultiplayerServerManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (NetworkManager.Singleton == null)
        {
            base.OnInspectorGUI();
            return;
        }

        SteamMultiplayerManager steamMultiplayerManager = (SteamMultiplayerManager)target;
        IReadOnlyList<MemberData> members = steamMultiplayerManager.GetMembers();

        EditorGUI.BeginDisabledGroup(disabled: true);

        EditorGUILayout.LabelField($"Current Members [{members.Count}]");

        EditorGUI.indentLevel++;
        foreach (MemberData member in members)
        {
            DrawMember(member);
        }
        EditorGUI.indentLevel--;

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.Space();

        base.OnInspectorGUI();
    }

    private void DrawMember(MemberData data)
    {
        GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.wordWrap = true;

        string text = $"Display Name: {data.DisplayName}, " +
                      $"SteamId: {data.SteamId}, " +
                      $"UnityNetcodeClientId: {data.UnityNetcodeClientId}, " +
                      $"Connection Guid: {data.ConnectionGuid} ";
        EditorGUILayout.LabelField(text, labelStyle);

        EditorGUILayout.Space();
    }
}

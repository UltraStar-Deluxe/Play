using System.Collections.Generic;
using SteamOnlineMultiplayer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SteamMultiplayerManager))]
public class SteamMultiplayerServerManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SteamMultiplayerManager steamMultiplayerManager = (SteamMultiplayerManager)target;
        DrawConnectedLobbyMembers(steamMultiplayerManager.GetMembers());

        base.OnInspectorGUI();
    }

    private void DrawConnectedLobbyMembers(IReadOnlyList<MemberData> members)
    {
        EditorGUI.BeginDisabledGroup(disabled: true);

        EditorGUILayout.LabelField($"Connected lobby member count: {members.Count}");

        EditorGUI.indentLevel++;
        foreach (MemberData member in members)
        {
            DrawConnectedLobbyMember(member);
        }
        EditorGUI.indentLevel--;

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.Space();

    }

    private void DrawConnectedLobbyMember(MemberData data)
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

using System.Collections.Generic;
using SteamOnlineMultiplayer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SteamLobbyMemberManager))]
public class SteamMultiplayerServerManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SteamLobbyMemberManager steamLobbyMemberManager = (SteamLobbyMemberManager)target;
        DrawConnectedSteamLobbyMembers(steamLobbyMemberManager.GetSteamLobbyMembers());

        base.OnInspectorGUI();
    }

    private void DrawConnectedSteamLobbyMembers(IReadOnlyList<SteamLobbyMember> members)
    {
        EditorGUI.BeginDisabledGroup(disabled: true);

        EditorGUILayout.LabelField($"Connected Steam lobby member count: {members.Count}");

        EditorGUI.indentLevel++;
        foreach (SteamLobbyMember member in members)
        {
            DrawConnectedSteamLobbyMember(member);
        }
        EditorGUI.indentLevel--;

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.Space();

    }

    private void DrawConnectedSteamLobbyMember(SteamLobbyMember data)
    {
        GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.wordWrap = true;

        string text = $"Display Name: {data.DisplayName}, " +
                      $"UnityNetcodeClientId: {data.UnityNetcodeClientId}, " +
                      $"SteamId: {data.SteamId}, ";
        EditorGUILayout.LabelField(text, labelStyle);

        EditorGUILayout.Space();
    }
}

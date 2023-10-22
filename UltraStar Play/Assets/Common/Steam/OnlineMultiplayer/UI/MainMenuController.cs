// using System;
// using Network.Framework;
// using System.Collections.Generic;
// using System.Linq;
// using Unity.Netcode;
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using System.Threading.Tasks;
// using Steamworks.Data;
//
// namespace Multiplayer.Runtime.UI
// {
//     [AddComponentMenu("Network/UI/UI Main Menu"), DisallowMultipleComponent]
//     public class MainMenuController : MonoBehaviour
//     {
//         [Header("Private Game References")]
//         [SerializeField] private Toggle createPrivate;
//         [SerializeField] private TMP_Dropdown visibilityDropdown;
//         [SerializeField] private Toggle joinableToggle;
//        [SerializeField] private TMP_InputField nameField;
//         [SerializeField] private TMP_InputField maxMembersField;
//         [SerializeField] private Button createServerBtn;
//
//         [Header("Public Game References")]
//         [SerializeField] private Button findOnlineGame;
//
//         [Header("Join Private Game References")]
//         [SerializeField] private TMP_InputField joinServerField;
//         [SerializeField] private Button joinPrivateGame;
//
//     //    [SerializeField] private Button m_RefreshLobbies;
//     //    [SerializeField] private Transform m_LobbyRect;
//     //    [SerializeField] private Button m_LobbyPrefab;
//     //    [SerializeField] private Button m_CreateDedicatedServerBtn;
//
//         private void Start()
//         {
//             var list = new List<string>
//             {
//                 nameof(SteamExtensions.LobbyVisibility.Public),
//                 nameof(SteamExtensions.LobbyVisibility.Private),
//                 nameof(SteamExtensions.LobbyVisibility.FriendsOnly),
//                 nameof(SteamExtensions.LobbyVisibility.Invisible)
//             };
//
//             if (visibilityDropdown != null)
//                 visibilityDropdown.AddOptions(list);
//
//             createServerBtn.onClick.AddListener(CreateServer);
//             joinServerField.onEndEdit.AddListener(JoinServer);
//             createPrivate.onValueChanged.AddListener(UpdateUI);
//             findOnlineGame.onClick.AddListener(FindOnlineLobby);
//
//             UpdateUI(false);
//           //  m_RefreshLobbies.onClick.AddListener(async() => await RefreshLobbies());
//          //   m_CreateDedicatedServerBtn.onClick.AddListener(CreateDedicatedServer);
//         }
//
//         private async void FindOnlineLobby()
//         {
//             print("Retrieving lobbies");
//             var onlineLobbies = await GetLobbies();
//             print($"Finding available lobby: {onlineLobbies.Length}");
//             var joinableLobbies = onlineLobbies.Where(a => a.MaxMembers > a.MemberCount) as Lobby[] ?? Array.Empty<Lobby>();
//             //find available lobby
//             if (joinableLobbies.Any())
//             {
//                 var lobby = joinableLobbies.First();
//                 print($"Lobby found! Joining {lobby.Id}");
//                 SteamMultiplayerClientManager.Instance.StartSteamClient(lobby.Owner.Id);
//             }
//             else //no existing lobbies, create our own public lobby
//             {
//                 print("no lobbies found, creating new lobby instead");
//                 await SteamMultiplayerNetworkManager.Instance.StartSteamHost(new SteamExtensions.LobbyConfig
//                 {
//                     name = $"Lobby {Guid.NewGuid()}",
//                     joinable = true,
//                     maxMembers = 30,
//                     visibility = SteamExtensions.LobbyVisibility.Public
//                 });
//             }
//         }
//
//         private void OnDestroy()
//         {
//             createServerBtn.onClick.RemoveAllListeners();
//             joinServerField.onEndEdit.RemoveAllListeners();
//             createPrivate.onValueChanged.RemoveAllListeners();
//             findOnlineGame.onClick.RemoveAllListeners();
//             //  m_RefreshLobbies.onClick.RemoveAllListeners();
//             //   m_CreateDedicatedServerBtn.onClick.RemoveAllListeners();
//         }
//
//         private void UpdateUI(bool createPrivateLobby)
//         {
//             //update required buttons
//             visibilityDropdown.gameObject.SetActive(createPrivateLobby);
//             joinableToggle.gameObject.SetActive(createPrivateLobby);
//             nameField.gameObject.SetActive(createPrivateLobby);
//             maxMembersField.gameObject.SetActive(createPrivateLobby);
//             createServerBtn.gameObject.SetActive(createPrivateLobby);
//
//             joinPrivateGame.gameObject.SetActive(!createPrivateLobby);
//             joinServerField.gameObject.SetActive(!createPrivateLobby);
//             findOnlineGame.gameObject.SetActive(!createPrivateLobby);
//         }
//
//         private async Task<Lobby[]> GetLobbies()
//         {
//             return await SteamExtensions.GetLobbiesAsync() ?? Array.Empty<Lobby>();
//
//             //refresh lobby list
//             /*for (int i = 0; i < m_LobbyRect.childCount; i++)
//                 Destroy(m_LobbyRect.GetChild(i).gameObject);*/
//
//             //create lobby list in UI
//             /*foreach (var lobby in lobbies)
//             {
//                 var obj = Instantiate(m_LobbyPrefab, m_LobbyRect);
//                 var tmpText = obj.GetComponentInChildren<TMP_Text>();
//
//                 tmpText.SetText($"Lobby [{lobby.MemberCount:00}]: {lobby.Owner}");
//                 obj.onClick.AddListener(() => NW_ClientManager.Instance.StartSteamClient(lobby.Owner.Id));
//             }*/
//         }
//
//         private void JoinServer(string input)
//         {
//             input = input.Trim();
//
//             if (string.IsNullOrEmpty(input))
//                 return;
//
//             var id = SteamExtensions.GetLobbyIdFromCode(input);
//             SteamMultiplayerClientManager.Instance.StartSteamClient(id);
//         }
//
//         private async void CreateServer()
//         {
//             if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer)
//                 return;
//
//             byte maxMembers = 4;
//
//             if (maxMembersField != null)
//             {
//                 if (!byte.TryParse(maxMembersField.text, out maxMembers))
//                     maxMembers = 1; // Need a least one member!
//             }
//
//             var lobbyName = $"Lobby {Guid.NewGuid()}";
//
//             if (nameField != null)
//                 lobbyName = nameField.text;
//
//             var joinable = true;
//
//             if (joinableToggle != null)
//                 joinable = joinableToggle.isOn;
//
//             var visibility = SteamExtensions.LobbyVisibility.Public;
//
//             if (visibilityDropdown != null)
//                 visibility = (SteamExtensions.LobbyVisibility)visibilityDropdown.value;
//
//             if (createServerBtn != null)
//                 createServerBtn.gameObject.SetActive(false);
//
//             await SteamMultiplayerNetworkManager.Instance.StartSteamHost(new SteamExtensions.LobbyConfig
//             {
//                 name = lobbyName,
//                 joinable = joinable,
//                 maxMembers = maxMembers,
//                 visibility = visibility,
//             });
//         }
//
//         /*
//         [Obsolete]
//         private async void CreateDedicatedServer()
//         {
//             if (NetworkManager.Singleton.IsServer)
//                 return;
//
//             byte maxMembers = 4;
//
//             if (m_MaxMembersField != null)
//             {
//                 if (!byte.TryParse(m_MaxMembersField.text, out maxMembers))
//                     maxMembers = 1; // Need a least one member!
//             }
//
//             string lobbyName = $"Lobby {System.Guid.NewGuid()}";
//
//             if (m_NameField != null)
//                 lobbyName = m_NameField.text;
//
//             bool joinable = true;
//
//             if (m_JoinableToggle != null)
//                 joinable = m_JoinableToggle.isOn;
//
//             var visibility = NW_SteamExtensions.LobbyVisibility.Public;
//
//             if (visibilityDropdown != null)
//                 visibility = (NW_SteamExtensions.LobbyVisibility)visibilityDropdown.value;
//
//             if (m_CreateServerBtn != null)
//                 m_CreateServerBtn.gameObject.SetActive(false);
//
//             await NW_NetworkManager.Instance.StartSteamServer(new NW_SteamExtensions.LobbyConfig
//             {
//                 name = lobbyName,
//                 joinable = joinable,
//                 maxMembers = maxMembers,
//                 visibility = visibility,
//             });
//         }*/
//     }
// }

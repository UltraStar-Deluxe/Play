using System;
using System.Collections.Generic;
using Steamworks.Data;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

namespace SteamOnlineMultiplayer
{
    public class OnlineMultiplayerConnectionUiControl : INeedInjection, IInjectionFinishedListener
    {
        [Inject(UxmlName = R.UxmlNames.hostGameButton)]
        private Button hostGameButton;

        [Inject(UxmlName = R.UxmlNames.hostGameVisibilityChooser)]
        private DropdownField hostGameVisibilityChooser;

        [Inject(UxmlName = R.UxmlNames.hostLobbyPasswordField)]
        private TextField hostLobbyPasswordField;

        [Inject(UxmlName = R.UxmlNames.joinLobbyPasswordField)]
        private TextField joinLobbyPasswordField;

        [Inject(UxmlName = R.UxmlNames.lobbyList)]
        private VisualElement lobbyList;

        [Inject(UxmlName = R.UxmlNames.searchLobbiesButton)]
        private Button searchLobbiesButton;

        [Inject]
        private SteamMultiplayerManager steamMultiplayerManager;

        [Inject]
        private SteamLobbyManager steamLobbyManager;

        [Inject]
        private SteamManager steamManager;

        private DropdownFieldControl<ELobbyVisibility> hostGameVisibilityChooserControl;

        private ELobbyVisibility HostSteamLobbyVisibility => hostGameVisibilityChooserControl.SelectedItem;

        private string JoinLobbyPassword => joinLobbyPasswordField.value.Trim();
        private string HostLobbyPassword => hostLobbyPasswordField.value.Trim();

        private enum ELobbyVisibility
        {
            Hidden,
            Public,
        }

        public void OnInjectionFinished()
        {
            hostGameButton.RegisterCallbackButtonTriggered(_ => HostGame());

            hostGameVisibilityChooserControl = new(
                hostGameVisibilityChooser,
                new List<ELobbyVisibility>()
                {
                    ELobbyVisibility.Hidden,
                    ELobbyVisibility.Public,
                },
                ELobbyVisibility.Public,
                item => StringUtils.ToTitleCase(item.ToString()));
            hostGameVisibilityChooserControl.Selection
                .Subscribe(newValue =>
                {
                    hostLobbyPasswordField.SetVisibleByDisplay(newValue is ELobbyVisibility.Hidden);
                    hostLobbyPasswordField.GetFirstAncestorOfType<AccordionItem>()?.UpdateTargetHeight();
                });

            searchLobbiesButton.RegisterCallbackButtonTriggered(_ => UpdateLobbyList());

            // Update lobby list when connected to Steam
            if (steamManager.IsConnectedToSteam)
            {
                UpdateLobbyList();
            }
            else
            {
                steamManager.ConnectedToSteamEventStream
                    .SubscribeOneShot(evt => UpdateLobbyList());
            }
        }

        private void JoinGame(Lobby lobby)
        {
            try
            {
                steamMultiplayerManager.JoinLobby(lobby);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                UiManager.CreateNotification($"Failed to join lobby: {ex.Message}");
            }
        }

        private void HostGame()
        {
            ELobbyVisibility lobbyVisibility = HostSteamLobbyVisibility;

            ESteamLobbyVisibility steamLobbyVisibility = lobbyVisibility is ELobbyVisibility.Hidden
                ? ESteamLobbyVisibility.Private
                : ESteamLobbyVisibility.Public;

            string lobbyPassword = lobbyVisibility is ELobbyVisibility.Hidden
                ? HostLobbyPassword
                : "";

            ObservableUtils.RunOnNewTaskAsObservable(async () =>
                    {
                        if (lobbyVisibility is ELobbyVisibility.Hidden
                            && lobbyPassword.IsNullOrEmpty())
                        {
                            throw new OnlineMultiplayerException("Hosting a hidden lobby requires a password");
                        }

                        await steamMultiplayerManager.CreateLobbyAsync(
                            new LobbyConfig()
                            {
                                name = $"Lobby {Guid.NewGuid()}",
                                joinable = true,
                                maxMembers = SteamConstants.MaxLobbyMembers,
                                visibility = steamLobbyVisibility,
                                password = lobbyPassword,
                            });

                        return true;
                    },
                    Disposable.Empty)
                .ObserveOnMainThread()
                .Select(_ =>
                {
                    Debug.Log("Successfully created new Steam lobby.");
                    steamMultiplayerManager.StartNetcodeNetworkManagerHost();
                    return true;
                })
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogException(ex);
                    UiManager.CreateNotification(ex.Message);
                })
                .Subscribe(_ =>
                {
                    Debug.Log("Successfully started host");
                    UiManager.CreateNotification("Hosting new online game");
                });
        }

        private void UpdateLobbyList()
        {
            lobbyList.Clear();

            string lobbyPassword = HostSteamLobbyVisibility is ELobbyVisibility.Hidden
                ? JoinLobbyPassword
                : "";

            ObservableUtils.RunOnNewTaskAsObservable(async () =>
                {
                    Lobby[] lobbies = await steamLobbyManager.GetLobbiesAsync(lobbyPassword);
                    return lobbies;
                },
                Disposable.Empty)
                .ObserveOnMainThread()
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogException(ex);
                    UiManager.CreateNotification($"Failed to update lobby list: {ex.Message}");
                    lobbyList.Add(new Label("Failed to fetch lobbies"));
                })
                .Subscribe(lobbies => FillLobbyList(lobbies));
        }

        private void FillLobbyList(Lobby[] lobbies)
        {
            if (lobbies.IsNullOrEmpty())
            {
                lobbyList.Add(new Label("No lobbies found."));
                if (JoinLobbyPassword.IsNullOrEmpty())
                {
                    lobbyList.Add(new Label("Try enter a lobby password to search hidden lobbies."));
                }
                else
                {
                    lobbyList.Add(new Label("Try a different lobby password to search hidden lobbies."));
                }
            }
            else
            {
                foreach (Lobby lobby in lobbies)
                {
                    Button joinLobbyButton = new Button();
                    joinLobbyButton.text = $"Join lobby {lobby.Id}";
                    joinLobbyButton.RegisterCallbackButtonTriggered(_ => JoinGame(lobby));
                    lobbyList.Add(joinLobbyButton);
                }
            }

            lobbyList.GetFirstAncestorOfType<AccordionItem>()?.UpdateTargetHeight();

            ThemeManager.ApplyThemeSpecificStylesToVisualElements(lobbyList);
        }
    }
}

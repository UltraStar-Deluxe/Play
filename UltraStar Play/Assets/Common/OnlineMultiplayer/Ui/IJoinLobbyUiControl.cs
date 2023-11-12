using System;
using UnityEngine.UIElements;

namespace CommonOnlineMultiplayer
{
    public interface IJoinLobbyUiControl : IDisposable
    {
        VisualElement CreateVisualElement();
    }
}

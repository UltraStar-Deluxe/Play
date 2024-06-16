using System;
using UnityEngine.UIElements;

namespace CommonOnlineMultiplayer
{
    public interface IHostLobbyUiControl : IDisposable
    {
        VisualElement CreateVisualElement();
    }
}

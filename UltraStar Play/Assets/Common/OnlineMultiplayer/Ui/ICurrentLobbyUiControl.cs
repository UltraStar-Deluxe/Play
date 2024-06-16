using System;
using UnityEngine.UIElements;

namespace CommonOnlineMultiplayer
{
    public interface ICurrentLobbyUiControl : IDisposable
    {
        VisualElement CreateVisualElement();
    }
}

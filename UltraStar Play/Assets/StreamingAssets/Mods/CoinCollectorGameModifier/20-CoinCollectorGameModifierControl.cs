using UnityEngine;
using UniInject;
using UnityEngine.UIElements;
using UniRx;
using System;
using System.Collections.Generic;

public class CoinCollectorGameModifierControl : GameRoundModifierControl
{
    public string modFolder;

    [Inject]
    private SingSceneControl singSceneControl;
    
    [Inject]
    private UIDocument uiDocument;

    private List<CoinCollectorGameModifierPlayerControl> coinCollectorPlayerControls = new List<CoinCollectorGameModifierPlayerControl>();

    private void Start()
    {
        AddStyleSheet();

        foreach (PlayerControl playerControl in singSceneControl.PlayerControls)
        {
            CreateCoinCollectorControl(playerControl);
        }
    }

    private void AddStyleSheet()
    {
        string styleSheetPath = $"{modFolder}/stylesheets/CoinCollectorStyles.uss";
        StyleSheet styleSheet = StyleSheetUtils.CreateStyleSheetFromFile(styleSheetPath);
        uiDocument.rootVisualElement.styleSheets.Add(styleSheet);
    }

    private void Update()
    {
        coinCollectorPlayerControls.ForEach(it => it.Update());
    }

    private void CreateCoinCollectorControl(PlayerControl playerControl)
    {
        CoinCollectorGameModifierPlayerControl coinCollectorGameModifierPlayerControl = new CoinCollectorGameModifierPlayerControl();
        coinCollectorGameModifierPlayerControl.modFolder = modFolder;
        playerControl.PlayerUiControl.Injector.Inject(coinCollectorGameModifierPlayerControl);
        coinCollectorPlayerControls.Add(coinCollectorGameModifierPlayerControl);
    }
}

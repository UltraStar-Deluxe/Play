using UniInject;
using UnityEngine.UIElements;

public class CoinCollectorGameModifier : IGameRoundMod
{
    [Inject]
    private ModObjectContext modObjectContext;

    public string DisplayName => "Coin Collector";
    public double DisplayOrder => 100;

    public VisualElement CreateConfigurationVisualElement()
    {
        return null;
    }

    public GameRoundModifierControl CreateControl()
    {
        CoinCollectorGameModifierControl control = GameObjectUtils.CreateGameObjectWithComponent<CoinCollectorGameModifierControl>();
        control.modFolder = modObjectContext.ModFolder;
        return control;
    }
}

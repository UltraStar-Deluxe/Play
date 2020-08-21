using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UniRx.Triggers;
using UnityEngine.EventSystems;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CharacterQuickJump : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongSelectSceneController songSelectSceneController;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Text uiText;

    public string character;

    void Start()
    {
        uiText.text = character.ToUpper();

        uiText.OnPointerClickAsObservable()
            .Subscribe(_ => character.IfNotNull(it => songSelectSceneController.DoCharacterQuickJump(it.ToLower()[0])));
        this.ObserveEveryValueChanged(me => me.character).WhereNotNull()
            .Subscribe(newCharacter => uiText.text = newCharacter.ToUpper());
    }
}

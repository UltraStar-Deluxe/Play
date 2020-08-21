using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CharacterQuickJumpBar : MonoBehaviour, INeedInjection
{
    // Quick jump letters that are also found on an Android contacts list.
    // Q, X, Y and Z are skipped (probably because nearly never used).
    private static readonly string characters = "&ABCDEFGHIJKLMNOPRSTUVW#";

    public CharacterQuickJump characterQuickJumpPrefab;

    [Inject]
    private Injector injector;

    void Start()
    {
        UpdateCharacters();
    }

    private void UpdateCharacters()
    {
        GetComponentsInChildren<CharacterQuickJump>().ForEach(it => Destroy(it.gameObject));

        foreach (char c in characters)
        {
            CreateCharacter(c);
        }
    }

    private void CreateCharacter(char character)
    {
        CharacterQuickJump characterQuickJump = Instantiate(characterQuickJumpPrefab, transform);
        characterQuickJump.character = character.ToString();
        injector.Inject(characterQuickJump);
    }
}

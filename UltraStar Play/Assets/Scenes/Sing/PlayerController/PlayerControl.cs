﻿using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniInject.Extensions;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    public PlayerNoteRecorder PlayerNoteRecorder { get; private set; }

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    public PlayerPitchTracker PlayerPitchTracker { get; private set; }

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    public MicSampleRecorder MicSampleRecorder { get; private set; }

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    public PlayerScoreController PlayerScoreController { get; private set; }

    [Inject]
    public PlayerProfile PlayerProfile { get; private set; }

    [Inject(Optional = true)]
    public MicProfile MicProfile { get; private set; }

    [Inject]
    private Voice voice;
    public Voice Voice
    {
        get
        {
            return voice;
        }
        private set
        {
            voice = value;
            SortedSentences = voice.Sentences.ToList();
            SortedSentences.Sort(Sentence.comparerByStartBeat);
        }
    }

    private readonly Subject<EnterSentenceEvent> enterSentenceEventStream = new Subject<EnterSentenceEvent>();
    public IObservable<EnterSentenceEvent> EnterSentenceEventStream => enterSentenceEventStream;

    // The sorted sentences of the Voice
    public List<Sentence> SortedSentences { get; private set; } = new List<Sentence>();

    [Inject]
    private Injector injector;

    // An injector with additional bindings, such as the PlayerProfile and the MicProfile.
    private Injector childrenInjector;

    private PlayerUiControl playerUiControl;

    [Inject]
    private SongMeta songMeta;

    private int displaySentenceIndex;

    public void OnInjectionFinished()
    {
        this.playerUiControl = new PlayerUiControl();
        this.childrenInjector = CreateChildrenInjectorWithAdditionalBindings();

        // Inject all
        // childrenInjector.Inject(playerUiControl);
        foreach (INeedInjection childThatNeedsInjection in gameObject.GetComponentsInChildren<INeedInjection>(true))
        {
            if (childThatNeedsInjection is not PlayerControl)
            {
                childrenInjector.Inject(childThatNeedsInjection);
            }
        }
        SetDisplaySentenceIndex(0);
    }

    private Injector CreateChildrenInjectorWithAdditionalBindings()
    {
        Injector newInjector = UniInjectUtils.CreateInjector(injector);
        newInjector.AddBindingForInstance(MicSampleRecorder);
        newInjector.AddBindingForInstance(PlayerPitchTracker);
        newInjector.AddBindingForInstance(PlayerNoteRecorder);
        newInjector.AddBindingForInstance(PlayerScoreController);
        newInjector.AddBindingForInstance(playerUiControl);
        newInjector.AddBindingForInstance(newInjector);
        newInjector.AddBindingForInstance(this);
        return newInjector;
    }

    public void SetCurrentBeat(double currentBeat)
    {
        // Change the current display sentence, when the current beat is over its last note.
        if (displaySentenceIndex < SortedSentences.Count && currentBeat >= GetDisplaySentence().LinebreakBeat)
        {
            Sentence nextDisplaySentence = GetUpcomingSentenceForBeat(currentBeat);
            int nextDisplaySentenceIndex = SortedSentences.IndexOf(nextDisplaySentence);
            if (nextDisplaySentenceIndex >= 0)
            {
                SetDisplaySentenceIndex(nextDisplaySentenceIndex);
            }
        }
    }

    private void SetDisplaySentenceIndex(int newValue)
    {
        displaySentenceIndex = newValue;

        Sentence displaySentence = GetSentence(displaySentenceIndex);

        // Update the UI
        enterSentenceEventStream.OnNext(new EnterSentenceEvent(displaySentence, displaySentenceIndex));
    }

    public Sentence GetSentence(int index)
    {
        Sentence sentence = (index >= 0 && index < SortedSentences.Count) ? SortedSentences[index] : null;
        return sentence;
    }

    public Note GetNextSingableNote(double currentBeat)
    {
        Note nextSingableNote = SortedSentences
            .SelectMany(sentence => sentence.Notes)
            // Freestyle notes are not displayed and not sung.
            // They do not contribute to the score.
            .Where(note => !note.IsFreestyle)
            .Where(note => currentBeat <= note.StartBeat)
            .OrderBy(note => note.StartBeat)
            .FirstOrDefault();
        return nextSingableNote;
    }

    public Sentence GetUpcomingSentenceForBeat(double currentBeat)
    {
        Sentence result = Voice.Sentences
            .Where(sentence => currentBeat < sentence.LinebreakBeat)
            .FirstOrDefault();
        return result;
    }

    public Sentence GetDisplaySentence()
    {
        return GetSentence(displaySentenceIndex);
    }

    public Note GetLastNoteInSong()
    {
        return SortedSentences.Last().Notes.OrderBy(note => note.EndBeat).Last();
    }

    public class EnterSentenceEvent
    {
        public Sentence Sentence { get; private set; }
        public int SentenceIndex { get; private set; }

        public EnterSentenceEvent(Sentence sentence, int sentenceIndex)
        {
            Sentence = sentence;
            SentenceIndex = sentenceIndex;
        }
    }
}
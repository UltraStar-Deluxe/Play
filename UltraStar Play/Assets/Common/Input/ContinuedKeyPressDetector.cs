using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

public class ContinuedKeyPressDetector : MonoBehaviour
{
    public KeyCode keyCode;

    public float initialDelayInSeconds = 0.75f;
    public float repeatedDelayInSeconds = 0.25f;

    private Subject<ContinuedKeyPressEvent> continuedKeyPressEventStream = new Subject<ContinuedKeyPressEvent>();
    public IObservable<ContinuedKeyPressEvent> ContinuedKeyPressEventStream => continuedKeyPressEventStream;

    private float keyPressedDurationInSeconds;
    private bool isFirstEvent;

    void Update()
    {
        if (Input.GetKey(keyCode))
        {
            if (Input.GetKeyDown(keyCode))
            {
                keyPressedDurationInSeconds = 0;
                isFirstEvent = true;
            }
            else
            {
                keyPressedDurationInSeconds += Time.deltaTime;
            }

            float thresholdDurationInSeconds = isFirstEvent ? initialDelayInSeconds : repeatedDelayInSeconds;
            if (keyPressedDurationInSeconds >= thresholdDurationInSeconds)
            {
                continuedKeyPressEventStream.OnNext(new ContinuedKeyPressEvent(keyCode, keyPressedDurationInSeconds));
                keyPressedDurationInSeconds = 0;
                isFirstEvent = false;
            }
        }
    }

    public class ContinuedKeyPressEvent
    {
        public KeyCode KeyCode { get; private set; }
        public float DurationInSeconds { get; private set; }

        public ContinuedKeyPressEvent(KeyCode keyCode, float durationInSeconds)
        {
            KeyCode = keyCode;
            DurationInSeconds = durationInSeconds;
        }
    }
}

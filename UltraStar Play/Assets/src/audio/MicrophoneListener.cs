using UnityEngine;
using System.Collections;
using UnityEngine.Audio; // required for dealing with audiomixers

[RequireComponent(typeof(AudioSource))]
public class MicrophoneListener : MonoBehaviour
{

    //Written in part by Benjamin Outram

    //option to toggle the microphone listenter on startup or not
    public bool startMicOnStartup = true;

    //allows start and stop of listener at run time within the unity editor
    public bool stopMicrophoneListener = false;
    public bool startMicrophoneListener = false;

    private bool microphoneListenerOn = false;

    //public to allow temporary listening over the speakers if you want of the mic output
    //but internally it toggles the output sound to the speakers of the audiosource depending
    //on if the microphone listener is on or off
    public bool disableOutputSound = false; 
 
    //an audio source also attached to the same object as this script is
    AudioSource src;

    //make an audio mixer from the "create" menu, then drag it into the public field on this script.
    //double click the audio mixer and next to the "groups" section, click the "+" icon to add a 
    //child to the master group, rename it to "microphone".  Then in the audio source, in the "output" option, 
    //select this child of the master you have just created.
    //go back to the audiomixer inspector window, and click the "microphone" you just created, then in the 
    //inspector window, right click "Volume" and select "Expose Volume (of Microphone)" to script,
    //then back in the audiomixer window, in the corner click "Exposed Parameters", click on the "MyExposedParameter"
    //and rename it to "Volume"
    public AudioMixer masterMixer;


    float timeSinceRestart = 0;






    void Start()
    {
        //start the microphone listener
        if (startMicOnStartup)
        {
            RestartMicrophoneListener();
            StartMicrophoneListener();
        }
    }

    void Update()
    {

        //can use these variables that appear in the inspector, or can call the public functions directly from other scripts
        if (stopMicrophoneListener)
        {
            StopMicrophoneListener();
        }
        if (startMicrophoneListener)
        {
            StartMicrophoneListener();
        }
        //reset paramters to false because only want to execute once
        stopMicrophoneListener = false;
        startMicrophoneListener = false;

        //must run in update otherwise it doesnt seem to work
        MicrophoneIntoAudioSource(microphoneListenerOn);

        //can choose to unmute sound from inspector if desired
        DisableSound(!disableOutputSound);


    }


    //stops everything and returns audioclip to null
    public void StopMicrophoneListener()
    {
        //stop the microphone listener
        microphoneListenerOn = false;
        //reenable the master sound in mixer
        disableOutputSound = false;
        //remove mic from audiosource clip
        src.Stop();
        src.clip = null;

        Microphone.End(null);
    }


    public void StartMicrophoneListener()
    {
        //start the microphone listener
        microphoneListenerOn = true;
        //disable sound output (dont want to hear mic input on the output!)
        disableOutputSound = true;
        //reset the audiosource
        RestartMicrophoneListener();
    }


    //controls whether the volume is on or off, use "off" for mic input (dont want to hear your own voice input!) 
    //and "on" for music input
    public void DisableSound(bool SoundOn)
    {

        float volume = 0;

        if (SoundOn)
        {
            volume = 0.0f;
        }
        else
        {
            volume = -80.0f;
        }

        if(masterMixer != null)
        {
            masterMixer.SetFloat("MasterVolume", volume);
        }
    }



    // restart microphone removes the clip from the audiosource
    public void RestartMicrophoneListener()
    {

        src = GetComponent<AudioSource>();

        //remove any soundfile in the audiosource
        src.clip = null;

        timeSinceRestart = Time.time;

    }

    //puts the mic into the audiosource
    void MicrophoneIntoAudioSource(bool MicrophoneListenerOn)
    {

        if (MicrophoneListenerOn)
        {
            //pause a little before setting clip to avoid lag and bugginess
            if (Time.time - timeSinceRestart > 0.5f && !Microphone.IsRecording(null))
            {
                src.clip = Microphone.Start(null, true, 10, 44100);

                //wait until microphone position is found (?)
                while (!(Microphone.GetPosition(null) > 0))
                {
                    // todo
                }

                src.Play(); // Play the audio source
            }
        }
    }

}
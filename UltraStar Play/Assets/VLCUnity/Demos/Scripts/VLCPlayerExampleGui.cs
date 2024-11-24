using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using LibVLCSharp;


///This script controls all the GUI for the VLC Unity Canvas Example
///It sets up event handlers and updates the GUI every frame
///This example shows how to safely set up LibVLC events and a simple way to call Unity functions from them
/// On Android, make sure you require Internet access in your manifest to be able to access internet-hosted videos in these demo scenes.
public class VLCPlayerExampleGui : MonoBehaviour
{
	public VLCPlayerExample vlcPlayer;
	
	//GUI Elements
	public RawImage screen;
	public AspectRatioFitter screenAspectRatioFitter;
	public Slider seekBar;
	public Button playButton;
	public Button pauseButton;
	public Button stopButton;
	public Button pathButton;
	public Button tracksButton;
	public Button volumeButton;
	public GameObject pathGroup; //Group containing pathInputField and openButton
	public InputField pathInputField;
	public Button openButton;
	public GameObject tracksButtonsGroup; //Group containing buttons to switch video, audio, and subtitle tracks
	public Slider volumeBar;
	public GameObject trackButtonPrefab;
	public GameObject trackLabelPrefab;
	public Color unselectedButtonColor; //Used for unselected track text
	public Color selectedButtonColor; //Used for selected track text

	//Configurable Options
	public int maxVolume = 100; //The highest volume the slider can reach. 100 is usually good but you can go higher.

	//State variables
	bool _isPlaying = false; //We use VLC events to track whether we are playing, rather than relying on IsPlaying 
	bool _isDraggingSeekBar = false; //We advance the seek bar every frame, unless the user is dragging it

	///Unity wants to do everything on the main thread, but VLC events use their own thread.
	///These variables can be set to true in a VLC event handler indicate that a function should be called next Update.
	///This is not actually thread safe and should be gone soon!
	bool _shouldUpdateTracks = false; //Set this to true and the Tracks menu will regenerate next frame
	bool _shouldClearTracks = false; //Set this to true and the Tracks menu will clear next frame

	List<Button> _videoTracksButtons = new List<Button>();
	List<Button> _audioTracksButtons = new List<Button>();
	List<Button> _textTracksButtons = new List<Button>();


	void Start()
	{
		//VLC Event Handlers
		vlcPlayer.mediaPlayer.Playing += (object sender, EventArgs e) => {
			//Always use Try/Catch for VLC Events
			try
			{
				//Because many Unity functions can only be used on the main thread, they will fail in VLC event handlers
				//A simple way around this is to set flag variables which cause functions to be called on the next Update
				_isPlaying = true;//Switch to the Pause button next update
				_shouldUpdateTracks = true;//Regenerate tracks next update


			}
			catch (Exception ex)
			{
				Debug.LogError("Exception caught in mediaPlayer.Play: \n" + ex.ToString());
			}
		};

		vlcPlayer.mediaPlayer.Paused += (object sender, EventArgs e) => {
			//Always use Try/Catch for VLC Events
			try
			{
				_isPlaying = false;//Switch to the Play button next update
			}
			catch (Exception ex)
			{
				Debug.LogError("Exception caught in mediaPlayer.Paused: \n" + ex.ToString());
			}
		};

		vlcPlayer.mediaPlayer.Stopped += (object sender, EventArgs e) => {
			//Always use Try/Catch for VLC Events
			try
			{
				_isPlaying = false;//Switch to the Play button next update
				_shouldClearTracks = true;//Clear tracks next update
			}
			catch (Exception ex)
			{
				Debug.LogError("Exception caught in mediaPlayer.Stopped: \n" + ex.ToString());
			}
		};

		//Buttons
		pauseButton.onClick.AddListener(() => { vlcPlayer.Pause(); });
		playButton.onClick.AddListener(() => { vlcPlayer.Play(); });
		stopButton.onClick.AddListener(() => { vlcPlayer.Stop(); });
		pathButton.onClick.AddListener(() => { 
			if(ToggleElement(pathGroup))
				pathInputField.Select();
		});
		tracksButton.onClick.AddListener(() => { ToggleElement(tracksButtonsGroup); SetupTrackButtons(); });
		volumeButton.onClick.AddListener(() => { ToggleElement(volumeBar.gameObject); });
		openButton.onClick.AddListener(() => { vlcPlayer.Open(pathInputField.text); });

		UpdatePlayPauseButton(vlcPlayer.playOnAwake);

		//Seek Bar Events
		var seekBarEvents = seekBar.GetComponent<EventTrigger>();

		EventTrigger.Entry seekBarPointerDown = new EventTrigger.Entry();
		seekBarPointerDown.eventID = EventTriggerType.PointerDown;
		seekBarPointerDown.callback.AddListener((data) => { _isDraggingSeekBar = true; });
		seekBarEvents.triggers.Add(seekBarPointerDown);

		EventTrigger.Entry seekBarPointerUp = new EventTrigger.Entry();
		seekBarPointerUp.eventID = EventTriggerType.PointerUp;
		seekBarPointerUp.callback.AddListener((data) => { 
			_isDraggingSeekBar = false;
			vlcPlayer.SetTime((long)((double)vlcPlayer.Duration * seekBar.value));
		});
		seekBarEvents.triggers.Add(seekBarPointerUp);

		//Path Input
		pathInputField.text = vlcPlayer.path;
		pathGroup.SetActive(false);

		//Track Selection Buttons
		tracksButtonsGroup.SetActive(false);

		//Volume Bar
		volumeBar.wholeNumbers = true;
		volumeBar.maxValue = maxVolume; //You can go higher than 100 but you risk audio clipping
		volumeBar.value = vlcPlayer.Volume;
		volumeBar.onValueChanged.AddListener((data) => { vlcPlayer.SetVolume((int)volumeBar.value);	});
		volumeBar.gameObject.SetActive(false);

	}

	void Update()
	{
		//Update screen aspect ratio. Doing this every frame is probably more than is necessary.

		if(vlcPlayer.texture != null)
			screenAspectRatioFitter.aspectRatio = (float)vlcPlayer.texture.width / (float)vlcPlayer.texture.height;

		UpdatePlayPauseButton(_isPlaying);

		UpdateSeekBar();

		if (_shouldUpdateTracks)
		{
			SetupTrackButtons();
			_shouldUpdateTracks = false;
		}

		if (_shouldClearTracks)
		{
			ClearTrackButtons();
			_shouldClearTracks = false;
		}

	}

	//Show the Pause button if we are playing, or the Play button if we are paused or stopped
	void UpdatePlayPauseButton(bool playing)
	{
		pauseButton.gameObject.SetActive(playing);
		playButton.gameObject.SetActive(!playing);
	}

	//Update the position of the Seek slider to the match the VLC Player
	void UpdateSeekBar()
	{
		if(!_isDraggingSeekBar)
		{
			var duration = vlcPlayer.Duration;
			if (duration > 0)
				seekBar.value = (float)((double)vlcPlayer.Time / duration);
		}
	}

	//Enable a GameObject if it is disabled, or disable it if it is enabled
	bool ToggleElement(GameObject element)
	{
		bool toggled = !element.activeInHierarchy;
		element.SetActive(toggled);
		return toggled;
	}

	//Create Audio, Video, and Subtitles button groups
	void SetupTrackButtons()
	{
		Debug.Log("SetupTrackButtons");
		ClearTrackButtons();
		SetupTrackButtonsGroup(TrackType.Video, "Video Tracks", _videoTracksButtons);
		SetupTrackButtonsGroup(TrackType.Audio, "Audio Tracks", _audioTracksButtons);
		SetupTrackButtonsGroup(TrackType.Text, "Subtitle Tracks", _textTracksButtons, true);

	}

	//Clear the track buttons menu
	void ClearTrackButtons()
	{
		var childCount = tracksButtonsGroup.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Destroy(tracksButtonsGroup.transform.GetChild(i).gameObject);
		}
	}

	//Create Audio, Video, or Subtitle button groups
	void SetupTrackButtonsGroup(TrackType type, string label, List<Button> buttonList, bool includeNone = false)
	{
		buttonList.Clear();
		var tracks = vlcPlayer.Tracks(type);
		var selected = vlcPlayer.SelectedTrack(type);

		if (tracks.Count > 0)
		{
			var newLabel = Instantiate(trackLabelPrefab, tracksButtonsGroup.transform);
			newLabel.GetComponentInChildren<Text>().text = label;

			for (int i = 0; i < tracks.Count; i++)
			{
				var track = tracks[i];
				var newButton = Instantiate(trackButtonPrefab, tracksButtonsGroup.transform).GetComponent<Button>();
				var textMeshPro = newButton.GetComponentInChildren<Text>();
				textMeshPro.text = track.Name;
				if (selected != null && track.Id == selected.Id)
					textMeshPro.color = selectedButtonColor;
				else
					textMeshPro.color = unselectedButtonColor;

				buttonList.Add(newButton);
				newButton.onClick.AddListener(() => {
					foreach (var button in buttonList)
						button.GetComponentInChildren<Text>().color = unselectedButtonColor;
					textMeshPro.color = selectedButtonColor;
					vlcPlayer.Select(track);
				});
			}
			if (includeNone)
			{
				var newButton = Instantiate(trackButtonPrefab, tracksButtonsGroup.transform).GetComponent<Button>();
				var textMeshPro = newButton.GetComponentInChildren<Text>();
				textMeshPro.text = "None";
				if (selected == null)
					textMeshPro.color = selectedButtonColor;
				else
					textMeshPro.color = unselectedButtonColor;

				buttonList.Add(newButton); 
				newButton.onClick.AddListener(() => {
					foreach (var button in buttonList)
						button.GetComponentInChildren<Text>().color = unselectedButtonColor;
					textMeshPro.color = selectedButtonColor;
					vlcPlayer.Unselect(type);
				});
			}

		}

	}
}
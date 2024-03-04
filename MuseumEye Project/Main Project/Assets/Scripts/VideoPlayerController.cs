using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class VideoPlayerController : MonoBehaviour {

	public MoviePlayer moviePlayer;

	public Button playButton;
	public Button pauseButton;
	public Button StopButton;
	public Slider seeker;

	public bool bStartedPlaying = false;

	void Start ()
	{
		
		playButton.gameObject.SetActive (true);
		pauseButton.gameObject.SetActive (false);

		seeker.value = 0;
	}


	void Update ()
	{
		if (bStartedPlaying == false)
		{
			bStartedPlaying = true;
			PlayClicked ();
		}

		if (moviePlayer.play) {

			seeker.value = moviePlayer.videoTime ;
		}

	}

	public void PlayClicked () {
		
		// Update UI
		seeker.minValue = 0;
		seeker.maxValue = moviePlayer.lengthSeconds;
		playButton.gameObject.SetActive (false);
		pauseButton.gameObject.SetActive (true);

		//Control Movie Player
		moviePlayer.play = true;
		//Allows reloading of audio
		moviePlayer.Reload ();

	}

	public void PauseClicked () {

		playButton.gameObject.SetActive (true);
		pauseButton.gameObject.SetActive (false);

		moviePlayer.play = false;

	}

	public void StopClicked()
	{
		playButton.gameObject.SetActive (true);
		pauseButton.gameObject.SetActive (false);

		moviePlayer.play = false;
		moviePlayer.videoTime = 0;
		seeker.value = 0;
	}

	public void seekerValueChanged () {
		moviePlayer.videoTime	= seeker.value;

	}
}

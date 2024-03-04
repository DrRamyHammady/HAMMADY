using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class PlayableToScene : MonoBehaviour {

	public PlayableDirector playableDirector;
	// Use this for initialization



	public void OnPlayableDestroy()
	{

		print("playable distroyed");

	}
	

}

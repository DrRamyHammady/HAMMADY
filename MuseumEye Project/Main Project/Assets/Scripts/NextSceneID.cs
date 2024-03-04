using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Place this script on a blank gameobject in your first scene & every scene in your app.
public class NextSceneID : MonoBehaviour
{
	//Public static reference to this object. Accessible from anywhere with NextSceneLoader.
	public static NextSceneID NextSceneIDRef;

	public string SceneName;

	// Use this for initialization
	void Start ()
	{
		if(NextSceneID.NextSceneIDRef == null)
		{
			NextSceneID.NextSceneIDRef = this;
		}
		else
		{
			//There is already one NextSceneLoader, destroy this copy.
			Destroy(this.gameObject);
		}

		//Keeps this in memory throughout all scene loads / unloads:
		DontDestroyOnLoad(this);
	}
	
	// Update is called once per frame
	

	public void SetNextSceneToLoad(string SceneNameToLoad)
	{
		SceneName = SceneNameToLoad;
	}
}

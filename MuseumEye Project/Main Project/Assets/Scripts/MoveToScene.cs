using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MoveToScene : MonoBehaviour {

	public GameObject loadingScreenObj;
	public Slider slider;
	public GameObject LoadingCanvas;
    //public int SceneNumber;
    public string SceneName;
    public static bool visited;

    AsyncOperation async;

	void OnTriggerEnter (Collider other)
	{
		Debug.Log ("Camera Entered Scene");
		LoadingCanvas.SetActive(true);
		StartCoroutine (LoadingScreen());
        visited = true;
	}


	IEnumerator LoadingScreen()
	{
		loadingScreenObj.SetActive (true);
        //async = SceneManager.LoadSceneAsync (SceneNumber, LoadSceneMode.Single);
        NextSceneID.NextSceneIDRef.SetNextSceneToLoad(SceneName);

        async = SceneManager.LoadSceneAsync("SceneLoader", LoadSceneMode.Single);
        async.allowSceneActivation = false;

		while (async.isDone == false) {
			slider.value = async.progress;
			if (async.progress == 0.9f) 
			{
				slider.value = 1f;
				async.allowSceneActivation = true;
			}
			yield return null;
		}
	}
    void Start()
    {
       if(visited == true)
        {
            Destroy(gameObject);
        }

    }
}

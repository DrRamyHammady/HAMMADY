using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;



public class StationMoveScene : MonoBehaviour {
	//public int SceneIndex;
    public string SceneName;
	public float Timetowait = 370f;

	public GameObject loadingCanvas;
	public Slider slider;

    public List<GameObject> ObjectsToDestroy = new List<GameObject>();

	AsyncOperation async;

	// Update is called once per frame
	void Start(){
		
			// execute block of code here
			StartCoroutine (LoadingScreen());
			print ("Load Scene");

	}

	IEnumerator LoadingScreen()
	{

		yield return new WaitForSeconds (Timetowait);

        foreach(GameObject GO in ObjectsToDestroy)
        {
            if(GO)
            {
                Destroy(GO);
            }
        }

        loadingCanvas.SetActive(true);

        NextSceneID.NextSceneIDRef.SetNextSceneToLoad(SceneName);

        async = SceneManager.LoadSceneAsync("SceneLoader", LoadSceneMode.Single);

        //async = SceneManager.LoadSceneAsync(SceneIndex, LoadSceneMode.Single);
        async.allowSceneActivation = false;

        while (async.isDone == false)
        {
            slider.value = async.progress;
            if (async.progress == 0.9f)
            {
                slider.value = 1f;
                async.allowSceneActivation = true;
            }
            yield return null;
        }
    }


}



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//This class goes in the 'empty' scene.
public class LoadNextScene : MonoBehaviour
{
    public GameObject loadingCanvas;
    public Slider slider;

    AsyncOperation async;

    // Use this for initialization
    void Start ()
	{
		Debug.Log("Entered SceneLoader");
        loadingCanvas.SetActive(true);
        StartCoroutine(LoadingScreen());
	}

    IEnumerator LoadingScreen()
    {
        float timer = 0;
        bool bStartedLoad = false;

        if (NextSceneID.NextSceneIDRef)
        {
            if (NextSceneID.NextSceneIDRef.SceneName != "")
            {
                Debug.Log("Loading new scene " + NextSceneID.NextSceneIDRef.SceneName + " asynchronously");
                async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(NextSceneID.NextSceneIDRef.SceneName);
                bStartedLoad = true;
            }
            else
            {
                Debug.LogWarning("No Scene name set in NextSceneID class!");
                async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(0);
                bStartedLoad = true;
            }
        }
        else
        {
            Debug.LogWarning("No NextSceneID object found in the static reference!");
            async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(0);
            bStartedLoad = true;
        }

        if (bStartedLoad == true)
        {
            async.allowSceneActivation = false;

            //while (async.isDone == false)
            //{
            //    slider.value = async.progress;
            //    if (async.progress == 0.9f)
            //    {
            //        slider.value = 1f;
            //        async.allowSceneActivation = true;
            //    }
            //    yield return null;
            //}
            while (async.isDone == false || slider.value != 1)
            {
                timer += Time.deltaTime / 2;
                // slider.value = async.progress;
                if (async.progress == 0.9f && slider.value == 1)
                {
                    //slider.value = 1f;
                    async.allowSceneActivation = true;
                }
                slider.value = Mathf.MoveTowards(0, 1, timer);
                yield return null;
            }
        }

        yield return null;
    }

        // Update is called once per frame
        void Update ()
	{
		
	}
}

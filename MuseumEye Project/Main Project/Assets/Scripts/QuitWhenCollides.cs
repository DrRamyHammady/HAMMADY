using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class QuitWhenCollides : MonoBehaviour {


  
    public GameObject Tut;
    public GameObject ExitButton;
    public AudioSource farewell;
    public GameObject GO;

    void OnTriggerEnter (Collider other)
	{
        
        Debug.Log ("Scene Going to Quit");
        ExecuteAfterTime(10.0f);
        Tut.SetActive(true);
        farewell.Play();
        GO = GameObject.Find("/MixedRealityCameraParent/MixedRealityCamera/AudioOutput");
        GO.SetActive(false);
        Destroy(gameObject);
        ExitButton.SetActive(true);
    }



    IEnumerator ExecuteAfterTime(float time)
    {
        yield return new WaitForSeconds(time);

#if UNITY_EDITOR

        UnityEditor.EditorApplication.isPlaying = false;

#else

    		Application.Quit ();

#endif
    }
}

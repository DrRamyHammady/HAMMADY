using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


/// <summary>
/// The Interactible class flags a Game Object as being "Interactible".
/// Determines what happens when an Interactible is being gazed at.
/// </summary>
public class GoToMuseumRoom : MonoBehaviour
{
    [Tooltip("Audio clip to play when interacting with this hologram.")]
    public AudioClip TargetFeedbackSound;
 
    private AudioSource audioSource;

    public string SceneName;

    public GameObject loadingCanvas;

    public Slider slider;

    AsyncOperation async;

    private Material[] defaultMaterials;

    void Start()
    {
        defaultMaterials = GetComponent<Renderer>().materials;

        // Add a BoxCollider if the interactible does not contain one.
        Collider collider = GetComponentInChildren<Collider>();
        if (collider == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }

        EnableAudioHapticFeedback();

       
    }

    private void EnableAudioHapticFeedback()
    {
        // If this hologram has an audio clip, add an AudioSource with this clip.
        if (TargetFeedbackSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.clip = TargetFeedbackSound;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1;
            audioSource.dopplerLevel = 0;
        }
    }

    void GazeEntered()
    {
        for (int i = 0; i < defaultMaterials.Length; i++)
        {
            defaultMaterials[i].SetFloat("_Highlight", .25f);
        }


    }

    void GazeExited()
    {
        for (int i = 0; i < defaultMaterials.Length; i++)
        {
            defaultMaterials[i].SetFloat("_Highlight", 0f);
        }

    }

    void OnSelect()
    {
        for (int i = 0; i < defaultMaterials.Length; i++)
        {
            defaultMaterials[i].SetFloat("_Highlight", .5f);
        }

        // Play the audioSource feedback when we gaze and select a hologram.
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }

        StartCoroutine(LoadingScreen());

    }

    IEnumerator LoadingScreen()
    {
        float timer=0;
        yield return null;
        loadingCanvas.SetActive(true);

        NextSceneID.NextSceneIDRef.SetNextSceneToLoad(SceneName);

        async = SceneManager.LoadSceneAsync("SceneLoader", LoadSceneMode.Single);

        async.allowSceneActivation = false;

        while (async.isDone == false || slider.value != 1)
        {
            timer += Time.deltaTime / 2;
           // slider.value = async.progress;
            if (async.progress == 0.9f && slider.value == 1 )
            {
                //slider.value = 1f;
                async.allowSceneActivation = true;
            }
            slider.value = Mathf.MoveTowards(0,1, timer);
            yield return null;
        }
    }
}
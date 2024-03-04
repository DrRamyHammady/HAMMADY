using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WhenSliderCompleted : MonoBehaviour {

    public Slider slider;
    public GameObject Completed;
    public AudioSource Wining;
    public AudioClip Clip;
	// Use this for initialization
	void Update () {
        if (slider.value == slider.maxValue )
        {
            Completed.SetActive(true);
            Wining.PlayOneShot(Clip);
        }
    }
	
	
}

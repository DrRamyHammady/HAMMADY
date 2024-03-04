using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HoloToolkit.Unity
{ 
public class customize : MonoBehaviour {

    // Use this for initialization
    void Start()
    {
        var soundManager = GameObject.Find("Audio Manager");
        TextToSpeechManager textToSpeech = soundManager.GetComponent<TextToSpeechManager> ();
        textToSpeech.Voice = TextToSpeechVoice.Mark;
        textToSpeech.SpeakText("Hello Ramy, How are you man");
    }

    // Update is called once per frame
    void Update()
    {

    }

}
}

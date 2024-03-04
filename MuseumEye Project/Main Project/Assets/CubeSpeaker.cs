using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using UnityEngine;

[RequireComponent(typeof(TextToSpeechManager))]
public class CubeSpeaker : MonoBehaviour, IInputClickHandler
{
    private TextToSpeechManager textToSpeechManager;
    private int count = 0;

    public void OnInputClicked(InputClickedEventData eventData)
    {
        this.textToSpeechManager.SpeakText(string.Format("You have clicked on me {0} times.", ++count));
    }

    // Use this for initialization
    void Start ()
    {
        this.textToSpeechManager = this.gameObject.GetComponent<TextToSpeechManager>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

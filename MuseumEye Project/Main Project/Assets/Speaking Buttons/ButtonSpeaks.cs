using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using UnityEngine;

[RequireComponent(typeof(TextToSpeechManager))]
public class ButtonSpeaks : MonoBehaviour, IInputClickHandler
{
    private TextToSpeechManager textToSpeechManager;
   

    public void OnInputClicked(InputClickedEventData eventData)
    {
        this.textToSpeechManager.SpeakText(string.Format("Hello Ramy. How are you man"));
    }

    // Use this for initialization
    void Start()
    {
        this.textToSpeechManager = this.gameObject.GetComponent<TextToSpeechManager>();
    }

    // Update is called once per frame
    void Update()
    {

    }
}

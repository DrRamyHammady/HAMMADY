using Academy.HoloToolkit.Unity;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// InteractibleAction performs custom actions when you gaze at the holograms.
/// </summary>
public class InteractibleTimeline : MonoBehaviour
{
    [Tooltip("Drag the Tagalong prefab asset you want to display.")]
   // public GameObject ObjectToTagAlong;

    public PlayableDirector Timeline;

    void RunTimeline()
    {
        Timeline.Play();
   }
}
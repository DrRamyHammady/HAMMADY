using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class UpdateTimeline : MonoBehaviour {

    public PlayableDirector Timeline;

    // Update is called once per frame
    void Update () {
        Timeline.Play();
    }
}

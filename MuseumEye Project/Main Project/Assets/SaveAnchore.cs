using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;


namespace HoloToolkit.Unity.SpatialMapping
{ 
public class SaveAnchore : MonoBehaviour {

    [Tooltip("Supply a friendly name for the anchor as the key name for the WorldAnchorStore.")]
    public string SavedAnchorFriendlyName = "SavedAnchorFriendlyName";

    protected WorldAnchorManager anchorManager;

    // Use this for initialization
    void Start () {

            CloseInteraction();

    }

    public void CloseInteraction()
    {
        anchorManager.AttachAnchor(gameObject, SavedAnchorFriendlyName);

    }


}
}

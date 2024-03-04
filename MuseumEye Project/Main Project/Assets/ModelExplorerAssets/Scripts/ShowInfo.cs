using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Academy.HoloToolkit.Unity;

public class ShowInfo : MonoBehaviour {

    public GameObject InfoText;

    public static GameObject LastInfoText;

    private GameObject revealedObj;
    void Update()
    {
        
        if (GazeManager.Instance.HitInfo.collider.tag == "Pointers")
        {
            revealedObj = GazeManager.Instance.HitInfo.collider.GetComponent<ShowInfo>().InfoText;
            revealedObj.SetActive(true);
            
        }
        else
        {
            InfoText.SetActive(false);
        }
    }



}

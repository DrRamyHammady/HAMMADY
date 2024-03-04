using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Academy.HoloToolkit.Unity;
using UnityEngine.UI;

public class ListOfPointers : MonoBehaviour {

    public GameObject[] ListOfPoints;
    public AudioSource CollideSound;

    public Slider slider;
    string PointerName;
    List<string> PointName = new List<string>();

    void Start()
    {
        slider.value = 0;
        slider.maxValue = ListOfPoints.Length;
    }

    void Update ()
    {

        if(GazeManager.Instance.HitInfo.collider.tag == "Pointers")
        {
            PointerName = GazeManager.Instance.HitInfo.collider.name;
        IncreaseAndCheckProgress();
        }
    }

    void IncreaseAndCheckProgress()
    {
        if (!PointName.Contains(PointerName))
        {
         PointName.Add(GazeManager.Instance.HitInfo.collider.name);
            slider.value++;
            CollideSound.Play();
        }
    }

}

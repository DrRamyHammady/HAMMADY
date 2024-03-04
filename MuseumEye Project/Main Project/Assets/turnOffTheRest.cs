using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class turnOffTheRest : MonoBehaviour {

    public GameObject Obj1;
    public GameObject Obj2;
    // Use this for initialization

    private void OnEnable()
    {
        print("I am here");
        Obj1.SetActive(false);
        Obj2.SetActive(false);
    }

 
	
}

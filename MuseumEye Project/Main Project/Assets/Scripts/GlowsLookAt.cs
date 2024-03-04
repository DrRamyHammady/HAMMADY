using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlowsLookAt : MonoBehaviour {

    Transform CameraPose;
    Vector3 newpose;
	
	// Update is called once per frame
	void Start () {
        CameraPose = Camera.main.transform;
	}

    private void Update()
    {
        //  transform.LookAt(CameraPose.position);
        newpose = CameraPose.position - transform.position;
        newpose.y = 0;
        transform.rotation = Quaternion.LookRotation(newpose.normalized);
         


    }
}

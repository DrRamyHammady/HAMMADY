using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyMe : MonoBehaviour {

	// Use this for initialization
	void Start () {
         ObjectManager.InstanceOM.listGO.Add(gameObject);
       
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}

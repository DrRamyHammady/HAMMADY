using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ObjectMoveToScene : MonoBehaviour {
	public int SceneIndex;
	private float nextActionTime = 0.0f;
	public float period = 0.1f;


	// Update is called once per frame
	void Update () {
		if (Time.time > nextActionTime) {
			nextActionTime += period;
			// execute block of code here
			LoadOnIndex (0);
		}
	}
	


		public void LoadOnIndex (int SceneIndex)
		{
			SceneManager.LoadScene (SceneIndex);
			print ("move to scene");
		}


}






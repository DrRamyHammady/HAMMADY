using UnityEngine;
using System.Collections;

public class ColoredCirclesScript : MonoBehaviour {

	// Use this for initialization

	void spin() {
		transform.Rotate (0, 0, - 150 * Time.deltaTime);
	}

	void Update() {
		spin();
	}
}

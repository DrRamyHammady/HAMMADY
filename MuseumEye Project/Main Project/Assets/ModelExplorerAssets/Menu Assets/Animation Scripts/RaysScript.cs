using UnityEngine;
using System.Collections;

public class RaysScript : MonoBehaviour {


	void spin() {
		transform.Rotate (0, 0, -30 * Time.deltaTime);
	}

	void Update() {
		spin();
	}
}

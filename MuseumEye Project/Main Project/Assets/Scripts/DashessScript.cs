using UnityEngine;
using System.Collections;

public class DashessScript : MonoBehaviour {


	void spin() {
		transform.Rotate (0, 0, 60 * Time.deltaTime);
	}

	void Update() {
		spin();
	}
}

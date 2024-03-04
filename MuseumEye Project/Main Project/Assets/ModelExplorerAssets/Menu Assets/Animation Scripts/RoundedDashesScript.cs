using UnityEngine;
using System.Collections;

public class RoundedDashesScript : MonoBehaviour {


	void spin() {
		transform.Rotate (0, 0, 100 * Time.deltaTime);
	}

	void Update() {
		spin();
	}
}

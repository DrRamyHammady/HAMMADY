using UnityEngine;
using System.Collections;

public class ResetButton : MonoBehaviour {

	public void ResetPos ()
	{
		transform.position = new Vector3 (0, -5 , 0);
		transform.rotation = Quaternion.Euler( new Vector3 (-90, -180 , 0));
		transform.localScale = new Vector3 ( 0.4f , 0.4f , 0.4f);

	}
}

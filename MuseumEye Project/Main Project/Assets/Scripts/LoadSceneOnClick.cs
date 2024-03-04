using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
public class LoadSceneOnClick : MonoBehaviour {

	public void LoadOnIndex (int SceneIndex)
	{
		SceneManager.LoadScene (SceneIndex);
	}
}

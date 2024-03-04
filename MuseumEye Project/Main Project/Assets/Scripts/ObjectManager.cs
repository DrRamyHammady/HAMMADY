using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ObjectManager : MonoBehaviour {

    public static ObjectManager InstanceOM;
    public bool Visited;
    public List<GameObject> listGO;

        // Use this for initialization
	void Start () {

        if (InstanceOM == null)
        {
            InstanceOM = this;
            DontDestroyOnLoad(gameObject);
        }
        //else
        //{
          
        //    Destroy(gameObject);
        //}
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        for (int i = 0; i < listGO.Count; i++)
        {
            if (listGO[i] != null)
            {
                Destroy(listGO[i]);
            }
        }
    }

    // Update is called once per frame
    void Update () {
		
	}
}

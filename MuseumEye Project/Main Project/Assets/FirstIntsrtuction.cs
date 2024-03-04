using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstIntsrtuction : MonoBehaviour
{
    public GameObject previousObj;
    public GameObject NextObj;
    private bool flag;
    public float sec = 5f;

    // Use this for initialization
    void Update()
    {
        if (previousObj.activeInHierarchy == false && flag == false)
        {
            StartCoroutine(LateCall());
            flag = true;
        }

    }


    IEnumerator LateCall()
    {

        yield return new WaitForSeconds(sec);

        NextObj.SetActive(true);
        //Do Function here...
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitThenDisabled : MonoBehaviour {

    public float sec = 14f;

    // Use this for initialization

    void OnEnable () {

        StartCoroutine(LateCall());

    }

    IEnumerator LateCall()
{

    yield return new WaitForSeconds(sec);

    gameObject.SetActive(false);
    //Do Function here...
}
}

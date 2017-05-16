using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCheck : MonoBehaviour {

    float fps;
    float deltaTime = 0.0f;
    float msec;

    void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        msec = deltaTime * 1000.0f;
        fps = fps = 1.0f / deltaTime;
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircularBufferTest : MonoBehaviour {
    public CircularBuffer<int> data;
    // Use this for initialization
    void Start () {
        int[] values = { 1, 2, 3, 4 };
        data = new CircularBuffer<int>(10);


        data.Push(1);
        data.Push(2);
        data.Push(3);
        data.Push(4);
        data.Push(5);
        data.Push(6);
        data.Push(7);
        data.Push(8);
        data.Push(9);
        data.Push(10);
        data.Push(11);
        data.Push(12);
        data.Push(13);
        data.Push(14);
        Debug.Log(data.Count);
        for (int i = 0; i < 10; i++)
            Debug.Log(i.ToString()+"번째값:"+(data.getValue(i)).ToString());

    }

    // Update is called once per frame
    void Update () {
		
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Droplet
{
    Vector2 position;
    float time;

    public Droplet()
    {
        time = 1000;
    }

    public void Reset(float x, float y)
    {
        position = new Vector2(x, y);
        time = 0;
    }

    public void Update()
    {
        time += Time.deltaTime;

    }

    public Vector4 MakeShaderParameter(float aspect)
    {
        return new Vector4(position.x * aspect, position.y, time, 0);
    }
}

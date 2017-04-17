using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
using Windows.Kinect;

public class TileTest : MonoBehaviour {

    private bool is_foot = false;

    private Transform tr;

    private int count = 0;

    // Use this for initialization
    void Start() {

        tr = GetComponent<Transform>();

    }

    // Update is called once per frame
    void Update() {

        is_foot = GetPosition(tr.position);

       
    }

    bool GetPosition(Vector3 pos)
    {
        
        RaycastHit hit;

        Ray ray = new Ray(tr.position, Vector3.forward);

        Debug.DrawRay(ray.origin, ray.direction, Color.red, 1000);
        if (!Physics.Raycast(ray, out hit))
            return false;

        Renderer rend = hit.transform.GetComponent<MeshRenderer>();
        MeshCollider meshCollider = (MeshCollider)hit.collider;

        if (rend == null || rend.sharedMaterial == null || rend.sharedMaterial.mainTexture == null || meshCollider == null)
            return false;
        //Debug.Log(hit.transform.name);
        Texture2D tex = (Texture2D)rend.material.mainTexture;
        Vector2 pixelUV = hit.textureCoord;
        pixelUV.x *= tex.width;
        pixelUV.y *= tex.height;

        count = 0;

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                if ((pixelUV.x + i < tex.width && pixelUV.x + i > 0)
                    && (pixelUV.y + j < tex.height && pixelUV.y + j > 0))
                {
                    if (tex.GetPixel((int)pixelUV.x + i, (int)pixelUV.y + j) == Color.green)
                    {
                        count++;
                    }
                }

            }
        }

        //print(tex.GetPixel((int)pixelUV.x, (int)pixelUV.y));

        if (count > 20) return true;
        else return false;
        
    }
}

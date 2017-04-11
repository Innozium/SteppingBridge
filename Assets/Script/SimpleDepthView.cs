using UnityEngine;
using Windows.Kinect;
using System;

public class SimpleDepthView : MonoBehaviour
{
    public GameObject depthSourceManager;
    private DepthSourceManager depthSourceManagerScript;

    Texture2D texture;
    byte[] depthBitmapBuffer;
    FrameDescription depthFrameDesc;

    public float scale = 1.0f;

    void Start()
    {
        // Get the description of the depth frames.
        depthFrameDesc = KinectSensor.GetDefault().DepthFrameSource.FrameDescription;

        // get reference to DepthSourceManager (which is included in the distributed 'Kinect for Windows v2 Unity Plugin zip')
        depthSourceManagerScript = depthSourceManager.GetComponent<DepthSourceManager>();

        // allocate.
        depthBitmapBuffer = new byte[depthFrameDesc.LengthInPixels * 4];
        texture = new Texture2D(depthFrameDesc.Width, depthFrameDesc.Height, TextureFormat.BGRA32, false);

        // arrange size of gameObject to be drawn
        gameObject.transform.localScale = new Vector3(scale * depthFrameDesc.Width / depthFrameDesc.Height, scale, 1.0f);
    }

    void Update()
    {
        updateTexture();
        this.GetComponent<Renderer>().material.mainTexture = texture;
    }

    // DepthMapRange 500 ~ 1500CM Recognition
    int DEPTHMAP_UNIT_CM_MIN = 500;
    int DEPTHMAP_UNIT_CM_MAX = 1500;
    void updateTexture()
    {
        // get new depth data from DepthSourceManager.
        ushort[] rawdata = depthSourceManagerScript.GetData();

        // convert to byte data (
        for (int i = 0; i < rawdata.Length; i++)
        {
            depthBitmapBuffer[i * 4 + 0] = (byte)0; // B // 
            depthBitmapBuffer[i * 4 + 1] = (byte)((DEPTHMAP_UNIT_CM_MIN < rawdata[i]) && (rawdata[i] < DEPTHMAP_UNIT_CM_MAX) ? 255 : 0); // G // COMMON
            depthBitmapBuffer[i * 4 + 2] = (byte)0; // R //
            depthBitmapBuffer[i * 4 + 3] = (byte)((DEPTHMAP_UNIT_CM_MIN < rawdata[i]) && (rawdata[i] < DEPTHMAP_UNIT_CM_MAX) ? 255 : 0); // A // COMMON
        }

        // make texture from byte array
        texture.LoadRawTextureData(depthBitmapBuffer);
        texture.Apply();
    }
}
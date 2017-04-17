using UnityEngine;
using Windows.Kinect;
using System;
using System.Collections;
using System.Collections.Generic;

using OpenCVForUnity;

public class SimpleDepthView : MonoBehaviour
{
    //Kinect V2 FPS 30FPS *3(Color) 30FPS(Depth) , 심도 취득 범위 0.5 ~ 8.0M 
    

   // DepthMapRange 500 ~ 1500CM 임의 설정.. (차후에 발쪽 인식 범위로 할 예정)
   [Range(500, 4500)]
    public int DEPTHMAP_UNIT_CM_MIN = 1000;
    [Range(500, 4500)]
    public int DEPTHMAP_UNIT_CM_MAX = 1500;


    public GameObject depthSourceManager;
    private DepthSourceManager depthSourceManagerScript;

    Texture2D texture;
    byte[] depthBitmapBuffer;
    FrameDescription depthFrameDesc;

    public float scale = 1.0f;
    public int f_DepthMapWidth; // 512
    public int f_DepthMapHeight; // 424 

    void Start()
    {
        // Get the description of the depth frames.
        depthFrameDesc = KinectSensor.GetDefault().DepthFrameSource.FrameDescription;

        // get reference to DepthSourceManager (which is included in the distributed 'Kinect for Windows v2 Unity Plugin zip')
        depthSourceManagerScript = depthSourceManager.GetComponent<DepthSourceManager>();

        // allocate.
        depthBitmapBuffer = new byte[depthFrameDesc.LengthInPixels * 4];
        texture = new Texture2D(depthFrameDesc.Width, depthFrameDesc.Height, TextureFormat.BGRA32, false);
        f_DepthMapWidth = depthFrameDesc.Width;
        f_DepthMapHeight = depthFrameDesc.Height;


        // arrange size of gameObject to be drawn
        gameObject.transform.localScale = new Vector3(scale * depthFrameDesc.Width / depthFrameDesc.Height, scale, 1.0f);


        StartCoroutine(CheckTest());
    }

    void Update()
    {
        updateTexture();
        this.GetComponent<Renderer>().material.mainTexture = texture;
    }

    void updateTexture()
    {
        // get new depth data from DepthSourceManager.
       
        ushort[] rawdata = depthSourceManagerScript.GetData();

        for (int r = 0; r < f_DepthMapHeight; r += 2) //위에서 아래로
        {
            for (int c = 0; c < f_DepthMapWidth; c += 2)//오른쪽에서 왼쪽으로 
            {

                ushort value = rawdata[r * f_DepthMapWidth + c];
                depthBitmapBuffer[(r * f_DepthMapWidth + c) * 4 + 1] =
                    (byte)((DEPTHMAP_UNIT_CM_MIN < value) && (value < DEPTHMAP_UNIT_CM_MAX) ? 255 : 0); // G // COMMON
                depthBitmapBuffer[(r * f_DepthMapWidth + c) * 4 + 3] =
                    (byte)((DEPTHMAP_UNIT_CM_MIN < value) && (value < DEPTHMAP_UNIT_CM_MAX) ? 255 : 0); // G // COMMON
            }
        }

        // make texture from byte array
        texture.LoadRawTextureData(depthBitmapBuffer);
        texture.Apply();
       
    }

    IEnumerator CheckTest()
    {
        // get new depth data from DepthSourceManager.
        ushort[] rawdata = depthSourceManagerScript.GetData();

        int count = 0;
        ushort check1Max = 0;
        ushort check2Max = 0;
        List<ushort> check3 = new List<ushort>();
        List<ushort> check4 = new List<ushort>();

        double sum = 0;
        double average = 0;
        double average2 = 0;
        double average3 = 0;

       

        yield return new WaitForSeconds(5.0f);

        while (true)
        {
            DEPTHMAP_UNIT_CM_MAX = 4500;

            rawdata = depthSourceManagerScript.GetData();

            for (int i = 0; i < rawdata.Length; i += 2)
            {
                if (count == 1)
                {
                    if (check1Max < rawdata[i] && rawdata[i] < DEPTHMAP_UNIT_CM_MAX)
                    {
                        check1Max = rawdata[i];
                    }
                }
                else if (count == 2)
                {
                    if (check2Max < rawdata[i] && rawdata[i] < DEPTHMAP_UNIT_CM_MAX)
                    {
                        check2Max = rawdata[i];
                    }
                }
               
            }

            yield return new WaitForFixedUpdate();

            count++;

            if (count > 2) break;

        }
        print(check1Max);
        print(check2Max);


        sum = (check1Max + check2Max);
        average = (sum / 2);

        print(average);


        do
        {
            yield return new WaitForFixedUpdate();

            rawdata = depthSourceManagerScript.GetData();

            ushort avgMin = (ushort)Mathf.Clamp((ushort)average - 500, 500, 4500);
            ushort avgMax = (ushort)Mathf.Clamp((ushort)average + 500, 500, 4500);

            print(avgMin);
            print(avgMax);

            for (int i = 0; i < rawdata.Length; i += 2)
            {
                if ((avgMin < rawdata[i] && avgMax > rawdata[i]) && rawdata[i] < DEPTHMAP_UNIT_CM_MAX)
                {
                    check3.Add(rawdata[i]);
                }
            }

        } while (false);

        sum = 0;
        sum += (average);
        for (int i = 0; i < check3.Count; i++)
        {
            sum += (check3[i]);
        }
        average2 = (sum / (check3.Count + 1));
        print(sum);
        print(check3.Count);
        print(average2);

        do
        {
            yield return new WaitForFixedUpdate();

            rawdata = depthSourceManagerScript.GetData();

            ushort avgMin = (ushort)Mathf.Clamp((ushort)average2 - 500, 500, 4500);
            ushort avgMax = (ushort)Mathf.Clamp((ushort)average2 + 500, 500, 4500);

            print(avgMin);
            print(avgMax);

            for (int i = 0; i < rawdata.Length; i += 2)
            {
                if ((avgMin < rawdata[i] && avgMax > rawdata[i]) && rawdata[i] < DEPTHMAP_UNIT_CM_MAX)
                {
                    check4.Add(rawdata[i]);
                }
            }

        } while (false);


        sum = 0;
        sum += (average2);
        for (int i = 0; i < check4.Count; i++)
        {
            sum += (check4[i]);
        }
        average3 = (sum / (check4.Count + 1));
        print(sum);
        print(check4.Count);
        print(average3);


        DEPTHMAP_UNIT_CM_MAX = (ushort)average3;
        DEPTHMAP_UNIT_CM_MIN = DEPTHMAP_UNIT_CM_MAX - 100;
        //DEPTHMAP_UNIT_CM_MAX -= 50;
    }

}


using UnityEngine;
using Windows.Kinect;
using System;
using System.Collections;
using System.Collections.Generic;
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
        ushort ave;
        //print(ave);

        for (int r = 0; r < f_DepthMapHeight; r += 1) //위에서 아래로
        {
            for (int c = 0; c < f_DepthMapWidth; c += 1)//오른쪽에서 왼쪽으로 
            {

                ushort value = rawdata[r * f_DepthMapWidth + c];
                depthBitmapBuffer[(r * f_DepthMapWidth + c) * 4 + 1] =
                    (byte)((DEPTHMAP_UNIT_CM_MIN < value) && (value < DEPTHMAP_UNIT_CM_MAX) ? 255 : 0); // G // COMMON
                depthBitmapBuffer[(r * f_DepthMapWidth + c) * 4 + 1] =
                    (byte)((DEPTHMAP_UNIT_CM_MIN < value) && (value < DEPTHMAP_UNIT_CM_MAX) ? 255 : 0); // G // COMMON
            }
        }

        // make texture from byte array
        texture.LoadRawTextureData(depthBitmapBuffer);
        texture.Apply();
    }

    IEnumerator CheckTest()
    {
        int count = 0;
        while(true)
        {
            BottomCheck();

            yield return new WaitForFixedUpdate();
            if (count > 2) break;

            count++;

        }
    }


    void BottomCheck()
    {
        // get new depth data from DepthSourceManager.
        ushort[] rawdata = depthSourceManagerScript.GetData();

        bool check = false;
        DEPTHMAP_UNIT_CM_MAX = 4500;

        

        while (!check)
        {
            int count = 0;

            for (int i = 0; i < rawdata.Length; i++)
            {
                //적외선이 충돌되는 물체가 없다면 최대값? 일테니
                //충된 객체가 많은 범위를 찾기위해
                //도달한 길이가 Max값보다 작은게 많다면
                //카운트 증가
                if (rawdata[i] <= DEPTHMAP_UNIT_CM_MAX)
                {
                    count++;
                }
            }

            //카운트가 쏘아진 적외선 갯수의 일정개수 보다 많다면 아직 최소길이가 아니다.
            //그렇다면 최대길이를 감소.
            //카운트가 더 적다면 그 길이로 부딪힌 영역이 많다는 소리이니...
            //그것을 바닥으로 해보자.
            if (count >= rawdata.Length - (rawdata.Length / 4)) DEPTHMAP_UNIT_CM_MAX -= 5;
            else check = true;

            if(check) print(count);
            if (DEPTHMAP_UNIT_CM_MAX <= 500) break;
        }

        DEPTHMAP_UNIT_CM_MIN = DEPTHMAP_UNIT_CM_MAX - 100;
        DEPTHMAP_UNIT_CM_MAX -= 50;
        //print(count);

    }


}


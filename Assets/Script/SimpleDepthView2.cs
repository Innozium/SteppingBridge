
using UnityEngine;
using Windows.Kinect;
using System;
using System.Collections;
using System.Collections.Generic;

using OpenCVForUnity;
// Declare the generic class.


public class SimpleDepthView2 : MonoBehaviour
{
    /*
     * Kinect V2 FPS 30FPS *3(Color) 30FPS(Depth) , 심도 취득 범위 0.5 ~ 8.0M 
     * DepthMapRange 500 ~ 1500CM 임의 설정.. (차후에 발쪽 인식 범위로 할 예정)
     * 
     * v1은 투광 한 적외선 패턴을 읽기 패턴의 왜곡에서 Depth 정보를 얻을 LightCoding방식의 Depth센서
     * v2는 투광한 적외선 펄스가 반사되어 돌아올때까지의 시간에서 Depth 정보 읽음 Time of Flight(TOF)방식
     */
    //FOR BACKGROUND SUBTRACTION METHODS 
    //three Mat objects are allocated to store the current frame and two foreground masks, obtained by using two different BS algorithms.

    public CircularBuffer<Mat> matBuffer;
    public const int MAT_BUFFER_SIZE = 30;//합영상 프레임 더하는 횟수 ex :  30프레임
    int sumCount;
    Mat sumMat;
    Mat avgMat;
    Mat prevMat;






    //비교 대상 위한 메테리얼& 텍스쳐
    public GameObject obj;
    public Texture2D tex;


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
        //wscommit
        tex = new Texture2D(depthFrameDesc.Width, depthFrameDesc.Height, TextureFormat.BGRA32, false);


        f_DepthMapWidth = depthFrameDesc.Width;
        f_DepthMapHeight = depthFrameDesc.Height;


        // arrange size of gameObject to be drawn
        gameObject.transform.localScale = new Vector3(scale * depthFrameDesc.Width / depthFrameDesc.Height, scale, 1.0f);

        //wscommit
        obj.transform.localScale = new Vector3(scale * depthFrameDesc.Width / depthFrameDesc.Height, scale, 1.0f);

        prevMat = new Mat(texture.height, texture.width, CvType.CV_8UC1);//1차원 행렬 선언
        sumMat = new Mat(texture.height, texture.width, CvType.CV_8UC1);//1차원 행렬 선언
        avgMat = new Mat(texture.height, texture.width, CvType.CV_8UC1);//1차원 행렬 선언
        //합영상을 위한 버퍼
        matBuffer = new CircularBuffer<Mat>(MAT_BUFFER_SIZE);
        sumCount = 0;
        // StartCoroutine(CheckTest());
    }

    void Update()
    {
        updateTexture();
        this.GetComponent<Renderer>().material.mainTexture = texture;
        obj.GetComponent<Renderer>().material.mainTexture = tex;
    }

    void updateTexture()
    {
        // get new depth data from DepthSourceManager.
        ushort[] rawdata = depthSourceManagerScript.GetData();

        //print(ave);

        for (int r = 0; r < f_DepthMapHeight; r += 1) //위에서 아래로
        {
            for (int c = 0; c < f_DepthMapWidth; c += 1)//오른쪽에서 왼쪽으로 
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

        /*
         *  noise delete!
         */

        Mat imgMat = new Mat(texture.height, texture.width, CvType.CV_8UC3);//img행렬 선언

        Mat dstMat = new Mat(texture.height, texture.width, CvType.CV_8UC1);//1차원 행렬 선언


        Utils.texture2DToMat(texture, imgMat);//texture값(깊이값 반영된) 행렬 대입

        Mat grayMat = new Mat();//1채널 영상 선언
        //색변경
        Imgproc.cvtColor(imgMat, grayMat, Imgproc.COLOR_RGB2GRAY);//컬러영상 0-> 흑백 영상으로 


        Mat kernel = new Mat(7, 7, CvType.CV_8U, new Scalar(1));







        dstMat = grayMat;
        Mat resultMat;
        resultMat = new Mat(texture.height, texture.width, CvType.CV_8UC1);//1차원 행렬 선언

        resultMat = dstMat;
        matBuffer.Push(dstMat);
        if (sumCount < MAT_BUFFER_SIZE)//MAX_BUFFER_SIZE횟수만큼 전까지는 평균값 구하지 않음
        {
            sumCount += 1;
        }
        else//횟수 채웠으니 평균값 구하기
        {

            sumMat = Mat.zeros(texture.height, texture.width, CvType.CV_8UC1);// 합영상 
            for (int i = 0; i < MAT_BUFFER_SIZE; i++)
            {
                Core.add(sumMat, matBuffer.getValue(i), sumMat);//합영상 구하기. 
            }
         


        }
        /*
         Core.absdiff(prevMat, dstMat, resultMat);
         if(Input.GetKeyDown(KeyCode.Space))
             prevMat = dstMat;
         Imgproc.threshold(resultMat, resultMat, 30, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);
         Imgproc.erode(resultMat, resultMat, kernel);//병목
         Imgproc.dilate(resultMat, resultMat, kernel);//팽창. 
         */



        Utils.matToTexture2D(grayMat, texture);//원본 깊이값 영상 텍스쳐로 전환
        Utils.matToTexture2D(sumMat, tex);//비교 대상 깊이값 영상  텍스쳐로 전환
        texture.Apply();//텍스쳐 적용
        tex.Apply();
        // Imgproc.Canny(grayMat, grayMat, 50, 200);//윤곽 

    }


}


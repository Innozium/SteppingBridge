﻿
using UnityEngine;
using Windows.Kinect;
using System;
using System.Collections;
using System.Collections.Generic;

using OpenCVForUnity;
// Declare the generic class.



public class SimpleDepthView : MonoBehaviour
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

    private Renderer renderer;

    public CircularBuffer<Mat> matBuffer;
    public CircularBuffer<Mat> matBuffer2;
    public CircularBuffer<Mat> matBuffer3;

    public const int MAT_BUFFER_SIZE = 5;//합영상 프레임 더하는 횟수 ex :  30프레임
    int sumCount1;
    int sumCount2;
    int sumCount3;

    Mat sumMat;
    Mat avgMat;
    Mat prevMat;
    Mat convert32Mat;
    Mat convert8Mat;

    Mat resultMat1;
    Mat resultMat2;
    Mat resultMat3;


    [Range(500, 4500)]
    public int DEPTHMAP_UNIT_CM_MIN = 1000;
    [Range(500, 8000)]
    public int DEPTHMAP_UNIT_CM_MAX = 8000;

    public GameObject depthSourceManager;
    private DepthSourceManager depthSourceManagerScript;

    Texture2D texture;

    public GameObject subObj;
    public Texture2D subTexture;

    byte[] depthBitmapBuffer;
    byte[] depthBitmapBuffer2;
    byte[] depthBitmapBuffer3;
    FrameDescription depthFrameDesc;

    public float scale = 1.0f;
    public int f_DepthMapWidth; // 512
    public int f_DepthMapHeight; // 424 

    [Range(50, 20000)]
    public int footMinArea = 4000;
    [Range(50, 20000)]
    public int footMaxArea = 20000;

    //테스트용 프리팹
    public GameObject particleObj;
    //프리팹에서 생성한 객체를 가지고 있을 리스트
    public List<ParticleObjClass> particleObjs = new List<ParticleObjClass>();
    //프리팹이 생성될 최대 갯수
    private const int PARTICLE_OBJ_MAX = 20;

    public bool is_test = false;
    public bool is_abs = false;

    [Range(300, 8000)]
    public int v = 300;

    #region ParticleObj부분
    private void ParticleObjInit()
    {
        //만들어줄 프리팹을 가지고 있다면.
        if (particleObj != null)
        {
            //오브젝트 생성 후 리스트에 담아준다.
            for (int i = 0; i < PARTICLE_OBJ_MAX; i++)
            {
                ParticleObjClass newParticle = new ParticleObjClass();
                newParticle.obj = (GameObject)Instantiate(particleObj);
                newParticle.obj.name = "testParticle" + i.ToString();
                newParticle.obj.SetActive(false);
                newParticle.lifeTime = 1.0f;
                particleObjs.Add(newParticle);
            }
        }
    }

    //좌표값을 넘겨 받아 위치를 구해서 오브젝트를 활성화 시킨다.
    //공식은 다음과 같다.
    //PLAYER_MINI_X = MINI_WIDTH* ( PLAYER_WORLD_X / WORLD_WIDTH )
    //PLAYER_MINI_Y = MINI_HEIGHT* ( PLAYER_WORLD_Y / WORLD_HEIGHT )
    private void OnParticleObjActive(int x, int y)
    {

        float posX = -20 * (x / (float)texture.width);
        float posY = 20 * (y / (float)texture.height);

        for (int i = 0; i < particleObjs.Count; i++)
        {
            //print(particleObjs[i].obj.activeSelf);
            //particleObjs가 현재 죽어있다면 살리고 위치를 지정해주자.
            if (!particleObjs[i].obj.activeSelf)
            {
                particleObjs[i].obj.SetActive(true);
                particleObjs[i].obj.transform.position = new Vector3(posX, posY, 0.0f);
                return;
            }
        }
    }

    //생존시간을 체크하고 0 이상이라면 감소시키고
    //0 이하라면 초기화 후 비활성화. 
    private void OnParticleObjTimeCheck()
    {
        for (int i = 0; i < particleObjs.Count; i++)
        {
            //particleObjs가 현재 살아있다면
            if (particleObjs[i].obj.activeSelf)
            { //lifeTime을 줄이자.
                particleObjs[i].lifeTime -= Time.deltaTime;

                //줄인 LifeTime이 0 이하라면
                if (particleObjs[i].lifeTime < 0)
                {
                    //시간을 초기화해주고
                    particleObjs[i].lifeTime = 1.0f;
                    //죽이자.
                    particleObjs[i].obj.SetActive(false);
                }

            }
        }
    }
    #endregion

    void Start()
    {
        renderer = GetComponent<Renderer>();

        // Get the description of the depth frames.
        depthFrameDesc = KinectSensor.GetDefault().DepthFrameSource.FrameDescription;

        // get reference to DepthSourceManager (which is included in the distributed 'Kinect for Windows v2 Unity Plugin zip')
        depthSourceManagerScript = depthSourceManager.GetComponent<DepthSourceManager>();

        // allocate.
        depthBitmapBuffer = new byte[depthFrameDesc.LengthInPixels * 4];
        depthBitmapBuffer2 = new byte[depthFrameDesc.LengthInPixels * 4];
        depthBitmapBuffer3 = new byte[depthFrameDesc.LengthInPixels * 4];

        texture = new Texture2D(depthFrameDesc.Width, depthFrameDesc.Height, TextureFormat.BGRA32, false);
        subTexture = new Texture2D(depthFrameDesc.Width, depthFrameDesc.Height, TextureFormat.BGRA32, false);

        f_DepthMapWidth = depthFrameDesc.Width;
        f_DepthMapHeight = depthFrameDesc.Height;

        // arrange size of gameObject to be drawn
        gameObject.transform.localScale = new Vector3(scale * depthFrameDesc.Width / depthFrameDesc.Height, scale, 1.0f);
        subObj.transform.localScale = new Vector3(scale * depthFrameDesc.Width / depthFrameDesc.Height, scale, 1.0f);


        prevMat = new Mat(texture.height, texture.width, CvType.CV_8UC1);//1차원 행렬 선언

        sumMat = new Mat(texture.height, texture.width, CvType.CV_32FC1);//1차원 행렬 선언
        avgMat = new Mat(texture.height, texture.width, CvType.CV_32FC1);//1차원 행렬 선언
        convert32Mat = new Mat(texture.height, texture.width, CvType.CV_32FC1);
        convert8Mat = new Mat(texture.height, texture.width, CvType.CV_8UC1);

        resultMat1 = new Mat(texture.height, texture.width, CvType.CV_8UC1);
        resultMat2 = new Mat(texture.height, texture.width, CvType.CV_8UC1);
        resultMat3 = new Mat(texture.height, texture.width, CvType.CV_8UC1);

        //평균을 내기위해 avgMat에 값을 넣어준다.
        double data = MAT_BUFFER_SIZE - 1;
        for (int i = 0; i < avgMat.height(); i++)
        {
            for (int j = 0; j < avgMat.width(); j++)
            {
                avgMat.put(i, j, data);
            }
        }

        //합영상을 위한 버퍼
        matBuffer = new CircularBuffer<Mat>(MAT_BUFFER_SIZE);
        matBuffer2 = new CircularBuffer<Mat>(MAT_BUFFER_SIZE);
        matBuffer3 = new CircularBuffer<Mat>(MAT_BUFFER_SIZE);

        sumCount1 = 0;
        sumCount2 = 0;
        sumCount3 = 0;


        //파티클 오브젝트를 생성한다.
        ParticleObjInit();
    }

    void Update()
    {
        updateTexture();
        renderer.material.mainTexture = texture;
        subObj.GetComponent<Renderer>().material.mainTexture = subTexture;
    }

    public Mat avgMatReturn(Texture2D texture, CircularBuffer<Mat> matBuffer, ref int sumCount)
    {
        Mat imgMat = new Mat(texture.height, texture.width, CvType.CV_8UC3);//img행렬 선언

        Mat dstMat = new Mat(texture.height, texture.width, CvType.CV_8UC1);//1차원 행렬 선언

        Mat nowMat = new Mat(texture.height, texture.width, CvType.CV_8UC1);//1차원 행렬 선언

        Utils.texture2DToMat(texture, imgMat);//texture값(깊이값 반영된) 행렬 대입

        Mat grayMat = new Mat(texture.height, texture.width, CvType.CV_8UC1);//1채널 영상 선언
        //색변경
        Imgproc.cvtColor(imgMat, grayMat, Imgproc.COLOR_RGB2GRAY);//컬러영상 0-> 흑백 영상으로 

        Mat kernel = new Mat(7, 7, CvType.CV_8U, new Scalar(1));

        convert32Mat = Mat.zeros(texture.height, texture.width, CvType.CV_32FC1);

        nowMat = grayMat;

        dstMat = grayMat;

        dstMat.convertTo(convert32Mat, CvType.CV_32FC1);

        matBuffer.Push(convert32Mat);

        //print(sumCount);
        //print("buffer" + matBuffer.Count);

        if (sumCount < MAT_BUFFER_SIZE)//MAX_BUFFER_SIZE횟수만큼 전까지는 평균값 구하지 않음
        {
            sumCount += 1;
        }
        else//횟수 채웠으니 평균값 구하기
        {

            sumMat = Mat.zeros(texture.height, texture.width, CvType.CV_32FC1);// 합영상 
            for (int i = 0; i < MAT_BUFFER_SIZE - 1; i++)
            {
                Core.add(sumMat, matBuffer.getValue(i), sumMat);//합영상 구하기. 
            }

            Core.divide(sumMat, avgMat, sumMat);

        }

        convert8Mat = Mat.zeros(texture.height, texture.width, CvType.CV_8UC1);

        sumMat.convertTo(convert8Mat, CvType.CV_8UC1);

        Mat resultMat = new Mat(texture.height, texture.width, CvType.CV_8UC1);//1차원 행렬 선언

        Core.subtract(convert8Mat, nowMat, resultMat);

        return resultMat;
    }

    void updateTexture()
    {
        resultMat1 = Mat.zeros(texture.height, texture.width, CvType.CV_8UC1);
        resultMat2 = Mat.zeros(texture.height, texture.width, CvType.CV_8UC1);
        resultMat3 = Mat.zeros(texture.height, texture.width, CvType.CV_8UC1);

        // get new depth data from DepthSourceManager.
        ushort[] rawdata = depthSourceManagerScript.GetData();

        v = 2500;

        for (int r = 0; r < f_DepthMapHeight; r += 1) //위에서 아래로
        {
            for (int c = 0; c < f_DepthMapWidth; c += 1)//오른쪽에서 왼쪽으로 
            {
                //value / (v / 256)

                ushort value = rawdata[r * f_DepthMapWidth + c];
                depthBitmapBuffer[(r * f_DepthMapWidth + c) * 4 + 1] =
                    (byte)((500 < value) && (value < 2500) ? 255 - (256 * value / (v)) : 0); // G // COMMON
                depthBitmapBuffer[(r * f_DepthMapWidth + c) * 4 + 3] =
                    (byte)((500 < value) && (value < 2500) ? 255 - (256 * value / (v)) : 0); // A // COMMON
            }
        }

        // make texture from byte array
        texture.LoadRawTextureData(depthBitmapBuffer);
        texture.Apply();
        
        resultMat1 = avgMatReturn(texture, matBuffer, ref sumCount1);

        v = 3200;

        for (int r = 0; r < f_DepthMapHeight; r += 1) //위에서 아래로
        {
            for (int c = 0; c < f_DepthMapWidth; c += 1)//오른쪽에서 왼쪽으로 
            {
                ushort value = rawdata[r * f_DepthMapWidth + c];
                depthBitmapBuffer[(r * f_DepthMapWidth + c) * 4 + 1] =
                    (byte)((2500 < value) && (value < 3200) ? 255 - (256 * value / (v)) : 0); // G // COMMON
                depthBitmapBuffer[(r * f_DepthMapWidth + c) * 4 + 3] =
                    (byte)((2500 < value) && (value < 3200) ? 255 - (256 * value / (v)) : 0); // A // COMMON
            }
        }

        // make texture from byte array
        texture.LoadRawTextureData(depthBitmapBuffer);
        texture.Apply();

        resultMat2 = avgMatReturn(texture, matBuffer2, ref sumCount2);

        v = 5000;

        for (int r = 0; r < f_DepthMapHeight; r += 1) //위에서 아래로
        {
            for (int c = 0; c < f_DepthMapWidth; c += 1)//오른쪽에서 왼쪽으로 
            {

                ushort value = rawdata[r * f_DepthMapWidth + c];
                depthBitmapBuffer[(r * f_DepthMapWidth + c) * 4 + 1] =
                    (byte)((3200 < value) && (value < 5000) ? 255 - (256 * value / (v)) : 0); // G // COMMON
                depthBitmapBuffer[(r * f_DepthMapWidth + c) * 4 + 3] =
                    (byte)((3200 < value) && (value < 5000) ? 255 - (256 * value / (v)) : 0); // A // COMMON
            }
        }

        // make texture from byte array
        texture.LoadRawTextureData(depthBitmapBuffer);
        texture.Apply();

        resultMat3 = avgMatReturn(texture, matBuffer3, ref sumCount3);

        Mat totalMat = new Mat(texture.height, texture.width, CvType.CV_8UC1);
        totalMat = Mat.zeros(texture.height, texture.width, CvType.CV_8UC1);

        Core.add(totalMat, resultMat1, totalMat);//합영상 구하기. 
        Core.add(totalMat, resultMat2, totalMat);//합영상 구하기. 
        Core.add(totalMat, resultMat3, totalMat);//합영상 구하기. 

        Utils.matToTexture2D(totalMat, texture);//원본 깊이값 영상 텍스쳐로 전환
        texture.Apply();//텍스쳐 적용

        Mat kernel = new Mat(7, 7, CvType.CV_8U, new Scalar(1));

        Imgproc.threshold(convert8Mat, convert8Mat, 5, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);
        Imgproc.threshold(totalMat, totalMat, 5, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);

        Imgproc.erode(totalMat, totalMat, kernel);//병목
        Imgproc.dilate(totalMat, totalMat, kernel);//팽창. 
        Imgproc.dilate(totalMat, totalMat, kernel);//팽창. 

        List<Point> touchPoints = new List<Point>();

        Mat hierarchy = new Mat();
        List<MatOfPoint> contours = new List<MatOfPoint>();
        //RETR_EXTERNAL
        Imgproc.findContours(totalMat, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

        for (int i = 0; i < contours.Count; i++)
        {
            Mat contourMat = new Mat();
            contourMat = contours[i];
            double contourArea = Imgproc.contourArea(contourMat);
            if (contourArea > footMinArea
                //&& contourArea < footMaxArea
                )
            {
                Scalar center = Core.mean(contourMat);
                Point footPoint = new Point(center.val[0], center.val[1]);
                touchPoints.Add(footPoint);
            }
            Scalar color = new Scalar(255, 0, 0);
            Imgproc.drawContours(totalMat, contours, i, color, 7);
        }

        for (int i = 0; i < touchPoints.Count; i++)
        { // touch points
            Scalar color = new Scalar(255, 0, 0);
            Imgproc.circle(totalMat, touchPoints[i], 50, color, 7);
            int delta_x = (int)touchPoints[i].x;
            int delta_y = (int)touchPoints[i].y;
            OnParticleObjActive(delta_x, delta_y);
        }

        //현재 활성화중인 ParticleObj의 lifeTime을 확인하기 위해!
        OnParticleObjTimeCheck();

        Utils.matToTexture2D(totalMat, subTexture);//원본 깊이값 영상 텍스쳐로 전환
        subTexture.Apply();//텍스쳐 적용
        
    }


}


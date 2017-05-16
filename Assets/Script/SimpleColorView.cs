using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using OpenCVForUnity;

public class SimpleColorView : MonoBehaviour
{
    public GameObject ColorSourceManager;
    private ColorSourceManager _ColorManager;

    [Range(50, 20000)]
    public int footMinArea = 4000;
    [Range(50, 20000)]
    public int footMaxArea = 20000;

    Texture2D textureColor;
    Texture2D texture;

    private Renderer renderer;

    public CircularBuffer<Mat> matBuffer;
    public const int MAT_BUFFER_SIZE = 5;//합영상 프레임 더하는 횟수 ex :  30프레임
    int sumCount;
    Mat sumMat;
    Mat avgMat;
    Mat prevMat;
    Mat convert32Mat;
    Mat convert8Mat;

    private int resizeWidth = 512;
    private int resizeHiehgt = 424;

    void Start()
    {
        gameObject.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(-1, 1));

        _ColorManager = ColorSourceManager.GetComponent<ColorSourceManager>();

        textureColor = _ColorManager.GetColorTexture();
        texture = new Texture2D(resizeWidth, resizeHiehgt, TextureFormat.BGRA32, false);

        renderer = GetComponent<Renderer>();

        prevMat = new Mat(resizeHiehgt, resizeWidth, CvType.CV_8UC1);//1차원 행렬 선언

        sumMat = new Mat(resizeHiehgt, resizeWidth, CvType.CV_32FC1);//1차원 행렬 선언
        avgMat = new Mat(resizeHiehgt, resizeWidth, CvType.CV_32FC1);//1차원 행렬 선언
        convert32Mat = new Mat(resizeHiehgt, resizeWidth, CvType.CV_32FC1);
        convert8Mat = new Mat(resizeHiehgt, resizeWidth, CvType.CV_8UC1);
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
        sumCount = 0;

        print(texture.height + " " + texture.width);

        
    }

    void Update()
    {

        updateTexture();
        renderer.material.mainTexture = texture;



    }

    public void updateTexture()
    {
        textureColor = _ColorManager.GetColorTexture();

        Mat imgMat = new Mat(textureColor.height, textureColor.width, CvType.CV_8UC3);//img행렬 선언

        Mat dstMat = new Mat(resizeHiehgt, resizeWidth, CvType.CV_8UC1);//1차원 행렬 선언

        Mat nowMat = new Mat(resizeHiehgt, resizeWidth, CvType.CV_8UC1);//1차원 행렬 선언

        Utils.texture2DToMat(textureColor, imgMat);//texture값(깊이값 반영된) 행렬 대입

        Mat resizeMat = new Mat(resizeHiehgt, resizeWidth, CvType.CV_8UC1);

        Imgproc.resize(imgMat, resizeMat, new Size(resizeWidth, resizeHiehgt));

        Mat grayMat = new Mat(resizeHiehgt, resizeWidth, CvType.CV_8UC1);//1채널 영상 선언
        //색변경
        Imgproc.cvtColor(resizeMat, grayMat, Imgproc.COLOR_RGB2GRAY);//컬러영상 0-> 흑백 영상으로 

        Mat kernel = new Mat(7, 7, CvType.CV_8U, new Scalar(1));

        convert32Mat = Mat.zeros(resizeHiehgt, resizeWidth, CvType.CV_32FC1);

        //Imgproc.threshold(grayMat, grayMat, 25, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);

        nowMat = grayMat;

        dstMat = grayMat;

        dstMat.convertTo(convert32Mat, CvType.CV_32FC1);

        matBuffer.Push(convert32Mat);
        if (sumCount < MAT_BUFFER_SIZE)//MAX_BUFFER_SIZE횟수만큼 전까지는 평균값 구하지 않음
        {
            sumCount += 1;
        }
        else//횟수 채웠으니 평균값 구하기
        {

            sumMat = Mat.zeros(resizeHiehgt, resizeWidth, CvType.CV_32FC1);// 합영상 
            for (int i = 0; i < MAT_BUFFER_SIZE - 1; i++)
            {
                Core.add(sumMat, matBuffer.getValue(i), sumMat);//합영상 구하기. 
            }

            Core.divide(sumMat, avgMat, sumMat);

        }

        convert8Mat = Mat.zeros(resizeHiehgt, resizeWidth, CvType.CV_8UC1);

        sumMat.convertTo(convert8Mat, CvType.CV_8UC1);

        Mat resultMat = new Mat(resizeHiehgt, resizeWidth, CvType.CV_8UC1);//1차원 행렬 선언

        //Imgproc.threshold(convert8Mat, convert8Mat, 25, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);
        //Imgproc.threshold(nowMat, nowMat, 25, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);

        Core.subtract(convert8Mat, nowMat, resultMat);
        Core.subtract(convert8Mat, nowMat, nowMat);

        print("color diff(convert8) " + convert8Mat.get(150, 150)[0]);
        print("color diff(now) " + nowMat.get(150, 150)[0]);
        print("color diff(resultMat) " + resultMat.get(150, 150)[0]);

        Imgproc.threshold(nowMat, nowMat, 25, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);

        //Imgproc.threshold(convert8Mat, convert8Mat, 5, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);
        Imgproc.threshold(resultMat, resultMat, 5, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);

        Imgproc.erode(resultMat, resultMat, kernel);//병목
        Imgproc.dilate(resultMat, resultMat, kernel);//팽창. 
        Imgproc.dilate(resultMat, resultMat, kernel);//팽창. 
        //Imgproc.erode(resultMat, resultMat, kernel);//병목
        //Imgproc.dilate(resultMat, resultMat, kernel);//팽창.
        //Imgproc.dilate(resultMat, resultMat, kernel);//팽창.
        //Imgproc.dilate(resultMat, resultMat, kernel);//팽창.
        //Imgproc.morphologyEx(resultMat, resultMat, Imgproc.MORPH_GRADIENT, kernel);
        //Imgproc.morphologyEx(resultMat, resultMat, Imgproc.MORPH_CLOSE, kernel);
        //Imgproc.morphologyEx(resultMat, resultMat, Imgproc.MORPH_OPEN, kernel); 

        List<Point> touchPoints = new List<Point>();

        Mat hierarchy = new Mat();
        List<MatOfPoint> contours = new List<MatOfPoint>();
        //RETR_EXTERNAL
        Imgproc.findContours(resultMat, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

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
            Imgproc.drawContours(resultMat, contours, i, color, 7);
        }

        for (int i = 0; i < touchPoints.Count; i++)
        { // touch points
            Scalar color = new Scalar(255, 0, 0);
            Imgproc.circle(resultMat, touchPoints[i], 50, color, 7);
        }

        //print("color contours : " + contours.Count);

        Utils.matToTexture2D(resultMat, texture);//원본 깊이값 영상 텍스쳐로 전환
        texture.Apply();//텍스쳐 적용

    }



}

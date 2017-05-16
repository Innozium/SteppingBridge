using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using OpenCVForUnity;

//윤곽선이 잡힌 포인트에 생성될 오브젝트의 정보 담을 클래스.
public class ParticleObjClass
{
    public GameObject obj; //오브젝트
    public RippleEffect rippleEffect; //RippleEffect 스크립트
    public float lifeTime; //살아있을 시간
}

public class SimpleInfraredView : MonoBehaviour
{
    //테스트용 프리팹
    public GameObject particleObj;
    //프리팹에서 생성한 객체를 가지고 있을 리스트
    public List<ParticleObjClass> particleObjs = new List<ParticleObjClass>();
    //프리팹이 생성될 최대 갯수
    private const int PARTICLE_OBJ_MAX = 5;
    //프리팹이 살아 있을 시간
    private const float PARTICLE_LIFE_TIME = 2.0f;

    //적외선 카메라 정보를 담을 오브젝트
    public GameObject InfraredSourceManager;
    private InfraredSourceManager _InfraredManager;

    //윤곽이 잡힌 객체들 중 터치 포인트로 잡을 범위 Min
    [Range(50, 20000)]
    public int footMinArea = 50;
    //윤곽이 잡힌 객체들 중 터치 포인트로 잡을 범위 Max
    [Range(50, 20000)]
    public int footMaxArea = 1000;

    //InfraredSourceManager에서 가져온 텍스처를 적용할 텍스처
    private Texture2D infraredTexture;
    //최종적으로 윤곽을 적용할 텍스처(확인용으로만 사용된다.)
    private Texture2D resultTexture;

    private Renderer renderer;

    public CircularBuffer<Mat> matBuffer;
    public const int MAT_BUFFER_SIZE = 10;//합영상 프레임 더하는 횟수 ex :  30프레임

    private int sumCount;
    private Mat sumMat;
    private Mat avgMat;
    private Mat prevMat;
    private Mat convert32Mat;
    private Mat convert8Mat;

    private int InfraredMapWidth = 512;
    private int InfraredMapHiehgt = 424;

    private const float PARTICLE_WORLD_X_MAX = 1;
    private const float PARTICLE_WORLD_Y_MAX = 1;


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
                newParticle.rippleEffect = newParticle.obj.GetComponent<RippleEffect>();
                newParticle.lifeTime = PARTICLE_LIFE_TIME;
                particleObjs.Add(newParticle);
            }
        }
    }

    //좌표값을 넘겨 받아 위치를 구해서 오브젝트를 활성화 시킨다.
    //공식은 다음과 같다.
    //PLAYER_MINI_X = MINI_WIDTH* ( PLAYER_WORLD_X / WORLD_WIDTH )
    //PLAYER_MINI_Y = MINI_HEIGHT* ( PLAYER_WORLD_Y / WORLD_HEIGHT )
    private void OnParticleObjActive(float x, float y)
    {
        ////x값은 결국 월드 0~20 값이 나온다.
        //float posX = (20 * (x / (float)resultTexture.width));
        ////y값은 결국 월드 0~20 값이 나온다.
        //float posY = 20 * (y / (float)resultTexture.height);

        //x값은 결국 월드 0~20 값이 나온다.
        float posX = (PARTICLE_WORLD_X_MAX * (x / (float)resultTexture.width));
        //y값은 결국 월드 0~20 값이 나온다.
        float posY = PARTICLE_WORLD_Y_MAX - PARTICLE_WORLD_Y_MAX * (y / (float)resultTexture.height);

        for (int i = 0; i < particleObjs.Count; i++)
        {
            //print(particleObjs[i].obj.activeSelf);
            //particleObjs가 현재 죽어있다면 살리고 위치를 지정해주자.
            if (!particleObjs[i].obj.activeSelf)
            {
                //활성화 시키고
                particleObjs[i].obj.SetActive(true);
                //위에서 계산한 x,y로 위치를 잡는다.
                particleObjs[i].rippleEffect.Emit(posX, posY);
                //particleObjs[i].obj.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

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
                    particleObjs[i].lifeTime = PARTICLE_LIFE_TIME;
                    //죽이자.
                    particleObjs[i].obj.SetActive(false);
                }

            }
        }
    }
    #endregion

    void Start()
    {
        gameObject.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(-1, 1));

        //InfraredSourceManager를 담아준다.
        _InfraredManager = InfraredSourceManager.GetComponent<InfraredSourceManager>();
        //InfraredFrameSource의 FrameDescription를 담아준다.
        FrameDescription frameDesc = KinectSensor.GetDefault().InfraredFrameSource.FrameDescription;

        //텍스처들을 초기화 해준다.
        infraredTexture = new Texture2D(frameDesc.Width, frameDesc.Height, TextureFormat.BGRA32, false);
        resultTexture = new Texture2D(frameDesc.Width, frameDesc.Height, TextureFormat.BGRA32, false);

        //_InfraredManager에서 적외선 카메라에 대한 텍스처를 받아와 적용시킨다.
        infraredTexture = _InfraredManager.GetInfraredTexture();
        //적외선 카메라 텍스처의 크기의 가로, 세로를 각각 저장한다.
        InfraredMapHiehgt = frameDesc.Height;
        InfraredMapWidth = frameDesc.Width;

        //Renderer를 담아둔다.
        renderer = gameObject.GetComponent<Renderer>();

        //사용할 Mat을 용도에 맞게 초기화 시켜둔다.
        prevMat = new Mat(InfraredMapHiehgt, InfraredMapWidth, CvType.CV_8UC1);//1차원 행렬 선언
        sumMat = new Mat(InfraredMapHiehgt, InfraredMapWidth, CvType.CV_32FC1);//1차원 행렬 선언
        avgMat = new Mat(InfraredMapHiehgt, InfraredMapWidth, CvType.CV_32FC1);//1차원 행렬 선언
        convert32Mat = new Mat(InfraredMapHiehgt, InfraredMapWidth, CvType.CV_32FC1);
        convert8Mat = new Mat(InfraredMapHiehgt, InfraredMapWidth, CvType.CV_8UC1);

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

        //파티클 오브젝트를 생성한다.
        ParticleObjInit();

    }

    void Update()
    {
        //텍스처를 업데이트한다.
        updateTexture();
        //업데이트한 텍스처를 적용시킨다.(이 작업은 확인용이라 안해도 무관하다.)
        //renderer.material.mainTexture = resultTexture;

    }

    public void updateTexture()
    {
        //_InfraredManager에서 적외선 카메라에 대한 텍스처를 받아와 적용시킨다.
        infraredTexture = _InfraredManager.GetInfraredTexture();
        infraredTexture.Apply();

        Mat imgMat = new Mat(InfraredMapHiehgt, InfraredMapWidth, CvType.CV_8UC3);//img행렬 선언

        Mat dstMat = new Mat(InfraredMapHiehgt, InfraredMapWidth, CvType.CV_8UC1);//1차원 행렬 선언

        Mat nowMat = new Mat(InfraredMapHiehgt, InfraredMapWidth, CvType.CV_8UC1);//1차원 행렬 선언

        Utils.texture2DToMat(infraredTexture, imgMat);//infraredTexture값(적외선 반영된) 행렬 대입

        Mat grayMat = new Mat(InfraredMapHiehgt, InfraredMapWidth, CvType.CV_8UC1);//1채널 영상 선언
       
        //색변경
        Imgproc.cvtColor(imgMat, grayMat, Imgproc.COLOR_RGB2GRAY);//컬러영상 0-> 흑백 영상으로 

        Mat kernel = new Mat(7, 7, CvType.CV_8U, new Scalar(1));

        //convert32Mat을 0으로 초기화 한다.
        convert32Mat = Mat.zeros(InfraredMapHiehgt, InfraredMapWidth, CvType.CV_32FC1);

        nowMat = grayMat;

        dstMat = grayMat;

        //배경을 합산하여 누적 시키기 위해 더 큰 값을 가질 수 있는 float타입으로 컨버트해서
        //convert32Mat에 저장한다. 이렇게 하지 않으면 값의 범위를 초과해서 제대로 나오지 않는다.
        dstMat.convertTo(convert32Mat, CvType.CV_32FC1);

        //matBuffer에 누적시킨다.
        matBuffer.Push(convert32Mat);
        if (sumCount < MAT_BUFFER_SIZE)//MAX_BUFFER_SIZE횟수만큼 전까지는 평균값 구하지 않음
        {
            sumCount += 1;
        }
        else//횟수 채웠으니 평균값 구하기
        {
            //sumMat을 0으로 초기화 한다.
            sumMat = Mat.zeros(InfraredMapHiehgt, InfraredMapWidth, CvType.CV_32FC1);// 합영상 
            for (int i = 0; i < MAT_BUFFER_SIZE - 1; i++)
            {
                Core.add(sumMat, matBuffer.getValue(i), sumMat);//합영상 구하기. 
            }

            //합산한 영상을 합산한 횟수만큼 나누어 평균을 구한다.
            Core.divide(sumMat, avgMat, sumMat);

        }

        //convert8Mat을 0으로 초기화한다.
        convert8Mat = Mat.zeros(InfraredMapHiehgt, InfraredMapWidth, CvType.CV_8UC1);

        //sumMat을 다시 ushort타입으로 변환한다.
        sumMat.convertTo(convert8Mat, CvType.CV_8UC1);

        Mat resultMat = new Mat(InfraredMapHiehgt, InfraredMapWidth, CvType.CV_8UC1);//1차원 행렬 선언

        //평균으로 구해진 영상과 현재 영상을 빼준다.
        //subtract으로 빼주는 이유는 마이너스 값을 구분하기 위함이다.
        Core.subtract(convert8Mat, nowMat, resultMat);

        Imgproc.erode(resultMat, resultMat, kernel);//병목
        Imgproc.dilate(resultMat, resultMat, kernel);//팽창. 
        Imgproc.dilate(resultMat, resultMat, kernel);//팽창. 
        Imgproc.dilate(resultMat, resultMat, kernel);//팽창. 
        Imgproc.dilate(resultMat, resultMat, kernel);//팽창. 


        //touchPoint로 검출된 좌표를 저장할 리스트
        List<Point> touchPoints = new List<Point>();

        Mat hierarchy = new Mat();
        //윤곽이 저장될 리스트
        List<MatOfPoint> contours = new List<MatOfPoint>();
        //윤곽을 구한다!
        Imgproc.findContours(resultMat, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

        for (int i = 0; i < contours.Count; i++)
        {
            Mat contourMat = new Mat();
            contourMat = contours[i];
            //윤곽의 크기를 검출한다.
            double contourArea = Imgproc.contourArea(contourMat);
            //검출된 윤곽의 크기가 내가 설정한 Min, Max 값 안에 들어간다면...
            if (contourArea > footMinArea
                && contourArea < footMaxArea
                )
            {
                Scalar center = Core.mean(contourMat);
                Point footPoint = new Point(center.val[0], center.val[1]);
                //touchPoints 리스트에 추가한다.
                touchPoints.Add(footPoint);
            }
            //아래는 확인용이다.
            //Scalar color = new Scalar(255, 0, 0);
            //Imgproc.drawContours(resultMat, contours, i, color, 7);
        }

        for (int i = 0; i < touchPoints.Count; i++)
        { 
            //확인용
            //Scalar color = new Scalar(255, 0, 0);
            //Imgproc.circle(resultMat, touchPoints[i], 50, color, 7);
            
            //좌표값을 받아서 파티클을 활성화시킨다.
            int delta_x = (int)touchPoints[i].x;
            int delta_y = (int)touchPoints[i].y;
            OnParticleObjActive(delta_x, delta_y);
        }

        //현재 활성화중인 ParticleObj의 lifeTime을 확인하기 위해!
        OnParticleObjTimeCheck();

        //확인용
        //Utils.matToTexture2D(resultMat, resultTexture);//원본 깊이값 영상 텍스쳐로 전환
        //resultTexture.Apply();//텍스쳐 적용

    }



}

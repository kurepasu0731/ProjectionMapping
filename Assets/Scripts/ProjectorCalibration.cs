using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Windows.Kinect;

public class ProjectorCalibration : MonoBehaviour {

    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void openWindow(string windowName);
    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void closeWindow(string windowName);
    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void showWindow(string windowName, System.IntPtr data, int width, int height);
    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void showKinectWindow(string windowName, System.IntPtr data, int width, int height);
    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void fullWindow(string windowName, int displayNum, System.IntPtr data, int width, int height);
    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void whiteFullWindow(string windowName, int displayNum);
    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void loadFullWindow(string windowName, string filePath, int displayNum);
    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void saveTexture(string filePath, System.IntPtr data, int width, int height);
    [DllImport("GrayCode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void makeGraycodeImage(int proWidth, int proHeight);
    [DllImport("GrayCode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void make_thresh(int proWidth, int proHeight, int camWidth, int camHeight);
    [DllImport("GrayCode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void makeCorrespondence(int proWidth, int proHeight, int camWidth, int camHeight, int[] proPointx, int[] proPointy, int[] camPointx, int[] camPointy, ref int correspondNum);
    [DllImport("projectorCalibration", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void calcCalibration(double[] srcPoint2dx, double[] srcPoint2dy, double[] srcPoint3dx, double[] srcPoint3dy, double[] srcPoint3dz, int srcCorrespond, int proWidth, int proHeight, double[] projectionMatrix, double[] externalMatrix, ref double reprojectionResult);
    [DllImport("projectorCalibration", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void loadPerspectiveMatrix(double[] projectionMatrix, double[] externalMatrix);


    public Camera myProjector;
    public int proWidth = 1680;
    public int proHeight = 1050;
    public int camWidth = 1920;
    public int camHeight = 1080;
    public int displayNum = 1;
    private bool calib_flag = false;

    private Texture2D tex;
    private Color32[] texturePixels_;
    private GCHandle texturePixelsHandle_;
    private IntPtr texturePixelsPtr_;

    private float timer = 0;
    private float lastTimer = 0;

    // GrayCode関連
    struct Graycode
    {
        public int[,] graycode;
        public int h_bit, w_bit;
        public int all_bit;
    }
    private Graycode g;
    private string[] Filename_posi;
    private string[] Filename_nega;
    private string graycodeWindow = "graycode";
    private string checkWindow = "check";
    private int posi_count = 0;
    private int nega_count = 0;


    // Kinect関連
    public GameObject multiSourceManager;
    MultiSourceManager multiSourceManagerScript;
    FrameDescription depthFrameDesc;
    CoordinateMapper mapper;


    // 内部パラメータと外部パラメータ
    public Matrix4x4 originalProjection;
    public Matrix4x4 originalExternal;


	// Use this for initialization
	void Start () {
        myProjector = GetComponent<Camera>();

        tex = new Texture2D(proWidth, proHeight, TextureFormat.ARGB32, false);


        // buffer for points mapped to camera space coordinate.
        mapper = KinectSensor.GetDefault().CoordinateMapper;

        // get reference to DepthSourceManager (which is included in the distributed 'Kinect for Windows v2 Unity Plugin zip')
        multiSourceManagerScript = multiSourceManager.GetComponent<MultiSourceManager>();
	}
	
	// Update is called once per frame
	void Update () {

        timer += Time.deltaTime;


        if (calib_flag && (timer - lastTimer) > 0.6f)
        {
            projectorCalibration();

            lastTimer = timer;
        }

        // キャリブレーションの開始
        if (Input.GetKeyDown(KeyCode.C))
        {
            callCalibration(proWidth, proHeight, displayNum);
        }


        // プロジェクタパラメータの読み込み
        if (Input.GetKeyDown(KeyCode.L))
        {
            loadParam();
        }

        // プロジェクタパラメータの更新
        if (Input.GetKeyDown(KeyCode.N))
        {
           // set projection matrix
            myProjector.projectionMatrix = originalProjection;

            // set external matrix
            myProjector.worldToCameraMatrix = originalExternal;
        }
	}


    void OnApplicationQuit()
    {
        closeWindow(checkWindow);
        closeWindow(graycodeWindow);
    }



    // ビット数の計算とグレイコードの作成
    void initGraycode()
    {
        g.graycode = new int[proHeight,proWidth];
        g.h_bit = (int)Math.Ceiling(Math.Log(proHeight + 1) / Math.Log(2));
        g.w_bit = (int)Math.Ceiling(Math.Log(proWidth + 1) / Math.Log(2));
        g.all_bit = g.h_bit + g.w_bit;
        if (!Directory.Exists("Calibration"))
        {
            Directory.CreateDirectory("Calibration");
        }
        if (!Directory.Exists("Calibration/GrayCodeImage"))
        {
            Directory.CreateDirectory("Calibration/GrayCodeImage");
        }
        if (!Directory.Exists("Calibration/GrayCodeImage/CaptureImage"))
        {
            Directory.CreateDirectory("Calibration/GrayCodeImage/CaptureImage");
        }
        if (!Directory.Exists("Calibration/GrayCodeImage/ProjectionGrayCode"))
        {
            Directory.CreateDirectory("Calibration/GrayCodeImage/ProjectionGrayCode");
        }
        if (!Directory.Exists("Calibration/GrayCodeImage/ThresholdImage"))
        {
            Directory.CreateDirectory("Calibration/GrayCodeImage/ThresholdImage");
        }
        


        int[] bin_code_h = new int[proHeight];  // 2進コード（縦）
	    int[] bin_code_w = new int[proWidth];   // 2進コード（横）
	    int[] graycode_h = new int[proHeight];  // グレイコード（縦）
	    int[] graycode_w = new int[proWidth];   // グレイコード（横）

        /***** 2進コード作成 *****/
        // 行について
        for (int y = 0; y < proHeight; y++)
            bin_code_h[y] = y + 1;
        // 列について
        for (int x = 0; x < proWidth; x++)
            bin_code_w[x] = x + 1;

        /***** グレイコード作成 *****/
        // 行について
        for (int y = 0; y < proHeight; y++)
            graycode_h[y] = bin_code_h[y] ^ (bin_code_h[y] >> 1);
        // 列について
        for (int x = 0; x < proWidth; x++)
            graycode_w[x] = bin_code_w[x] ^ (bin_code_w[x] >> 1);
        // 行列を合わせる（行 + 列）
        for (int y = 0; y < proHeight; y++)
        {
            for (int x = 0; x < proWidth; x++)
                g.graycode[y,x] = (graycode_h[y] << g.w_bit) | graycode_w[x];
        }
    }


    // グレイコードのための準備
    void readyGraycode()
    {
        initGraycode();

        // 書式付入出力（グレイコード読み込み用）
        Filename_posi = new string[g.all_bit];
        Filename_nega = new string[g.all_bit];

        // 連番でファイル名を読み込む
        print("投影用グレイコード画像読み込み中");
        for (uint i = 0; i < g.all_bit; i++)
        {
            Filename_posi[i] = "Calibration/GrayCodeImage/ProjectionGrayCode/posi" + i.ToString().PadLeft(2, '0') + ".bmp";
            Filename_nega[i] = "Calibration/GrayCodeImage/ProjectionGrayCode/nega" + i.ToString().PadLeft(2, '0') + ".bmp";

            if (!File.Exists(Filename_posi[i]) || !File.Exists(Filename_nega[i]))
            {
                print("グレイコード画像を作成します。");
                makeGraycodeImage(proWidth, proHeight);

                for (uint j = 0; j < g.all_bit; j++)
                {
                    Filename_posi[j] = "Calibration/GrayCodeImage/ProjectionGrayCode/posi" + j.ToString().PadLeft(2, '0') + ".bmp";
                    Filename_nega[j] = "Calibration/GrayCodeImage/ProjectionGrayCode/nega" + j.ToString().PadLeft(2, '0') + ".bmp";
                }
                break;
            }
        }

        print("グレイコード投影開始");
        openWindow(graycodeWindow);
        loadFullWindow(graycodeWindow, Filename_posi[0], displayNum);

        lastTimer = timer;
    }


    // プロジェクタキャリブレーション
    void projectorCalibration()
    {
        // Kinectからカラーを取得
        tex = multiSourceManagerScript.GetColorTexture();

        // Convert texture to ptr
        texturePixels_ = tex.GetPixels32();
        texturePixelsHandle_ = GCHandle.Alloc(texturePixels_, GCHandleType.Pinned);
        texturePixelsPtr_ = texturePixelsHandle_.AddrOfPinnedObject();

        showKinectWindow(checkWindow, texturePixelsPtr_, multiSourceManagerScript.ColorWidth, multiSourceManagerScript.ColorHeight);

        string[] Filename_posi_cam = new string[g.all_bit];
        string[] Filename_nega_cam = new string[g.all_bit];
        // ポジパターン投影 & 撮影
        if (posi_count < g.all_bit)
        {
            // ポジパターン撮影結果を保存
            // 横縞
            if (posi_count < g.h_bit)
                Filename_posi_cam[posi_count] = "Calibration/GrayCodeImage/CaptureImage/CameraImg" + 0 + "_" + (posi_count + 1).ToString().PadLeft(2, '0') + "_" + 1 + ".bmp";
            // 縦縞
            else
                Filename_posi_cam[posi_count] = "Calibration/GrayCodeImage/CaptureImage/CameraImg" + 1 + "_" + (posi_count - g.h_bit + 1).ToString().PadLeft(2, '0') + "_" + 1 + ".bmp";
            // 保存
            saveTexture(Filename_posi_cam[posi_count], texturePixelsPtr_, multiSourceManagerScript.ColorWidth, multiSourceManagerScript.ColorHeight);

            posi_count++;

            // 投影
            if (posi_count < g.all_bit)
            {
                loadFullWindow(graycodeWindow, Filename_posi[posi_count], displayNum);
            }
            else
            {
                loadFullWindow(graycodeWindow, Filename_nega[0], displayNum);
            }
        }

        // ネガパターン投影 & 撮影
        else if (posi_count >= g.all_bit && nega_count < g.all_bit)
        {
            // ネガパターン撮影結果を保存
            // 横縞
            if (nega_count < g.h_bit)
                Filename_nega_cam[nega_count] = "Calibration/GrayCodeImage/CaptureImage/CameraImg" + 0 + "_" + (nega_count + 1).ToString().PadLeft(2, '0') + "_" + 0 + ".bmp";
            // 縦縞
            else
                Filename_nega_cam[nega_count] = "Calibration/GrayCodeImage/CaptureImage/CameraImg" + 1 + "_" + (nega_count - g.h_bit + 1).ToString().PadLeft(2, '0') + "_" + 0 + ".bmp";
            // 保存
            saveTexture(Filename_nega_cam[nega_count], texturePixelsPtr_, multiSourceManagerScript.ColorWidth, multiSourceManagerScript.ColorHeight);

            nega_count++;

            // 投影
            if (nega_count < g.all_bit)
            {
                loadFullWindow(graycodeWindow, Filename_nega[nega_count], displayNum);
            }
            else
            {
                // 投影終了
                calib_flag = false;

                closeWindow(graycodeWindow);
                closeWindow(checkWindow);
                print("グレイコード投影終了");

                make_thresh(proWidth, proHeight, camWidth, camHeight);
                print("2値化終了");

                int[] proPointx = new int[proWidth * proHeight];
                int[] proPointy = new int[proWidth * proHeight];
                int[] camPointx = new int[proWidth * proHeight];
                int[] camPointy = new int[proWidth * proHeight];
                int correspondNum = 0;
                makeCorrespondence(proWidth, proHeight, camWidth, camHeight, proPointx, proPointy, camPointx, camPointy, ref correspondNum);
                print("グレイコード終了");

                List<double> Point2dx = new List<double>();
                List<double> Point2dy = new List<double>();
                List<double> Point3dx = new List<double>();
                List<double> Point3dy = new List<double>();
                List<double> Point3dz = new List<double>();
                int correspoindNum2 = 0;

                // Kinectのカラー空間をカメラ空間へ変換
                calcColorPointToWorld(multiSourceManagerScript.GetDepthData(), multiSourceManagerScript.ColorWidth, multiSourceManagerScript.ColorHeight,
                                        camPointx, camPointy, proPointx, proPointy, correspondNum, ref Point2dx, ref Point2dy, ref Point3dx, ref Point3dy, ref Point3dz, ref correspoindNum2);

                double[] proPoint2dx = new double[correspoindNum2];
                double[] proPoint2dy = new double[correspoindNum2];
                double[] camPoint3dx = new double[correspoindNum2];
                double[] camPoint3dy = new double[correspoindNum2];
                double[] camPoint3dz = new double[correspoindNum2];
                proPoint2dx = Point2dx.ToArray();
                proPoint2dy = Point2dy.ToArray();
                camPoint3dx = Point3dx.ToArray();
                camPoint3dy = Point3dy.ToArray();
                camPoint3dz = Point3dz.ToArray();
  
                print("対応点数："+correspoindNum2);


                double[] cameraMat = new double[16];
                double[] externalMat = new double[16];
                double result = 0;

                // プロジェクタキャリブレーション
                print("キャリブレーション実行");
                calcCalibration(proPoint2dx, proPoint2dy, camPoint3dx, camPoint3dy, camPoint3dz, correspoindNum2, proWidth, proHeight, cameraMat, externalMat, ref result);

                print("再投影誤差："+result);


                // 透視投影変換行列を掛ける
                originalProjection.m00 = (float)cameraMat[0];
                originalProjection.m01 = (float)cameraMat[1];
                originalProjection.m02 = (float)cameraMat[2];
                originalProjection.m03 = (float)cameraMat[3];
                originalProjection.m10 = (float)cameraMat[4];
                originalProjection.m11 = (float)cameraMat[5];
                originalProjection.m12 = (float)cameraMat[6];
                originalProjection.m13 = (float)cameraMat[7];
                originalProjection.m20 = (float)cameraMat[8];
                originalProjection.m21 = (float)cameraMat[9];
                originalProjection.m22 = (float)cameraMat[10];
                originalProjection.m23 = (float)cameraMat[11];
                originalProjection.m30 = (float)cameraMat[12];
                originalProjection.m31 = (float)cameraMat[13];
                originalProjection.m32 = (float)cameraMat[14];
                originalProjection.m33 = (float)cameraMat[15];

                // set projection matrix
                myProjector.projectionMatrix = originalProjection;

                originalExternal.m00 = (float)externalMat[0];
                originalExternal.m01 = (float)externalMat[1];
                originalExternal.m02 = (float)externalMat[2];
                originalExternal.m03 = (float)externalMat[3];
                originalExternal.m10 = (float)externalMat[4];
                originalExternal.m11 = (float)externalMat[5];
                originalExternal.m12 = (float)externalMat[6];
                originalExternal.m13 = (float)externalMat[7];
                originalExternal.m20 = (float)externalMat[8];
                originalExternal.m21 = (float)externalMat[9];
                originalExternal.m22 = (float)externalMat[10];
                originalExternal.m23 = (float)externalMat[11];
                originalExternal.m30 = (float)externalMat[12];
                originalExternal.m31 = (float)externalMat[13];
                originalExternal.m32 = (float)externalMat[14];
                originalExternal.m33 = (float)externalMat[15];

                // set external matrix
                myProjector.worldToCameraMatrix = originalExternal;


                posi_count = 0;
                nega_count = 0;
            }
        }

        texturePixelsHandle_.Free();
    }


    // 3次元座標の取得
    void calcColorPointToWorld(ushort[] depthData, int colorWidth, int colorHeight, int[] srcPointx, int[] srcPointy, int[] proPointx, int[] proPointy, int srcLen, 
                        ref List<double> dstPoint2dx, ref List<double> dstPoint2dy, ref List<double> dstPoint3dx, ref List<double> dstPoint3dy, ref List<double> dstPoint3dz, ref int dstLen)
    {
        var frameDesc = KinectSensor.GetDefault().DepthFrameSource.FrameDescription;

        // depthをcolorに合わせる
        DepthSpacePoint[] depthSpace = new DepthSpacePoint[colorWidth * colorHeight];
        mapper.MapColorFrameToDepthSpace(depthData, depthSpace);

        // depthをcameraに合わせる
        CameraSpacePoint[] cameraSpace = new CameraSpacePoint[depthData.Length];
        mapper.MapDepthFrameToCameraSpace(depthData, cameraSpace);


        dstPoint2dx.Clear();
        dstPoint2dy.Clear();
        dstPoint3dx.Clear();
        dstPoint3dy.Clear();
        dstPoint3dz.Clear();

        dstLen = 0;

        for (int i = 0; i < srcLen; ++i)
        {
            int src_index = srcPointy[i] * multiSourceManagerScript.ColorWidth + srcPointx[i];
            int dst_x = (int)(depthSpace[src_index].X);
            int dst_y = (int)(depthSpace[src_index].Y);
            int dst_index = dst_y * frameDesc.Width + dst_x;

            if( ((0<=dst_x) && (dst_x < frameDesc.Width)) && ((0<=dst_y) && (dst_y<frameDesc.Height)) )
            {
                if( (0.5f<=cameraSpace[dst_index].Z) && (cameraSpace[dst_index].Z<=8.0f) )
                {
                    dstPoint2dx.Add((double)(proPointx[i]));
                    dstPoint2dy.Add((double)(proPointy[i]));
                    dstPoint3dx.Add(-cameraSpace[dst_index].X);
                    dstPoint3dy.Add(cameraSpace[dst_index].Y);
                    dstPoint3dz.Add(cameraSpace[dst_index].Z);

                    dstLen++;
                }
            }
        }
    }


    // 1点の3次元座標の取得
    void calcColorPointToWorld(ushort[] depthData, int colorWidth, int colorHeight, List<int> srcPoint, ref List<double> dstPoint)
    {
        var frameDesc = KinectSensor.GetDefault().DepthFrameSource.FrameDescription;

        // depthをcolorに合わせる
        DepthSpacePoint[] depthSpace = new DepthSpacePoint[colorWidth * colorHeight];
        mapper.MapColorFrameToDepthSpace(depthData, depthSpace);

        // depthをcameraに合わせる
        CameraSpacePoint[] cameraSpace = new CameraSpacePoint[depthData.Length];
        mapper.MapDepthFrameToCameraSpace(depthData, cameraSpace);


        dstPoint.Clear();

        int src_index = srcPoint[1] * multiSourceManagerScript.ColorWidth + srcPoint[0];
        int dst_x = (int)(depthSpace[src_index].X);
        int dst_y = (int)(depthSpace[src_index].Y);
        int dst_index = dst_y * frameDesc.Width + dst_x;

        if (((0 <= dst_x) && (dst_x < frameDesc.Width)) && ((0 <= dst_y) && (dst_y < frameDesc.Height)))
        {
            if ((0.5f <= cameraSpace[dst_index].Z) && (cameraSpace[dst_index].Z <= 8.0f))
            {
                dstPoint.Add(-cameraSpace[dst_index].X);
                dstPoint.Add(cameraSpace[dst_index].Y);
                dstPoint.Add(cameraSpace[dst_index].Z);
            }
        }
    }


    // キャリブレーションの呼び出し
    public void callCalibration(int width, int height, int num)
    {
        proWidth = width;
        proHeight = height;
        displayNum = num;

        openWindow(checkWindow);

        // グレイコードのための準備
        readyGraycode();

        calib_flag = true;
    }


    // パラメータの読み込み
    public void loadParam()
    {
        double[] cameraMat = new double[16];
        double[] externalMat = new double[16];

        loadPerspectiveMatrix(cameraMat, externalMat);

        // 透視投影変換行列を掛ける
        originalProjection.m00 = (float)cameraMat[0];
        originalProjection.m01 = (float)cameraMat[1];
        originalProjection.m02 = (float)cameraMat[2];
        originalProjection.m03 = (float)cameraMat[3];
        originalProjection.m10 = (float)cameraMat[4];
        originalProjection.m11 = (float)cameraMat[5];
        originalProjection.m12 = (float)cameraMat[6];
        originalProjection.m13 = (float)cameraMat[7];
        originalProjection.m20 = (float)cameraMat[8];
        originalProjection.m21 = (float)cameraMat[9];
        originalProjection.m22 = (float)cameraMat[10];
        originalProjection.m23 = (float)cameraMat[11];
        originalProjection.m30 = (float)cameraMat[12];
        originalProjection.m31 = (float)cameraMat[13];
        originalProjection.m32 = (float)cameraMat[14];
        originalProjection.m33 = (float)cameraMat[15];

        // set projection matrix
        myProjector.projectionMatrix = originalProjection;

        originalExternal.m00 = (float)externalMat[0];
        originalExternal.m01 = (float)externalMat[1];
        originalExternal.m02 = (float)externalMat[2];
        originalExternal.m03 = (float)externalMat[3];
        originalExternal.m10 = (float)externalMat[4];
        originalExternal.m11 = (float)externalMat[5];
        originalExternal.m12 = (float)externalMat[6];
        originalExternal.m13 = (float)externalMat[7];
        originalExternal.m20 = (float)externalMat[8];
        originalExternal.m21 = (float)externalMat[9];
        originalExternal.m22 = (float)externalMat[10];
        originalExternal.m23 = (float)externalMat[11];
        originalExternal.m30 = (float)externalMat[12];
        originalExternal.m31 = (float)externalMat[13];
        originalExternal.m32 = (float)externalMat[14];
        originalExternal.m33 = (float)externalMat[15];

        // set external matrix
        myProjector.worldToCameraMatrix = originalExternal;
    }
}

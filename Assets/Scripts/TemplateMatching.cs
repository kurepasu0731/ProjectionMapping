using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using Windows.Kinect;

public class TemplateMatching : MonoBehaviour {

    [DllImport("TemplateMatching", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void templateMatching(System.IntPtr data, int width, int height, int templatenumber, double thresh, ref int x, ref int y);

    [DllImport("TemplateMatching", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void openWindow(string windowName);

    [DllImport("TemplateMatching", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void closeWindow(string windowName);

    public GameObject infraredSourceManager;
    public GameObject multiSourceManager;
    public int templatenumber;
    public double thresh;
    public double offset; //ブロックの表面から中心までのオフセット(m)

    MultiSourceManager multiSourceManagerScript;
    InfraredSourceManager infraredSourceManagerScript;

    FrameDescription depthFrameDesc;
    FrameDescription infraredFrameDesc;
    CameraSpacePoint[] cameraSpacePoints;
    CoordinateMapper mapper;

    private int depthWidth;
    private int depthHeight;

    private Texture2D tex;
    private Color32[] texturePixels_;
    private GCHandle texturePixelsHandle_;
    private IntPtr texturePixelsPtr_;

    private int x;
    private int y;

	// Use this for initialization
	void Start () {

        // Get the description of the depth frames.
        infraredFrameDesc = KinectSensor.GetDefault().InfraredFrameSource.FrameDescription;
        depthFrameDesc = KinectSensor.GetDefault().DepthFrameSource.FrameDescription;
        depthWidth = depthFrameDesc.Width;
        depthHeight = depthFrameDesc.Height;

        // buffer for points mapped to camera space coordinate.
        cameraSpacePoints = new CameraSpacePoint[depthWidth * depthHeight];
        mapper = KinectSensor.GetDefault().CoordinateMapper;

        // get reference to infraredSourceManager (which is included in the distributed 'Kinect for Windows v2 Unity Plugin zip')
        multiSourceManagerScript = multiSourceManager.GetComponent<MultiSourceManager>();
        infraredSourceManagerScript = infraredSourceManager.GetComponent<InfraredSourceManager>();

        //最初はてきとーな場所に配置
        transform.position = new Vector3(0.0f, 10.0f, 10.0f);

	}
	
	// Update is called once per frame
	void Update () {
        // get new depth data from DepthSourceManager.
        ushort[] rawdata = multiSourceManagerScript.GetDepthData();
        // map to camera space coordinate
        mapper.MapDepthFrameToCameraSpace(rawdata, cameraSpacePoints);

        // Kinectからカラーを取得
        tex = infraredSourceManagerScript.GetInfraredTexture();

        // Convert texture to ptr
        texturePixels_ = tex.GetPixels32();
        texturePixelsHandle_ = GCHandle.Alloc(texturePixels_, GCHandleType.Pinned);
        texturePixelsPtr_ = texturePixelsHandle_.AddrOfPinnedObject();

        //テンプレートマッチング
        templateMatching(texturePixelsPtr_, depthWidth, depthHeight, templatenumber, thresh, ref x, ref y);

        print("(x, y): (" + x + ", " + y + ")");

        if (x != -1 && y != -1) //thresh以上の値でマッチングしていたら
        {

            //GameObjectの3次元位置を求める
            //DepthSpacePoint matching_result = new DepthSpacePoint();
            //matching_result.X = (float)x;
            //matching_result.Y = (float)y;
            //ushort depth = rawdata[y * depthWidth + x];

            //CameraSpacePoint matchingPoint = mapper.MapDepthPointToCameraSpace(matching_result, depth);

            CameraSpacePoint matchingPoint = cameraSpacePoints[y * depthWidth + x];

            if (!float.IsInfinity(matchingPoint.X) && !float.IsInfinity(matchingPoint.Y) && !float.IsInfinity(matchingPoint.Z))
            {
                //移動(X軸逆)
                transform.position = new Vector3(-matchingPoint.X, matchingPoint.Y, (float)(matchingPoint.Z + offset));
            }
        }
        else
        {
            //てきとーな場所にとばす
            transform.position = new Vector3(0.0f, 10.0f, 10.0f);

        }
    }

    void OnApplicationQuit()
    {
        closeWindow("search image");
        closeWindow("result image");
    }
}

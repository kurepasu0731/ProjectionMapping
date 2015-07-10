using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;


public class TemplateMatchingManager : MonoBehaviour {

    [DllImport("TemplateMatchingManager", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void templateMatchingManager(System.IntPtr ir_data, ushort[] depth_data, TemplateLabel[] Label, int labelNum, int desk_pos_y, int desk_pos_z);

    [DllImport("TemplateMatchingManager", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void openWindow(string windowName);

    [DllImport("TemplateMatchingManager", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void closeWindow(string windowName);

    [DllImport("BackgroundDifference", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void backGroundDifference(ushort[] data1, System.IntPtr data2, int width, int height, int noise, int humanNoise, System.IntPtr data3);

    public GameObject infraredSourceManager;
    public GameObject multiSourceManager;
    public int blocknum;//ブロックの数=ラベルの数
    public TemplateLabel[] blocks;//各ブロックの情報
    //机の位置
    public int desk_pos_y;
    public int desk_pos_z;
    //背景差分パラメータ
    //public int noise;
    //public int humanNoise;

    MultiSourceManager multiSourceManagerScript;
    InfraredSourceManager infraredSourceManagerScript;

    //IR画像取得分
    private Texture2D tex;
    private Color32[] texturePixels_;
    private GCHandle texturePixelsHandle_;
    private IntPtr texturePixelsPtr_;


    //背景差分後のデータ
    private IntPtr texturePixelsPtr2_;

	// Use this for initialization
	void Start () {
        //配列初期化
        blocks = new TemplateLabel[blocknum];

        // get reference to infraredSourceManager (which is included in the distributed 'Kinect for Windows v2 Unity Plugin zip')
        multiSourceManagerScript = multiSourceManager.GetComponent<MultiSourceManager>();
        infraredSourceManagerScript = infraredSourceManager.GetComponent<InfraredSourceManager>();

	}
	
	// Update is called once per frame
	void Update () {
        //blocksの中身入れる

	    //IR背景差分
        //↓
        //テンプレートマッチング

        // KinectからIR画像を取得
        tex = infraredSourceManagerScript.GetInfraredTexture();

        // Convert texture to ptr
        texturePixels_ = tex.GetPixels32();
        texturePixelsHandle_ = GCHandle.Alloc(texturePixels_, GCHandleType.Pinned);
        texturePixelsPtr_ = texturePixelsHandle_.AddrOfPinnedObject();

        // get new depth data from DepthSourceManager.
        ushort[] rawdata = multiSourceManagerScript.GetDepthData();
        int depthWidth = multiSourceManagerScript.GetdepthWidth();
        int depthHeight = multiSourceManagerScript.GetdepthHeight();


        //背景差分
        //backGroundDifference(rawdata, texturePixelsPtr_, depthWidth, depthHeight, noise, humanNoise, texturePixelsPtr2_);

        //テンプレートマッチング
        //templateMatchingManager(texturePixelsPtr2_, rawdata, blocks, blocknum, desk_pos_y, desk_pos_z);
        templateMatchingManager(texturePixelsPtr_, rawdata, blocks, blocknum, desk_pos_y, desk_pos_z);
    }

    void OnApplicationQuit()
    {
        closeWindow("search image");
        closeWindow("result image");
    }
}

public struct TemplateLabel
{
	public int templateNum;		// テンプレート番号(0は割り振られていない)
	public int checkNum;			// 発見回数
	public bool detect_flag;		// 検出確認(5回連続で検出されたらtrue)
	public int objectID;			// オブジェクト毎に割り振るID

	// 発見が1回以上
	public int detect_x;			// 発見したx座標
	public int detect_y;			// 発見したy座標

	// 検出時
	//cv::Mat depthRect;		// 検出時の深度値
	
};


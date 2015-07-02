using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.InteropServices;


public class ProjectionWindow : MonoBehaviour {

    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void openWindow(string windowName);
    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void closeWindow(string windowName);
    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void showWindow(string windowName, System.IntPtr data, int width, int height);
    [DllImport("multiWindow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void fullWindow(string windowName, int displayNum, System.IntPtr data, int width, int height);


    public Camera myProjector;
    public int proWidth = 1680;
    public int proHeight = 1050;
    private string windowName = "Projection";
    public int displayNum = 1;

    private Texture2D tex;

    private Color32[] texturePixels_;
    private GCHandle texturePixelsHandle_;
    private IntPtr texturePixelsPtr_;

    private bool projection_flag = false;

	// Use this for initialization
    void Start()
    {
        myProjector = GetComponent<Camera>();

        tex = new Texture2D(proWidth, proHeight, TextureFormat.ARGB32, false);
    }
	
	// Update is called once per frame
	void Update () {

        // 投影
        if (projection_flag)
        {

            // off-screen rendering
            var camtex = RenderTexture.GetTemporary(proWidth, proHeight, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1);
            myProjector.targetTexture = camtex;
            myProjector.Render();
            RenderTexture.active = camtex;

            tex.ReadPixels(new Rect(0, 0, camtex.width, camtex.height), 0, 0);
            tex.Apply();

            // Convert texture to ptr
            texturePixels_ = tex.GetPixels32();
            texturePixelsHandle_ = GCHandle.Alloc(texturePixels_, GCHandleType.Pinned);
            texturePixelsPtr_ = texturePixelsHandle_.AddrOfPinnedObject();

            // Show a window
            fullWindow(windowName, displayNum, texturePixelsPtr_, proWidth, proHeight);

            texturePixelsHandle_.Free();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(camtex);
            myProjector.targetTexture = null;
        }
	}

    public void callProjection(int width, int height, int num)
    {
        proWidth = width;
        proHeight = height;
        displayNum = num;

        projection_flag = !projection_flag;

        if (projection_flag)
        {
            closeWindow(windowName);
            openWindow(windowName);
        }
        else
        {
            closeWindow(windowName);
        }
    }


    void OnApplicationQuit()
    {
        closeWindow(windowName);
    }
}

using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.InteropServices;

public class multiWindow : MonoBehaviour {

	[DllImport("multiWindow", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
	static extern void openWindow(string windowName);
	[DllImport("multiWindow", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
	static extern void closeWindow (string windowName);
	[DllImport("multiWindow", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
	static extern void showWindow (string windowName, System.IntPtr data, int width, int height);
	[DllImport("multiWindow", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
	static extern void fullWindow (string windowName, int displayNum, System.IntPtr data, int width, int height);

	public Camera myCamera;
	public int camWidth = 1024;
	public int camHeight = 768;
	public string windowName = "myWindow";
	public int displayNum = 0;

	private Texture2D tex;

	private Color32[] texturePixels_;
	private GCHandle  texturePixelsHandle_;
	private IntPtr    texturePixelsPtr_;

	// Use this for initialization
	void Start () {
		myCamera = GetComponent<Camera>();

		tex = new Texture2D (camWidth, camHeight, TextureFormat.ARGB32, false);

		// Create a window
		openWindow (windowName);
	}
	
	// Update is called once per frame
	void Update () {

		// off-screen rendering
		var camtex = RenderTexture.GetTemporary (camWidth, camHeight, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1);
		myCamera.targetTexture = camtex;
		myCamera.Render ();
		RenderTexture.active = camtex;
		
		tex.ReadPixels (new Rect (0, 0, camtex.width, camtex.height), 0, 0);
		tex.Apply ();

		// Convert texture to ptr
		texturePixels_       = tex.GetPixels32();
		texturePixelsHandle_ = GCHandle.Alloc(texturePixels_, GCHandleType.Pinned);
		texturePixelsPtr_    = texturePixelsHandle_.AddrOfPinnedObject();

		// Show a window
		fullWindow (windowName, displayNum, texturePixelsPtr_, camWidth, camHeight);

		texturePixelsHandle_.Free();

		RenderTexture.active = null;
		RenderTexture.ReleaseTemporary (camtex);
		myCamera.targetTexture = null;
	}

	void OnApplicationQuit()
	{
		closeWindow(windowName);
	}
}

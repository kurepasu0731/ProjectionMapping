using UnityEngine;
using System.Collections;

public class gui : MonoBehaviour {

    public ProjectionWindow window;
    public ProjectorCalibration calibration;

    private string width = "1680";
    private string height = "1050";
    private string num = "1";

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnGUI()
    {

        if (GUI.Button(new Rect(20, 30, 150, 20), "キャリブレーション"))
        {
            calibration.callCalibration(int.Parse(width), int.Parse(height), int.Parse(num));
        }
        if (GUI.Button(new Rect(20, 50, 150, 20), "パラメータ読み込み"))
        {
            calibration.loadParam();
        }
        if (GUI.Button(new Rect(20, 70, 150, 20), "投影"))
        {
            window.callProjection(int.Parse(width), int.Parse(height), int.Parse(num));     // 投影の切り替え
        }


        GUI.TextField(new Rect(20, 100, 100, 20), "Projector width");
        width = GUI.TextField(new Rect(120, 100, 50, 20), width);
        GUI.TextField(new Rect(20, 120, 100, 20), "Projector height");
        height = GUI.TextField(new Rect(120, 120, 50, 20), height);
        GUI.TextField(new Rect(20, 140, 100, 20), "Projector Num");
        num = GUI.TextField(new Rect(120, 140, 50, 20), num);
    }
}

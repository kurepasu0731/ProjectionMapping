using UnityEngine;
using System.Collections;


public class MatchingObjects : MonoBehaviour {

    public GameObject multiSourceManager;
    MultiSourceManager multiSourceManagerScript;

    public GameObject templateMatchingManager;
    TemplateMatchingManager templateMatchingManagerScript;
    //public int templateNum;		// テンプレート番号(0は割り振られていない)
    public int objectID;			// オブジェクト毎に割り振るID

    //スケール
    public float scale;

    //オフセット
    public float Xoffset;
    public float Yoffset;
    public float Zoffset;

    //マッチング結果(IR画面上)
    private int x;
    private int y;


    // Use this for initialization
	void Start () {
        templateMatchingManagerScript = templateMatchingManager.GetComponent<TemplateMatchingManager>();
        multiSourceManagerScript = multiSourceManager.GetComponent<MultiSourceManager>();

        //最初はてきとーな場所に配置
        transform.position = new Vector3(0.0f, 10.0f, 10.0f);

    }
	
	// Update is called once per frame
	void Update () {
        //マッチング結果を取ってくる
        x = templateMatchingManagerScript.blocks[objectID].detect_x;
        y = templateMatchingManagerScript.blocks[objectID].detect_y;

        print("(x, y): (" + x + ", " + y + ")");

        if (x != -1 && y != -1) //thresh以上の値でマッチングしていたら
        {

          //CameraSpacePoint matchingPoint = cameraSpacePoints[y * depthWidth + x];
            int depthWidth = multiSourceManagerScript.GetdepthWidth();
            float X = multiSourceManagerScript.cameraSpacePoints[y * depthWidth + x].X;
            float Y = multiSourceManagerScript.cameraSpacePoints[y * depthWidth + x].Y;
            float Z = multiSourceManagerScript.cameraSpacePoints[y * depthWidth + x].Z;


            if (!float.IsInfinity(X) && !float.IsInfinity(Y) && !float.IsInfinity(Z))
            {
                //移動(X軸逆)
                transform.position = new Vector3(-(X + Xoffset) * scale, (Y + Yoffset) * scale, (Z + Zoffset) * scale);
            }
        }
        else
        {
            //てきとーな場所にとばす
            transform.position = new Vector3(0.0f, 10.0f, 10.0f);

        }
    }
}

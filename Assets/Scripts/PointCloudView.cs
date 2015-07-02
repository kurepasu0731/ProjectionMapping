using UnityEngine;
using System.Collections;
using Windows.Kinect;


public class PointCloudView : MonoBehaviour {

    public GameObject multiSourceManager;
    MultiSourceManager multiSourceManagerScript;
    FrameDescription depthFrameDesc;
    CameraSpacePoint[] cameraSpacePoints;
    CoordinateMapper mapper;

    private int depthWidth;
    private int depthHeight;


    private Vector3[] pointCloud;
    private Mesh mesh;
    private Vector3[] vertices;
    private Vector2[] uv;
    private int[] triangles;
    private const int downsampleSize = 2;

    void Start () {

        // Get the description of the depth frames.
        depthFrameDesc = KinectSensor.GetDefault().DepthFrameSource.FrameDescription;
        depthWidth = depthFrameDesc.Width;
        depthHeight = depthFrameDesc.Height;

        // buffer for points mapped to camera space coordinate.
        cameraSpacePoints = new CameraSpacePoint[depthWidth * depthHeight];
        mapper = KinectSensor.GetDefault ().CoordinateMapper;

        // get reference to DepthSourceManager (which is included in the distributed 'Kinect for Windows v2 Unity Plugin zip')
        multiSourceManagerScript = multiSourceManager.GetComponent<MultiSourceManager> ();

        // point cloud
        pointCloud = new Vector3[depthWidth * depthHeight];

        // crate mesh
        CreateMesh(depthWidth / downsampleSize, depthHeight / downsampleSize);

        transform.position = new Vector3(0.0f, 0.0f, 10.0f);
    }

    void Update () {
        // get new depth data from DepthSourceManager.
        ushort[] rawdata = multiSourceManagerScript.GetDepthData ();
        // map to camera space coordinate
        mapper.MapDepthFrameToCameraSpace (rawdata, cameraSpacePoints);

        for (int i = 0; i < cameraSpacePoints.Length; i++) {

            pointCloud[i] = new Vector3(-cameraSpacePoints[i].X, cameraSpacePoints[i].Y, cameraSpacePoints[i].Z);
        }


        gameObject.GetComponent<Renderer>().material.mainTexture = multiSourceManagerScript.GetColorTexture();

        RefreshMesh(multiSourceManagerScript.GetDepthData(), multiSourceManagerScript.ColorWidth, multiSourceManagerScript.ColorHeight);
    }


    void CreateMesh(int width, int height)
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        vertices = new Vector3[width * height];
        uv = new Vector2[width * height];
        triangles = new int[6 * ((width - 1) * (height - 1))];

        int triangleIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;

                vertices[index] = new Vector3(x, y, 0);
                uv[index] = new Vector2(((float)x / (float)width), ((float)y / (float)height));

                // Skip the last row/col
                if (x != (width - 1) && y != (height - 1))
                {
                    int topLeft = index;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + width;
                    int bottomRight = bottomLeft + 1;

                    //triangles[triangleIndex++] = topLeft;
                    //triangles[triangleIndex++] = topRight;
                    //triangles[triangleIndex++] = bottomLeft;
                    //triangles[triangleIndex++] = bottomLeft;
                    //triangles[triangleIndex++] = topRight;
                    //triangles[triangleIndex++] = bottomRight;

                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = bottomRight;
                    triangles[triangleIndex++] = topRight;
                }
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    private void RefreshMesh(ushort[] depthData, int colorWidth, int colorHeight)
    {
        var frameDesc = KinectSensor.GetDefault().DepthFrameSource.FrameDescription;

        ColorSpacePoint[] colorSpace = new ColorSpacePoint[depthData.Length];
        mapper.MapDepthFrameToColorSpace(depthData, colorSpace);

        for (int y = 0; y < frameDesc.Height; y += downsampleSize)
        {
            for (int x = 0; x < frameDesc.Width; x += downsampleSize)
            {
                int indexX = x / downsampleSize;
                int indexY = y / downsampleSize;
                int smallIndex = (indexY * (frameDesc.Width / downsampleSize)) + indexX;


                //double avg = GetAvg(depthData, x, y, frameDesc.Width, frameDesc.Height);

                vertices[smallIndex].x = (float)pointCloud[(y * frameDesc.Width) + x].x;
                vertices[smallIndex].y = (float)pointCloud[(y * frameDesc.Width) + x].y;
                vertices[smallIndex].z = (float)pointCloud[(y * frameDesc.Width) + x].z - 10.0f;

                // Update UV mapping with CDRP
                var colorSpacePoint = colorSpace[(y * frameDesc.Width) + x];
                uv[smallIndex] = new Vector2(colorSpacePoint.X / colorWidth, colorSpacePoint.Y / colorHeight);
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }


    private double GetAvg(ushort[] depthData, int x, int y, int width, int height)
    {
        double sum = 0.0;

        for (int y1 = y; y1 < y + downsampleSize; y1++)
        {
            for (int x1 = x; x1 < x + downsampleSize; x1++)
            {
                int fullIndex = (y1 * width) + x1;
                if (depthData[fullIndex] == 0)
                    sum += 10;
                else
                    sum += depthData[fullIndex] * 0.001;

            }
        }

        return sum / (downsampleSize * downsampleSize);
    }
}

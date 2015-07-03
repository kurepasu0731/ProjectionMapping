#include <opencv2/opencv.hpp>
#include <Windows.h>

namespace Projection{

	typedef struct disp_prop{
		int index;
		int x,y,width,height;
	} Disp_Prop;

	static int dispCount=-1;
	static std::vector<Disp_Prop> Disps_Prop;

	//ディスプレイの情報入手
	inline BOOL CALLBACK DispEnumProc(HMONITOR hMonitor, HDC hdcMonitor, LPRECT lprcMonitor, LPARAM dwData ) {
		Disp_Prop di;
		di.index = dispCount++;
		di.x = lprcMonitor->left;
		di.y = lprcMonitor->top;
		di.width = lprcMonitor->right - di.x;
		di.height = lprcMonitor->bottom - di.y;
		Disps_Prop.push_back(di);

		return TRUE; // TRUEは探索継続，FALSEで終了
	}

	//ディスプレイ検出
	inline void SearchDisplay(void) {
		// 一度だけ実行する
		if (dispCount == -1) {
			dispCount = 0;
			Disps_Prop = std::vector<Disp_Prop>();
			EnumDisplayMonitors(NULL, NULL, DispEnumProc, 0);
			Sleep(200);
		}
	}

	inline cv::Size getDispSize(int dispNum)
	{
		SearchDisplay();
		if (dispNum < dispCount) {
			Disp_Prop di = Disps_Prop[dispNum];
			return cv::Size(di.width, di.height);
		}
		return cv::Size(0, 0);
	}

	//プロジェクション
	inline void MySetFullScrean(const int num, const char *windowname){

		HWND windowHandle = ::FindWindowA(NULL, windowname);

		SearchDisplay();

		if (NULL != windowHandle) {

			//-ウィンドウスタイル変更（メニューバーなし、最前面）-
			SetWindowLongPtr(windowHandle,  GWL_STYLE, WS_POPUP);
			SetWindowLongPtr(windowHandle, GWL_EXSTYLE, WS_EX_TOPMOST);

			//-最大化する-
			ShowWindow(windowHandle, SW_MAXIMIZE);
			cv::setWindowProperty(windowname, CV_WND_PROP_FULLSCREEN, CV_WINDOW_FULLSCREEN );

			//-ディスプレイを指定-
			Disp_Prop di = Disps_Prop[num];

			//-クライアント領域をディスプレーに合わせる-
			SetWindowPos(windowHandle, NULL, di.x, di.y, di.width, di.height, SWP_FRAMECHANGED | SWP_NOZORDER);
		}
	}
}


extern "C" {

	__declspec(dllexport) void openWindow(const char* windowName)
    {
        cv::namedWindow(windowName, CV_WINDOW_NORMAL);
    }

	__declspec(dllexport) void closeWindow(const char* windowName)
    {
		cv::destroyWindow(windowName);
    }

	__declspec(dllexport) void showWindow(const char* windowName, unsigned char* const data, int width, int height)
    {
        cv::Mat img(height, width, CV_8UC4, data);
		cv::Mat dst;
		cv::cvtColor(img, dst, CV_RGBA2BGRA);
		cv::flip(dst, dst, 0);

        cv::imshow(windowName, img);
		//cv::waitKey(2);
    }

	__declspec(dllexport) void showKinectWindow(const char* windowName, unsigned char* const data, int width, int height)
    {
        cv::Mat img(height, width, CV_8UC4, data);
		cv::Mat dst;
		cv::cvtColor(img, dst, CV_RGBA2BGRA);

        cv::imshow(windowName, dst);
		//cv::waitKey(2);
    }

	__declspec(dllexport) void fullWindow(const char* windowName, int displayNum, unsigned char* const data, int width, int height)
    {
        cv::Mat img(height, width, CV_8UC4, data);
		cv::Mat dst;
		cv::cvtColor(img, dst, CV_RGBA2BGRA);
		cv::flip(dst, dst, 0);

		cv::resize(dst, dst, Projection::getDispSize(displayNum));
		Projection::MySetFullScrean(displayNum, windowName);
        cv::imshow(windowName, dst);
		//cv::waitKey(2);
    }

	__declspec(dllexport) void whiteFullWindow(const char* windowName, int displayNum)
    {
        cv::Mat img(cv::Size(640, 480), CV_8UC4, cv::Scalar::all(255));

		cv::resize(img, img, Projection::getDispSize(displayNum));
		Projection::MySetFullScrean(displayNum, windowName);
        cv::imshow(windowName, img);
		cv::waitKey(2);
    }

	__declspec(dllexport) void loadFullWindow(const char* windowName, const char* filePath, int displayNum)
    {
        cv::Mat img = cv::imread(filePath);

		cv::resize(img, img, Projection::getDispSize(displayNum));
		Projection::MySetFullScrean(displayNum, windowName);
        cv::imshow(windowName, img);
		cv::waitKey(2);
    }


	__declspec(dllexport) void timerKinectWindow(const char* windowName, unsigned char* const data, int width, int height, int timer)
    {
        cv::Mat img(height, width, CV_8UC4, data);
		cv::Mat dst;
		cv::cvtColor(img, dst, CV_RGBA2BGRA);

		cv::putText(dst, std::to_string(timer), cv::Point(50,100), cv::FONT_HERSHEY_SIMPLEX, 4.0, cv::Scalar(0,0,200), 2, CV_AA);

        cv::imshow(windowName, dst);
		cv::waitKey(2);
    }


	__declspec(dllexport) bool projectionChecker(const char* windowName, const char* pathImage, int displayNum, int numX, int numY, double point2dx[], double point2dy[])
    {
        cv::Mat img = cv::imread(pathImage);
		cv::Size projectorSize = Projection::getDispSize(displayNum);

		cv::resize(img, img, projectorSize);
		Projection::MySetFullScrean(displayNum, windowName);
        cv::imshow(windowName, img);				// 投影


		// チェッカーパターンの交点検出		
		const cv::Size patternSize( numX, numY );		// チェッカーパターンの交点の数
		cv::vector<cv::Point2f>	imagePoints;			// チェッカー交点座標を格納する行列
		cv::TermCriteria criteria( CV_TERMCRIT_ITER | CV_TERMCRIT_EPS, 20, 0.001 ); 

		if( cv::findChessboardCorners( img, patternSize, imagePoints) ) 
		{
			cv::Mat grayImg;
			cv::cvtColor(img, grayImg, CV_BGR2GRAY);	//グレースケール化
			//コーナー位置をサブピクセル精度に修正
			cv::cornerSubPix(grayImg, imagePoints, cv::Size(3, 3), cv::Size(-1, -1), criteria);

			for(int i = 0; i < imagePoints.size(); ++i)
			{
				point2dx[i] = imagePoints[i].x;
				point2dy[i] = (projectorSize.height-1) - imagePoints[i].y;		// Unity座標に合わせる
			}
			return true;
		}
		else
		{
			return false;
		}
    }


	__declspec(dllexport) bool getWorldCheckerPoint(const char* windowName, unsigned char* const data, int width, int height, int numX, int numY, double point2dx[], double point2dy[])
    {
        cv::Mat img(height, width, CV_8UC4, data);
		cv::Mat dst;
		cv::cvtColor(img, dst, CV_RGBA2BGRA);

		// チェッカーパターンの交点検出		
		const cv::Size patternSize( numX, numY );		// チェッカーパターンの交点の数
		cv::vector<cv::Point2f>	imagePoints;			// チェッカー交点座標を格納する行列
		cv::TermCriteria criteria( CV_TERMCRIT_ITER | CV_TERMCRIT_EPS, 20, 0.001 ); 

		if( cv::findChessboardCorners( img, patternSize, imagePoints) ) 
		{
			cv::Mat grayImg;
			cv::cvtColor(dst, grayImg, CV_BGR2GRAY);	//グレースケール化
			//コーナー位置をサブピクセル精度に修正
			cv::cornerSubPix(grayImg, imagePoints, cv::Size(3, 3), cv::Size(-1, -1), criteria);

			// 検出点を描画する
			cv::drawChessboardCorners( dst, patternSize, ( cv::Mat )( imagePoints ), true );
			if(strcmp( windowName, "" ) != 0)
				cv::imshow(windowName, dst);

			for(int i = 0; i < imagePoints.size(); ++i)
			{
				point2dx[i] = imagePoints[i].x;
				point2dy[i] = (height-1) - imagePoints[i].y;		// Unity座標に合わせる
			}
			return true;
		}
		else
		{
			// 検出点を描画する
			cv::drawChessboardCorners( dst, patternSize, ( cv::Mat )( imagePoints ), true );
			if(strcmp( windowName, "" ) != 0)
				cv::imshow(windowName, dst);

			return false;
		}
    }


	__declspec(dllexport) void saveTexture(const char* filePath, unsigned char* const data, int width, int height)
	{
		cv::Mat img(height, width, CV_8UC4, data);
		cv::Mat dst;
		cv::cvtColor(img, dst, CV_RGBA2BGRA);

		cv::imwrite(filePath, dst);
	}
}
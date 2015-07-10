#include <iostream>
#include <sstream>

#include <Kinect.h>
#include <opencv2\opencv.hpp>

#include <opencv2/core/core.hpp>
#include <opencv2/imgproc/imgproc.hpp>
#include <opencv2/highgui/highgui.hpp>


// Visual Studio Professional�ȏ���g���ꍇ��CComPtr�̗��p���������Ă��������B
#include "ComPtr.h"
//#include <atlbase.h>

// ���̂悤�Ɏg���܂�
// ERROR_CHECK( ::GetDefaultKinectSensor( &kinect ) );
// ���Ђł̉���̂��߂Ƀ}�N���ɂ��Ă��܂��B���ۂɂ͓W�J�����`�Ŏg�����Ƃ��������Ă��������B
#define ERROR_CHECK( ret )  \
    if ( (ret) != S_OK ) {    \
        std::stringstream ss;	\
        ss << "failed " #ret " " << std::hex << ret << std::endl;			\
        throw std::runtime_error( ss.str().c_str() );			\
    }


int allLabelNum = 10;

struct TemplateLabel
{
	int templateNum;		// �e���v���[�g�ԍ�(0�͊���U���Ă��Ȃ�)
	int checkNum;			// ������
	bool detect_flag;		// ���o�m�F(5��A���Ō��o���ꂽ��true)
	int objectID;			// �I�u�W�F�N�g���Ɋ���U��ID

	// ������1��ȏ�
	int detect_x;			// ��������x���W
	int detect_y;			// ��������y���W

	// ���o��
	cv::Mat depthRect;		// ���o���̐[�x�l
	
	
	TemplateLabel()
		: templateNum (0)
		, checkNum (0)
		, detect_flag (false)
		, objectID (-1)
		, detect_x (0)
		, detect_y (0)
	{}
};



typedef std::pair<float, cv::Point> str_pair;


bool sort_greater(const str_pair& left,const str_pair& right){
    return left.first > right.first;
}


// �e���v���[�g�}�b�`���O
void templateMatching(cv::Mat input_image, cv::Mat depth_image, TemplateLabel Label[], int labelNum, int desk_pos_y, int desk_pos_z )
{
	/***** �@�}�X�N�X�V���� *****/
	
	// �e���v���[�g���
	int allTemplateNum = 2;
	std::vector<std::pair<std::string, float>> fileNames;
	std::pair<std::string, float> fileName1;
	fileName1.first = "template1.png";
	fileName1.second = 0.65;
	fileNames.push_back(fileName1);
	std::pair<std::string, float> fileName2;
	fileName2.first = "template2.png";
	fileName2.second = 0.55;
	fileNames.push_back(fileName2);

	// �e���v���[�g���̊i�[
	std::vector<std::pair<cv::Mat, float>> templateImgs;
	for(int i = 0; i < allTemplateNum; ++i)
	{
		std::pair<cv::Mat, float> templateImg;
		templateImg.first = cv::imread(fileNames.at(i).first, 0);
		templateImg.second = fileNames.at(i).second;

		templateImgs.push_back(templateImg);
	}

	// �����p�̉摜
	cv::Mat matching_img;
	input_image.copyTo(matching_img);

	// �I�u�W�F�N�g�̉��[��臒l
	std::vector<int> under_pos;
	for(int i = 0; i < input_image.cols; ++i)
	{
		under_pos.push_back(desk_pos_y);		// �����͊���y���W
	}

	// �}�X�N�̊m�F
	for(int i = 0; i < labelNum; ++i)
	{
		// ���o�ς݂̃��x���ɑ΂���
		if(Label[i].detect_flag)
		{
			
			// �O�t���[������̃Y���ʂ̌v�Z
			int sum_diff = 0;
			for(int x = Label[i].detect_x; x < Label[i].detect_x + templateImgs.at(Label[i].templateNum-1).first.cols; ++x)
			{
				for(int y = Label[i].detect_y; y < Label[i].detect_y + templateImgs.at(Label[i].templateNum-1).first.rows; ++y)
				{
					// �[�x�l�����Ă��Ȃ��ꍇ�Ɗ�����O�̐[�x�l�͍l�����Ȃ�
					if(depth_image.at<UINT16>(y,x) != 0 && Label[i].depthRect.at<UINT16>(y,x) != 0 && depth_image.at<UINT16>(y,x) >= desk_pos_z)
					{
						sum_diff += abs((int)(Label[i].depthRect.at<UINT16>(y,x) - depth_image.at<UINT16>(y,x)) );
					}
				}				
			}

			// �S�̂�4����1���ꂽ�猟�o���O��
			if(sum_diff >= templateImgs.at(Label[i].templateNum-1).first.cols * templateImgs.at(Label[i].templateNum-1).first.rows * 100 / 4 )
			{
				// ���x���̏�����
				Label[i].templateNum = 0;
				Label[i].checkNum = 0;
				Label[i].detect_flag = false;
				Label[i].objectID = -1;
				Label[i].detect_x = 0;
				Label[i].detect_y = 0;
			}
			else
			{
				// ���o�ς݂̃��x���̓}�X�N����
				cv::Rect roi_rect(0, 0, templateImgs.at(Label[i].templateNum-1).first.cols, templateImgs.at(Label[i].templateNum-1).first.rows);
				roi_rect.x = Label[i].detect_x;
				roi_rect.y = Label[i].detect_y;

				cv::rectangle(matching_img, roi_rect, cv::Scalar(0), -1);

				// �I�u�W�F�N�g�̉��[��臒l�̍X�V
				for(int x = Label[i].detect_x; x < Label[i].detect_x + templateImgs.at(Label[i].templateNum-1).first.cols; ++x)
				{
					if(Label[i].detect_y <= under_pos.at(x))
						under_pos.at(x) = Label[i].detect_y;
				}
			}
		}
	}


	/*****  �A�}�b�`���O���� *****/
	cv::Mat result_img;
	cv::Point max_pt;
	double maxVal;
	std::vector<std::pair<int, cv::Point>> good_positions;

	for (int i = 0; i < templateImgs.size(); ++i)
	{
		cv::Rect roi_rect(0, 0, templateImgs.at(i).first.cols, templateImgs.at(i).first.rows);

		// 臒l�ȏ�̃e���v���[�g�𕡐����o
		do
		{
			cv::matchTemplate(matching_img, templateImgs.at(i).first, result_img, CV_TM_CCOEFF_NORMED);		// �e���v���[�g�}�b�`���O

			cv::minMaxLoc(result_img, NULL, &maxVal, NULL, &max_pt);

			// 臒l�ȏ�
			if(maxVal >= templateImgs.at(i).second)
			{
				// ��x���������ꏊ�̓}�X�N����
				roi_rect.x = max_pt.x;
				roi_rect.y = max_pt.y;
				cv::rectangle(matching_img, roi_rect, cv::Scalar(0), -1);

				// �ςݏオ���Ă���t�߂̏ꍇ�͉��o�^
				int under_y = max_pt.y+templateImgs.at(i).first.rows;
				bool pile_flag = false;
				for(int x = max_pt.x; x < max_pt.x+templateImgs.at(i).first.cols; ++x)
				{
					if(under_y >= under_pos.at(x)-20 && under_y <= under_pos.at(x)+20)
					{
						pile_flag = true;
						break;
					}
				}
				if(pile_flag)
				{
					// ���o�^
					std::pair<int, cv::Point> good_position;
					good_position.first = i+1;
					good_position.second = max_pt;
					good_positions.push_back(good_position);
				}
			}
		}
		while(maxVal >= templateImgs.at(i).second);
	}


	/*****  �B���x���o�^���� *****/

	// ���Ɍ��o�ς݂̂��̂�O�񔭌��������̈ȊO�͏�����
	for(int i = 0; i < labelNum; ++i)
	{
		bool init_flag = true;	// �������t���O

		// ���o�ς݂łȂ����x���ɑ΂���
		if(!Label[i].detect_flag)
		{
			// �O�񔭌������ʒu�ɓ����I�u�W�F�N�g�����邩����
			for(int j = 0; j < good_positions.size(); ++j)
			{
				// �e���v���[�g�ԍ��������ŁA���W�ʒu���߂�(�ߖT3��f�ȓ�)
				if( Label[i].templateNum == good_positions.at(j).first
					&& good_positions.at(j).second.x >= Label[i].detect_x-3 && good_positions.at(j).second.x <= Label[i].detect_x+3
					&& good_positions.at(j).second.y >= Label[i].detect_y-3 && good_positions.at(j).second.y <= Label[i].detect_y+3)
				{
					Label[i].checkNum++;	// �����񐔂𑝂₷

					// 5��ȏ㔭�����ꂽ�猟�o
					if(Label[i].checkNum >= 5)
					{
						Label[i].detect_flag = true;
						Label[i].checkNum = 0;
						Label[i].depthRect = depth_image.clone();
					}

					good_positions.at(j).first = 0; // �o�^�t���O

					// ���������烋�[�v�𔲂���
					init_flag = false;
					break;
				}
			}
		}
		else
		{
			init_flag = false;
		}

		// ���x���̏�����
		if(init_flag)
		{
			Label[i].templateNum = 0;
			Label[i].checkNum = 0;
			Label[i].detect_flag = false;
			Label[i].objectID = -1;
			Label[i].detect_x = 0;
			Label[i].detect_y = 0;
		}
	}

	// �V�K�ɓo�^
	for(int i = 0; i < good_positions.size(); ++i)
	{
		// �܂��o�^����Ă��Ȃ����
		if(good_positions.at(i).first != 0)
		{
			for(int j = 0; j < labelNum; ++j )
			{
				// �o�^����Ă��Ȃ����x���ł���Γo�^
				if(Label[j].templateNum == 0)
				{
					Label[j].templateNum = good_positions.at(i).first;
					Label[j].checkNum = 1;
					Label[j].detect_x = good_positions.at(i).second.x;
					Label[j].detect_y = good_positions.at(i).second.y;
				}
			}
		}
	}


	// ����(�m�F�p)
	cv::cvtColor(matching_img, matching_img, CV_GRAY2BGR);

	cv::namedWindow("search image", CV_WINDOW_AUTOSIZE|CV_WINDOW_FREERATIO);
	cv::namedWindow("result image", CV_WINDOW_AUTOSIZE|CV_WINDOW_FREERATIO);

	for(int i = 0; i < labelNum; ++i)
	{
		std::cout << i << "�Ԗ�:" << Label[i].templateNum << ", ��:" << Label[i].checkNum << ", ���W;" << Label[i].detect_x << "," << Label[i].detect_y << std::endl;
		// �o�^����Ă��郉�x���̂�
		if(Label[i].templateNum != 0)
		{
			cv::Rect roi_rect(0, 0, templateImgs.at(Label[i].templateNum-1).first.cols, templateImgs.at(Label[i].templateNum-1).first.rows);
			roi_rect.x = Label[i].detect_x;
			roi_rect.y = Label[i].detect_y;

			if(Label[i].detect_flag)
				cv::rectangle(matching_img, roi_rect, cv::Scalar(0,0,255), 3);
			else if(Label[i].templateNum == 1)
				cv::rectangle(matching_img, roi_rect, cv::Scalar(0,255,0), 3);
			else
				cv::rectangle(matching_img, roi_rect, cv::Scalar(255,0,0), 3);
		}
	}

	cv::imshow("search image", matching_img);
	cv::imshow("result image", result_img);
}



class KinectApp
{
private:

    IKinectSensor* kinect;

    IDepthFrameReader* depthFrameReader;
    std::vector<UINT16> depthBuffer;

	IInfraredFrameReader* infraredFrameReader;
    std::vector<UINT16> infraredBuffer;

    int infraredWidth;
    int infraredHeight;


    const char* DepthWindowName;

    UINT16 minDepthReliableDistance;
    UINT16 maxDepthReliableDistance;

    int depthWidth;
    int depthHeight;

    int depthPointX;
    int depthPointY;

	// �e���v���[�g�摜
	cv::Mat tmp_img1;

	TemplateLabel templateLabel[10];

public:

    // ������
    void initialize()
    {
        // �f�t�H���g��Kinect���擾����
        ERROR_CHECK( ::GetDefaultKinectSensor( &kinect ) );

        // Kinect���J��
        ERROR_CHECK( kinect->Open() );

        BOOLEAN isOpen = false;
        ERROR_CHECK( kinect->get_IsOpen( &isOpen ) );
        if ( !isOpen ){
            throw std::runtime_error("Kinect���J���܂���");
        }

		DepthWindowName = "IR Image";

        // Depth���[�_�[���擾����
        ComPtr<IDepthFrameSource> depthFrameSource;
        ERROR_CHECK( kinect->get_DepthFrameSource( &depthFrameSource ) );
        ERROR_CHECK( depthFrameSource->OpenReader( &depthFrameReader ) );

        // Depth�摜�̃T�C�Y���擾����
        ComPtr<IFrameDescription> depthFrameDescription;
        ERROR_CHECK( depthFrameSource->get_FrameDescription( &depthFrameDescription ) );
        ERROR_CHECK( depthFrameDescription->get_Width( &depthWidth ) );
        ERROR_CHECK( depthFrameDescription->get_Height( &depthHeight ) );

        depthPointX = depthWidth / 2;
        depthPointY = depthHeight / 2;

        // Depth�̍ő�l�A�ŏ��l���擾����
        ERROR_CHECK( depthFrameSource->get_DepthMinReliableDistance( &minDepthReliableDistance ) );
        ERROR_CHECK( depthFrameSource->get_DepthMaxReliableDistance( &maxDepthReliableDistance ) );

        std::cout << "Depth�f�[�^�̕�   : " << depthWidth << std::endl;
        std::cout << "Depth�f�[�^�̍��� : " << depthHeight << std::endl;

        std::cout << "Depth�ŏ��l       : " << minDepthReliableDistance << std::endl;
        std::cout << "Depth�ő�l       : " << maxDepthReliableDistance << std::endl;

	    // �ԊO���摜���[�_�[���擾����
        ComPtr<IInfraredFrameSource> infraredFrameSource;
        ERROR_CHECK( kinect->get_InfraredFrameSource( &infraredFrameSource ) );
        ERROR_CHECK( infraredFrameSource->OpenReader( &infraredFrameReader ) );

        // �ԊO���摜�̃T�C�Y���擾����
        ComPtr<IFrameDescription> infraredFrameDescription;
        ERROR_CHECK( infraredFrameSource->get_FrameDescription( &infraredFrameDescription ) );
        ERROR_CHECK( infraredFrameDescription->get_Width( &infraredWidth ) );
        ERROR_CHECK( infraredFrameDescription->get_Height( &infraredHeight ) );

        // �o�b�t�@�[���쐬����
        infraredBuffer.resize( infraredWidth * infraredHeight );

        // �o�b�t�@�[���쐬����
        depthBuffer.resize( depthWidth * depthHeight );

		tmp_img1 = cv::imread("template1.png", 0);
    }

    void run()
    {
        while ( 1 ) {
            update();
            draw();

            auto key = cv::waitKey( 10 );
            if ( key == 'q' ){
                break;
            }
        }
    }

private:

    // �f�[�^�̍X�V����
    void update()
    {
        updateDepthFrame();
    }

    void updateDepthFrame()
    {
		// �t���[�����擾����
        ComPtr<IInfraredFrame> infraredFrame;
        auto ret = infraredFrameReader->AcquireLatestFrame( &infraredFrame );
        if ( ret == S_OK ){
            // BGRA�̌`���Ńf�[�^���擾����
            ERROR_CHECK( infraredFrame->CopyFrameDataToArray( infraredBuffer.size(), &infraredBuffer[0] ) );

            // �t���[�����������
            // infraredFrame->Release();
			
			// �J���[�f�[�^��\������
        cv::Mat colorImage( infraredHeight, infraredWidth, CV_16UC1, &infraredBuffer[0] );
        cv::imshow( "Infrared Image", colorImage );


		// Depth�f�[�^��\������
		cv::Mat depthtruth( depthHeight, depthWidth, CV_16UC1 );
		cv::Mat backImage( depthHeight, depthWidth, CV_16UC1 );
		cv::Mat mask( depthHeight, depthWidth, CV_8UC1 );
		cv::Mat diff;

        // Depth�t���[�����擾����
        ComPtr<IDepthFrame> depthFrame;
        auto ret = depthFrameReader->AcquireLatestFrame( &depthFrame );
        if ( ret != S_OK ){
            return;
        }

        // �f�[�^���擾����
        ERROR_CHECK( depthFrame->CopyFrameDataToArray( depthBuffer.size(), &depthBuffer[0] ) );

		// Depth�f�[�^��\������
        cv::Mat depthImage( depthHeight, depthWidth, CV_8UC1 );

        // Depth�f�[�^��0-255�̃O���[�f�[�^�ɂ���
        for ( int i = 0; i < depthImage.total(); ++i ){
            //depthImage.data[i] = ~((depthBuffer[i] * 255) / maxDepthReliableDistance);
            depthImage.data[i] = ~((depthBuffer[i] * 255) / 1700);

			//�w�i�������ݗp(����̂ݎ��s)
			depthtruth.at<UINT16>(i) = depthBuffer[i];
        }


		//�w�i����
		//�������ݗp (����̂ݎ��s)
		//cv::FileStorage fs("depthDATA.xml", cv::FileStorage::WRITE);
		//fs << "depth_mat" << depthtruth;


		////�ǂݍ��ݗp
		cv::FileStorage fs2("depthDATA.xml", cv::FileStorage::READ);
		fs2["depth_mat"] >> backImage;

		int noise = 50;

		for(int i=0;i<backImage.total();i++){
			if( depthBuffer[i] <= backImage.at<UINT16>(i) - noise && depthBuffer[i] != 0){
				mask.data[i]=255;
			}else{
				mask.data[i]=0;
			}
		}

		  //// �T���摜
			colorImage.copyTo(diff,mask);
			
			diff.convertTo(diff, CV_8UC1, 255.0/65535.0);
			cv::imshow("diff",diff);


			templateMatching(diff, depthtruth, templateLabel, allLabelNum, 310, 1450);

    }
	}

    void draw()
    {
        
    }

    
};

void main()
{
    try {
        KinectApp app;
        app.initialize();
        app.run();
    }
    catch ( std::exception& ex ){
        std::cout << ex.what() << std::endl;
    }
}



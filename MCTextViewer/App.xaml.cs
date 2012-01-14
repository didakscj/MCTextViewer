using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using DropNet;
using DropNet.Models;
using System.IO.IsolatedStorage;


namespace MCTextViewer
{
    public partial class App : Application
    {
        private static MainViewModel viewModel = null;
        public static DropNetClient _client { get; set; }
        public static String _selectedtextfile = "";
        public static bool _filesaving = false;
        /// <summary>
        /// 바인딩할 뷰에서 사용되는 정적 ViewModel입니다.
        /// </summary>
        /// <returns>MainViewModel 개체입니다.</returns>
        public static MainViewModel ViewModel
        {
            get
            {
                // 필요할 때까지 뷰 모델 만들기를 지연합니다.
                if (viewModel == null)
                    viewModel = new MainViewModel();

                return viewModel;
            }
        }

        /// <summary>
        /// 전화 응용 프로그램의 루트 프레임에 간단하게 액세스할 수 있습니다.
        /// </summary>
        /// <returns>전화 응용 프로그램의 루트 프레임입니다.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Application 개체의 생성자입니다.
        /// </summary>
        public App()
        {
            // Catch되지 않은 예외의 전역 처리기입니다. 
            UnhandledException += Application_UnhandledException;

            // 표준 Silverlight 초기화
            InitializeComponent();

            // 전화 관련 초기화
            InitializePhoneApplication();

            // 디버깅하는 동안 그래픽 프로파일링 정보를 표시합니다.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // 현재 프레임 속도 카운터를 표시합니다.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // 각 프레임에서 다시 그려지는 응용 프로그램의 영역을 표시합니다.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // 색이 지정된 오버레이를 포함하는 GPU에 전달되는 페이지 영역을 표시하는 
                // 비프로덕션 분석 시각화 모드를 설정합니다.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // 응용 프로그램의 PhoneApplicationService 개체에 대한 UserIdleDetectionMode 속성을 사용 안 함으로 설정하여
                // 응용 프로그램 유휴 검색을 해제합니다.
                // 주의:- 디버그 모드에서만 사용합니다. 사용자 유휴 검색을 해제하는 응용 프로그램은 사용자가 전화를 사용하지 않을 경우에도
                // 계속 실행되어 배터리 전원을 소모합니다.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

            
        }

        // 시작 메뉴 등에서 응용 프로그램을 시작할 때 실행할 코드입니다.
        // 이 코드는 응용 프로그램이 다시 활성화될 때는 실행되지 않습니다.
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
        }

        // 응용 프로그램이 활성화(포그라운드로 이동)될 때 실행할 코드입니다.
        // 이 코드는 응용 프로그램이 처음 시작될 때는 실행되지 않습니다.
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            // 응용 프로그램 상태가 적절하게 복원되었는지 확인합니다.
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }

        // 응용 프로그램이 비활성화(백그라운드로 전송)될 때 실행할 코드입니다.
        // 이 코드는 응용 프로그램이 닫힐 때는 실행되지 않습니다.
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            // 여기서 필수 응용 프로그램 상태가 유지되는지 확인합니다.
            if (App._filesaving)
            {
                IsolatedStorageSettings.ApplicationSettings[App._selectedtextfile + "readcount"] = 0;
            }
        }

        // 응용 프로그램이 닫힐 때(예: 사용자가 [뒤로]를 누르는 경우) 실행할 코드입니다.
        // 이 코드는 응용 프로그램이 비활성화될 때는 실행되지 않습니다.
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        // 탐색이 실패할 때 실행할 코드입니다.
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // 탐색이 실패했습니다. 중단하고 디버거를 실행합니다.
                System.Diagnostics.Debugger.Break();
            }
        }

        // 처리되지 않은 예외에 대해 실행할 코드입니다.
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // 처리되지 않은 예외가 발생했습니다. 중단하고 디버거를 실행합니다.
                System.Diagnostics.Debugger.Break();
            }
        }

        #region 전화 응용 프로그램 초기화

        // 이중 초기화를 사용하지 않습니다.
        private bool phoneApplicationInitialized = false;

        // 이 메서드에 추가 코드를 추가하지 않습니다.
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // 프레임을 만들지만 프레임을 아직 RootVisual로 설정하지 않습니다. 이렇게 하면
            // 응용 프로그램이 렌더링할 준비가 될 때까지 시작 화면이 활성 상태를 유지합니다.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // 탐색 오류를 처리합니다.
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // 다시 초기화하지 않습니다.
            phoneApplicationInitialized = true;
        }

        // 이 메서드에 추가 코드를 추가하지 않습니다.
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // 응용 프로그램을 렌더링하도록 루트 Visual을 설정합니다.
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // 더 이상 필요하지 않으므로 이 처리기를 제거합니다.
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion
    }
}
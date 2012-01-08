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
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.ObjectModel;
using MCTextViewer.ViewModels;
using DropNet;
using DropNet.Models;
using DropNet.Helpers;
using Microsoft.Phone.Shell;
using System.Windows.Resources;
using System.Text.RegularExpressions;
using System.Threading;

namespace MCTextViewer
{
    
    public partial class MainPage : PhoneApplicationPage
    {
        bool dpfile = false;

        /**
         *  라이브러리 항목 리스트
         */
        private ObservableCollection<LibraryDataList> _textlists = new ObservableCollection<LibraryDataList>();
        public ObservableCollection<LibraryDataList> TextLists
        {
            get { return _textlists; }
            set
            {
                _textlists = value;
            }
        }

        /**
        *  드롭박스 항목 리스트
        */
        private ObservableCollection<DropBoxDataList> _dropboxdatalist = new ObservableCollection<DropBoxDataList>();
        public ObservableCollection<DropBoxDataList> Dropboxdatalist
        {
            get { return _dropboxdatalist; }
            set
            {
                _dropboxdatalist = value;
            }
        }

        /**
        *  다운로드 항목 리스트
        */
        private ObservableCollection<Downlist> _downlists = new ObservableCollection<Downlist>();
        public ObservableCollection<Downlist> Downlists
        {
            get { return _downlists; }
            set
            {
                _downlists = value;
            }
        }

        ProgressBar browserLoadingBar;
        ProgressBar dropboxLoadingBar;
        ProgressBar downloadLoadingBar;

        bool DropboxConnected = false;

        String DropboxUserToken = "";
        String DropboxScretToken = "";

        ApplicationBarIconButton seledtedDeleteButton = new ApplicationBarIconButton(
            new Uri("/Images/delete-icon.png", UriKind.Relative));
        ApplicationBarIconButton seledtedViewButton = new ApplicationBarIconButton(
           new Uri("/Images/view-icon.png", UriKind.Relative));
        // 생성자
        public MainPage()
        {
            InitializeComponent();
            
            
            ApplicationBar = new ApplicationBar();
            ApplicationBar.Mode = ApplicationBarMode.Minimized;
            ApplicationBar.Opacity = 0.5;
            this.seledtedDeleteButton.Text = "Delete";
            this.seledtedViewButton.Text = "View";

            
            ApplicationBar.Buttons.Add(seledtedViewButton);
            ApplicationBar.Buttons.Add(seledtedDeleteButton);
            seledtedDeleteButton.Click += new EventHandler(seledtedDeleteButton_Click);
            seledtedViewButton.Click += new EventHandler(seledtedViewButton_Click);

            //드롭박스 파일 다운로드 탭 초기화
            InitTextFileDownloadTab();

            //리스트박스 아이템 소스 지정
            this.lb_library.ItemsSource = TextLists;
            ReadFileList();

            // ListBox 컨트롤의 데이터 컨텍스트를 샘플 데이터로 설정합니다.
            //DataContext = App.ViewModel;
            //this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        private void InitTextFileDownloadTab()
        {
            loginBrowser.Visibility = Visibility.Collapsed;
            loginBrower_loadingPanel.Visibility = Visibility.Collapsed;
            browserLoadingBar = new ProgressBar();
            browserLoadingBar.Foreground = (Brush)Application.Current.Resources["PhoneAccentBrush"];
            loginBrower_loadingPanel.Children.Add(browserLoadingBar);

            //드롭박스 데이타 리스트 선 색
            dropboxTitle.Visibility = Visibility.Collapsed;             
            lb_dropboxdata.ItemsSource = Dropboxdatalist;
            lb_dropboxdata.BorderBrush = (Brush)Application.Current.Resources["PhoneForegroundBrush"];
            lb_dropboxdata.Visibility = Visibility.Collapsed;
            // 드롭박스 로딩 프로그래스바
            dropboxLoadingBar = new ProgressBar();
            dropboxLoadingBar.Foreground = (Brush)Application.Current.Resources["PhoneAccentBrush"];
            dropboxloadingPanel.Children.Add(dropboxLoadingBar);
            dropboxloadingPanel.Visibility = Visibility.Collapsed;



            //다운로드 데이타 리스트 선 색
            downloadingTitle.Visibility = Visibility.Collapsed;  
            lb_daownloaddata.ItemsSource = Downlists;
            lb_daownloaddata.BorderBrush = (Brush)Application.Current.Resources["PhoneForegroundBrush"];
            lb_daownloaddata.Visibility = Visibility.Collapsed;
            // 다운로드 로딩 프로그래스바
            downloadLoadingBar = new ProgressBar();
            downloadLoadingBar.Foreground = (Brush)Application.Current.Resources["PhoneAccentBrush"];
            downloadLoadingPanel.Children.Add(downloadLoadingBar);
            downloadLoadingPanel.Visibility = Visibility.Collapsed;
        }

        //// ViewModel 항목에 대한 데이터를 로드합니다.
        //private void MainPage_Loaded(object sender, RoutedEventArgs e)
        //{
        //    if (!App.ViewModel.IsDataLoaded)
        //    {
        //        App.ViewModel.LoadData();
        //    }
        //}

        /**
         *  격리 저장장소의 파일 리스트 읽기 
         */
        public void ReadFileList()
        {
            _textlists.Clear();
            IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication();
            //StreamReader sr = null;
            
            string[] dirs = file.GetDirectoryNames("DownloadFiles");
            if (dirs.Length != 0)
            {
                string[] filenames = file.GetFileNames("DownloadFiles\\*");
                foreach (string s in filenames)
                {
                    DateTimeOffset accessdate =  file.GetLastAccessTime("DownloadFiles\\" + s);
                    string[] paths = s.Split('.');
                    string iconname = "";
                    switch ( paths[paths.Length-1].ToUpper())
                    {
                        case "TXT":
                            iconname = "txticon.png";
                            break;
                        case "PDF":
                            iconname = "pdficon.png";
                            break;
                        case "RTF":
                            iconname = "rtficon.png";
                            break;
                        default:
                            iconname = "etcicon.png";
                            break;
                    }
                    _textlists.Add(new LibraryDataList(s, accessdate.DateTime.ToString(), iconname));

                }
            }
            else
            {
                //_textlists.Add(new LibraryDataList("No Text File")); 
            }
        }


        #region Private Instance Method

        /// <summary>
        /// 선택된 PanoramaItem 페이지를 활성화 한다.
        /// </summary>
        private void ActivateSelectedPanoramaItem()
        {
            PanoramaItem panoItem = panoramaMain.SelectedItem as PanoramaItem;
            if (panoItem == null)
                return;

            string panoItemTag = panoItem.Tag.ToString();
            if (string.IsNullOrWhiteSpace(panoItemTag))
                return;

            switch (panoItemTag)
            {
                case "panoItemTextLibrary":
                    ApplicationBar.Mode = ApplicationBarMode.Default;
                    //ApplicationBarMenuItem menuitem = new ApplicationBarMenuItem("menu|");
                    //ApplicationBar.Buttons.Add(seledtedDeleteButton);
                    ApplicationBar.IsVisible = true;
                    ApplicationBar.Mode = ApplicationBarMode.Minimized;
                    ReadFileList();
                    break;

                case "panoItemDropBoxDownload":
                    ApplicationBar.IsVisible = false;
                    if (!DropboxConnected)
                        DropboxConnect();

                    break;
            }
        }

        #endregion Private Instance Method


        private void lb_library_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //LibraryDataList libSelectedItem = (LibraryDataList)lb_library.SelectedItem;
            //int libSelectedItemIndex = lb_library.SelectedIndex;

            ApplicationBar.Mode = ApplicationBarMode.Default;

        }

        /// <summary>
        /// 선택된 PanoramaItem 변경 시
        /// </summary>
        /// <param name="sender"> Panorama Control </param>
        /// <param name="e"> SelecionChanged 이벤트 정보 </param>
        private void Panorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ActivateSelectedPanoramaItem();
        }

        void seledtedViewButton_Click(object sender, EventArgs e)
        {
            LibraryDataList libSelectedItem = (LibraryDataList)lb_library.SelectedItem;
            int libSelectedItemIndex = lb_library.SelectedIndex;
            //MessageBox.Show(libSelectedItemIndex.ToString());
            if (libSelectedItemIndex != -1)
            {
                NavigationService.Navigate(new Uri("/TextViewPage.xaml?data="+libSelectedItem.Name, UriKind.Relative));

                
            }
        }

        void seledtedDeleteButton_Click(object sender, EventArgs e)
        {
            LibraryDataList libSelectedItem = (LibraryDataList)lb_library.SelectedItem;
            int libSelectedItemIndex = lb_library.SelectedIndex;
             //MessageBox.Show(libSelectedItemIndex.ToString());
            if (libSelectedItemIndex != -1)
            {
                //리스트에서 삭제
                _textlists.RemoveAt(libSelectedItemIndex);

                //저장장소파일 삭제
                IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication();
                //StreamReader sr = null;

                string[] dirs = file.GetDirectoryNames("DownloadFiles");
                if (dirs.Length != 0)
                {
                    String deletefile = "DownloadFiles\\" + libSelectedItem.Name;
                    file.DeleteFile(deletefile);
                }
                else
                {
                    //MessageBox.Show("Fail: Delete File");
                    popupmessage("Delete File error");
                }

                try
                {
                    //셋팅 지우기
                    IsolatedStorageSettings.ApplicationSettings[libSelectedItem.Name] = null;
                    IsolatedStorageSettings.ApplicationSettings[libSelectedItem.Name + "readcount"] = null;
                }
                catch (KeyNotFoundException)
                {
                    //없으면 말고
                }

                ApplicationBar.Mode = ApplicationBarMode.Minimized;
                ReadFileList();
            }


        }

        /**
         * 드롭박스 접속
         */
        private void DropboxConnect()
        {
           
            loginBrower_loadingPanel.Visibility = Visibility.Visible;
            browserLoadingBar.IsIndeterminate = true;
            
            if (DropboxConnected == false)
            {
                //드롭박스 접속용 초기화
                DropboxConnected = true;

                App._client = new DropNetClient("meqy9y4nicqmr3k", "8g6858obthlzghw");

                // Async
                App._client.GetTokenAsync((userToken) =>
                {
                   
                    //Dont really need to do anything with userLogin, DropNet takes care of it for now
                    var url = App._client.BuildAuthorizeUrl("http://dkdevelopment.net/BoxShotLogin.htm");
                    //Capture the LoadCompleted event from the browser so we can check when the user has logged in
                    loginBrowser.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(loginBrowser_LoadCompleted);
                    //Open a browser with the URL
                    if (url != null)
                    {  
                        loginBrowser.Navigate(new Uri(url));
                    }
                },
                (error) =>
                {
                    //Handle error
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        //MessageBox.Show("Network Error");
                        popupmessage("Connection error");
                        DropboxConnected = false;
                    });
                });
            }
            else
            {
                browserLoadingBar.IsIndeterminate = false;
                loginBrower_loadingPanel.Visibility = Visibility.Collapsed;
                
            }
        }

        void loginBrowser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            browserLoadingBar.IsIndeterminate = false;
            loginBrower_loadingPanel.Visibility = Visibility.Collapsed;
            loginBrowser.Visibility = Visibility.Visible;
      
            //Check for the callback path here (or just check it against "/1/oauth/authorize")
            if (e.Uri.AbsolutePath == "/BoxShotLogin.htm")
            {
                loginBrower_loadingPanel.Visibility = Visibility.Visible;
                browserLoadingBar.IsIndeterminate = true;
                //The User has logged in!
                //Now to convert the Request Token into an Access Token
                App._client.GetAccessTokenAsync(response =>
                {
                    if (response != null)
                    {
                        //GREAT SUCCESS!
                        //Now we should save the Token and Secret so the user doesnt have to login next time
                        //response.Token;
                        //response.Secret;

                        DropboxUserToken = response.Token;
                        DropboxScretToken = response.Secret;

                        DropboxConnected = true;
                        browserLoadingBar.IsIndeterminate = false;
                        loginBrower_loadingPanel.Visibility = Visibility.Collapsed;
                        loginBrowser.Visibility = Visibility.Collapsed;

                        //NavigationService.Navigate(new Uri("/Page1.xaml", UriKind.Relative));
                        LoadDropBox("/");
                    }
                },
                    (error) =>
                    {
                        //OH DEAR GOD WHAT HAPPENED?!
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            //MessageBox.Show(error.Message);
                            popupmessage("Connection error");
                        });
                    });
            }
            else
            {
                //Probably the login page loading, ignore
                loginBrowser.Visibility = Visibility.Visible;
                browserLoadingBar.IsIndeterminate = true;
            }
        }

        private void LoadDropBox(String path)
        {
            lb_dropboxdata.Visibility = Visibility.Visible;
            dropboxloadingPanel.Visibility = Visibility.Visible;
            dropboxLoadingBar.IsIndeterminate = true;
            dropboxTitle.Visibility = Visibility.Visible; 

            lb_daownloaddata.Visibility = Visibility.Visible;
            downloadLoadingPanel.Visibility = Visibility.Visible;
            //downloadLoadingBar.IsIndeterminate = false;
            downloadingTitle.Visibility = Visibility.Visible;
            _dropboxdatalist.Clear();

            App._client.GetMetaDataAsync(path, (response) =>
            {
                //_model.Meta = response;
                //if (response != null)
                // {
                //     MessageBox.Show(response.ToString());

                // }
                //MessageBox.Show(response.Path.ToString());
                if (response != null)
                {
                    string[] paths = path.Split('/');
                    String prePath = "/";
                    for (int i = 0; i < paths.Length - 1; i++)
                    {
                        if (prePath != "/")
                        {
                            prePath += "/";
                        }
                        prePath += paths[i];
                    }

                    _dropboxdatalist.Add(new DropBoxDataList("../", prePath, true));
                    foreach (var c in response.Contents.OrderBy(f => f.Name).OrderByDescending(f => f.Is_Dir))
                    {

                        if (c.Is_Dir)
                        {
                            _dropboxdatalist.Add(new DropBoxDataList(c.Name, c.Path, c.Is_Dir));
                        }
                        else
                        {
                            //string Pattern = @"\.pdf";
                            // if (System.Text.RegularExpressions.Regex.IsMatch(c.Name, Pattern))
                            // {
                            _dropboxdatalist.Add(new DropBoxDataList(c.Name, c.Path, c.Is_Dir));
                            // }
                        }
                    }
                    dropboxLoadingBar.IsIndeterminate = false;
                    dropboxloadingPanel.Visibility = Visibility.Collapsed;
                }
            },
                (error) =>
                {
                    //OH DEAR GOD WHAT HAPPENED?!
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        //MessageBox.Show("DropBox Read Error");
                        popupmessage("DropBox Read Error");
                        LoadDropBox("/");
                    });
                });
        }

        private void lb_dropboxdata_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DropBoxDataList selectedBoxItem;
            if (lb_dropboxdata.SelectedItem != null)
            {
                selectedBoxItem = (DropBoxDataList)this.lb_dropboxdata.SelectedItem;

                if (selectedBoxItem.Is_Dir)
                {
                    LoadDropBox(selectedBoxItem.Path);
                }
                else
                {
                    SelectedFileDownload(selectedBoxItem);
                }
            }
        }

        private void SelectedFileDownload(DropBoxDataList selectedBoxItem)
        {
            DropBoxFileDownload dropdownload = new DropBoxFileDownload();

            String requesturl = dropdownload.getfiledownloadrequest(selectedBoxItem.Path, "meqy9y4nicqmr3k",
                                        "8g6858obthlzghw", DropboxUserToken, DropboxScretToken);

            
            //MessageBox.Show(requesturl);
            //다운로드 리스트에 추가
            if (_downlists.Count == 0)
            {
                downloadLoadingBar.IsIndeterminate = true;
                downloadLoadingPanel.Visibility = Visibility.Visible;
            }

            
            dpfile = false;
            //선택한 아이템이 이미 다운로드중인지 체크
            for (int i = 0; i < _downlists.Count; i++)
            {
                if (_downlists[i].Name == selectedBoxItem.Name)
                {
                    dpfile = true;
                    break;
                }
            }
            //이미 다운로드중이 아니면 추가하고 다운로드
            if (!dpfile)
            {
                _downlists.Add(new Downlist(selectedBoxItem.Name, 0));
                filedownload(requesturl);
            }

            //선택해제
            lb_dropboxdata.SelectedIndex = -1;
           // int _downlistsindex = _downlists.Count - 1;
           

            //MessageBox.Show(selectedBoxItem.Name);

            //파일 다운로드용 드롭박스 클라이언트 
            //드롭박스 접속용 초기화

            //DropNetClient _downclient = new DropNetClient("meqy9y4nicqmr3k", "8g6858obthlzghw");

            //_downclient.UserLogin = new UserLogin { Token = DropboxUserToken, Secret = DropboxScretToken };
            
           
            //_downclient.GetFileAsync(selectedBoxItem.Path,
            //    (response) =>
            //    {
            //        IsolatedStorageFile file;
            //        file = IsolatedStorageFile.GetUserStoreForApplication();
            //        string[] dirs = file.GetDirectoryNames("DownloadFiles");
            //        if (dirs.Length == 0)
            //        {
            //            file.CreateDirectory("DownloadFiles");
            //        }

            //        StreamWriter sw = new StreamWriter(new IsolatedStorageFileStream("DownloadFiles\\" + selectedBoxItem.Name, FileMode.Create, file));
            //        sw.WriteLine(response.Content);
            //        sw.Close();

            //        for (int i = 0; i < _downlists.Count; i++)
            //        {
            //            string Pattern = @selectedBoxItem.Name;
            //            Downlist downname = _downlists[i];
            //            if (System.Text.RegularExpressions.Regex.IsMatch(downname.Name, Pattern))
            //            {
            //                _downlists.RemoveAt(i);
            //                if (_downlists.Count == 0)
            //                {
            //                    downloadLoadingBar.IsIndeterminate = false;
            //                    downloadLoadingPanel.Visibility = Visibility.Collapsed;
            //                }
            //                break;
            //            }
            //        }

            //    },

            //    (error) =>
            //    {
            //        MessageBox.Show("File Download Fail");
            //        downloadLoadingBar.IsIndeterminate = false;
            //        downloadLoadingPanel.Visibility = Visibility.Collapsed;
            //        _downlists.Clear();
            //    });
        }

        private void filedownload(String requesturl)
        {
            var url = new Uri(requesturl, UriKind.Absolute);
            WebClient webClient = new WebClient();
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(webClient_DownloadProgressChanged);
            webClient.OpenReadCompleted += new OpenReadCompletedEventHandler(webClient_OpenReadCompleted);
            webClient.OpenReadAsync(url, url);
        }

        void webClient_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {

            var orgurl = e.UserState as Uri;

            string[] spliturls = Regex.Split(orgurl.LocalPath, "/");
            String filename = spliturls[spliturls.Length-2];
            //        IsolatedStorageFile file;
            //        file = IsolatedStorageFile.GetUserStoreForApplication();
            //        string[] dirs = file.GetDirectoryNames("DownloadFiles");
            //        if (dirs.Length == 0)
            //        {
            //            file.CreateDirectory("DownloadFiles");
            //        }

            //        StreamWriter sw = new StreamWriter(new IsolatedStorageFileStream("DownloadFiles\\" + selectedBoxItem.Name, FileMode.Create, file));
            //        sw.WriteLine(response.Content);
            //        sw.Close();


            try
            {
                var resInfo = new StreamResourceInfo(e.Result, null);
                var reader = new StreamReader(resInfo.Stream);
                byte[] contents;
                using (BinaryReader bReader = new BinaryReader(reader.BaseStream))
                {
                    contents = bReader.ReadBytes((int)reader.BaseStream.Length);
                }

                IsolatedStorageFile file;
                file = IsolatedStorageFile.GetUserStoreForApplication();
                string[] dirs = file.GetDirectoryNames("DownloadFiles");
                if (dirs.Length == 0)
                {
                    file.CreateDirectory("DownloadFiles");
                }

                if (contents[0] == 0xEF && contents[1] == 0xBB && contents[2] == 0xBF)
                {
                    // UTF-8
                    //MessageBox.Show("utf-8");
                    using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("DownloadFiles\\" + filename, FileMode.Create, file))
                    {
                        stream.Write(contents, 0, contents.Length);
                    }
                }
                else if (contents[0] == 0xFF && contents[1] == 0xFE)
                {
                    // UTF-16 Little Endian
                    //MessageBox.Show("utf-16");
                    using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("DownloadFiles\\" + filename, FileMode.Create, file))
                    {
                        stream.Write(contents, 0, contents.Length);
                    }
                }
                else if (contents[0] == 0xFE && contents[1] == 0xFF)
                {
                    // UTF-16 Big Endian 
                    //MessageBox.Show("utf-16 big");
                    using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("DownloadFiles\\" + filename, FileMode.Create, file))
                    {
                        stream.Write(contents, 0, contents.Length);
                    }
                }
                else if (contents[0] == 0 && contents[1] == 0 && contents[2] == 0xFE && contents[3] == 0xFF)
                {
                    // UTF-32 Big Endian
                    //MessageBox.Show("utf-32 big");
                    using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("DownloadFiles\\" + filename, FileMode.Create, file))
                    {
                        stream.Write(contents, 0, contents.Length);
                    }
                }
                else
                {
                    String convertstring = EUCKR_Unicode_Library.EUCKR_Unicode_Converter.GetUnicodeString(contents);
                    byte[] unicodecontents = System.Text.Encoding.UTF8.GetBytes(convertstring);
                    using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("DownloadFiles\\" + filename, FileMode.Create, file))
                    {
                        stream.Write(unicodecontents, 0, unicodecontents.Length);
                    }
                }

                int index = getItemIndexFromName(filename);
                if (index != -1)
                {
                    _downlists.RemoveAt(index);
                    if (_downlists.Count == 0)
                    {
                        downloadLoadingBar.IsIndeterminate = false;
                        downloadLoadingPanel.Visibility = Visibility.Collapsed;
                    }
                }

                try
                {
                    //셋팅 지우기
                    IsolatedStorageSettings.ApplicationSettings[filename] = null;
                    IsolatedStorageSettings.ApplicationSettings[filename + "readcount"] = null;
                }
                catch (KeyNotFoundException)
                {
                    //없으면 말고
                }
            }
            catch (Exception)
            {
                popupmessage(filename + ": Download Failed");
                int index = getItemIndexFromName(filename);
                if (index != -1)
                {
                    _downlists.RemoveAt(index);
                    if (_downlists.Count == 0)
                    {
                        downloadLoadingBar.IsIndeterminate = false;
                        downloadLoadingPanel.Visibility = Visibility.Collapsed;
                    }
                }
            }

           
        }

        void webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var orgurl = e.UserState as Uri;

            string[] spliturls = Regex.Split(orgurl.LocalPath, "/");
            String filename = spliturls[spliturls.Length - 2];
      
            int index = getItemIndexFromName(filename);
            if (index != -1)
            {
                _downlists[index].Progressbarval = e.BytesReceived * 100 / e.TotalBytesToReceive;
            }
        }

        private int getItemIndexFromName(String filename)
        {
            for (int i = 0; i < _downlists.Count; i++)
            {
                string Pattern = filename;
                Downlist downname = _downlists[i];
                if (System.Text.RegularExpressions.Regex.IsMatch(downname.Name, Pattern))
                {
                    return i;
                }
            }

            return -1;
        }



        public void popupmessage(String message)
        {
            poptext.Text = message;
            Thread pop = new Thread(popupmessage);
            pop.Start();
        }

        public void popupmessage()
        {
            Dispatcher.BeginInvoke(() =>
            {
                openani.Begin();
            });
            Thread.Sleep(3000);
            Dispatcher.BeginInvoke(() =>
            {
                closeani.Begin();
            });
        }

    }
}
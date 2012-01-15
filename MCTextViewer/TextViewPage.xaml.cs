using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Microsoft.Phone.Controls;
using System.Windows.Media;

namespace MCTextViewer
{

    public partial class TextViewPage : PhoneApplicationPage
    {

        private String fileName = "";
        private int textindex = 0;
        private List<string> pages = new List<string>();
        private int pagemove = 0;
        private double touchY = 0;
        private string totalString;
        private int readcount = 0;

        //기본 20
        private int textfontsize = 20;

        private bool isnewpage = false;
        private bool fontsizechanging = false;
        private bool textloading = false;

        private int LINEWIDTHSIZE;
        private int LINECOUNT;
        private bool FONTSIZECHANGED = false;

        private BackgroundWorker bw1 = new BackgroundWorker();
        private BackgroundWorker bw2 = new BackgroundWorker();

        
        public TextViewPage()
        {
            InitializeComponent();
            stackPanel1.Visibility = Visibility.Collapsed;
            textLoadingPanel.Visibility = Visibility.Visible;
            textLoadingbar.IsIndeterminate = true;
            isnewpage = true;
            Touch.FrameReported += new TouchFrameEventHandler(Touch_FrameReported);

            
        }

        void Touch_FrameReported(object sender, TouchFrameEventArgs e)
        {
            try
            {
                touchY = e.GetPrimaryTouchPoint(this).Position.Y;

            }
            catch (ArgumentException)
            {
                touchY = 0;
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            if (NavigationContext.QueryString.TryGetValue("data", out this.fileName))
            {
                pageTextName.Text = this.fileName.ToString();
                App._selectedtextfile = this.fileName.ToString();

                //화면 표시 글자수, 줄 수를 계산해놓음(폰트사이트 20기준, 한줄 46바이트, 28줄)
                try
                {
                    textviewblock.FontSize = (int)IsolatedStorageSettings.ApplicationSettings["Appsetting_fontsize"];
                }
                catch (Exception)
                {
                    textviewblock.FontSize = 20;
                }
                if ((int)textviewblock.FontSize <= 20)
                {
                    LINEWIDTHSIZE = 45 + (4 * (20 - (int)textviewblock.FontSize));
                    LINECOUNT = 28 + (2 * (20 - (int)textviewblock.FontSize));
                }
                else
                {
                    LINEWIDTHSIZE = 45 + (2 * (20 - (int)textviewblock.FontSize));
                    LINECOUNT = 28 + (1 * (20 - (int)textviewblock.FontSize));
                }

                //LINECOUNT = 28 + (2 * (20 - (int)textviewblock.FontSize));

                //셋팅 읽어옴
                readsetting();
                if (textfontsize != (int)textviewblock.FontSize)
                {
                    //폰트설정이 바뀜
                    readcount = 0;
                    FONTSIZECHANGED = true;
                }
                
            }
            else
            {
                MessageBox.Show("Read File error");
                //popupmessage("Read File error");
            }

            if (isnewpage)
            {
                isnewpage = false;
                makepage();
            }
        }

        private void readFile()
        {
            //저장장소파일
            IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication();
            string[] dirs = file.GetDirectoryNames("DownloadFiles");
            if (dirs.Length != 0)
            {
                try
                {
                    String readfile = "DownloadFiles\\" + this.fileName;
                    if (readcount > 0)
                    {
                        readfile = "DownloadFiles\\ResavedFiles\\" + this.fileName + "_resave";
                    }

                    StreamReader sr = new StreamReader(new IsolatedStorageFileStream(readfile, FileMode.Open, file), Encoding.UTF8);
                    totalString = sr.ReadToEnd();
                    sr.Close();
                }
                catch (IsolatedStorageException)
                {
                    //NavigationService.GoBack();
                    return;
                }
            }
        }

        public void makepage()
        {
            textLoadingbar.IsIndeterminate = false;
            textLoadingbar.Value = 0;
            textLoadingbar.Maximum = 100;
            //var bw = new BackgroundWorker();
            bw1.WorkerReportsProgress = true;
            bw1.WorkerSupportsCancellation = true;
            try { bw1.CancelAsync(); }
            catch (Exception) { }
            bw1.DoWork += (s, a) =>
            {
                textloading = true;
                //파일 읽어옴
                readFile();

                if (readcount > 0)
                {
                    //처음 읽는게 아님
                    int linecnt = 0;
                    string tmppage = "";
                    string[] lines = Regex.Split(totalString, "\n");
                    for (int i = 0; i < lines.Length; i++)
                    {
                        
                        tmppage += lines[i]+"\n";
                        linecnt++;

                        if (linecnt >= LINECOUNT)
                        {
                            pages.Add(tmppage);
                            tmppage = "";
                            linecnt = 0;
                        }
                        
                        bw1.ReportProgress(((i + 1) * 100) / lines.Length);
                    }
                    pages.Add(tmppage);
                }
                else
                {   //처음 읽었을때
                    fontsizechanging = true;

                    changeLineforFont(bw1);
                    
                }
                
            };
            bw1.RunWorkerCompleted += (s, a) =>
            {
                
                
                //페이지수가 결정됐으니까 슬라이더에 설정해놓음
                pageslider.Maximum = pages.Count;
                pageslider.Minimum = 1;

                //textLoadingbar.IsIndeterminate = false;
                //textLoadingPanel.Visibility = Visibility.Collapsed;

                


                //셋팅 읽어옴
                readsetting();

                //처음 보는거면 파일 다시 저장
                if (readcount == 0 || FONTSIZECHANGED)
                {
                    // Thread savenewfile = new Thread(resavefile);
                    ContentPanel.Background = BrushFromColorName("Black");
                    resavefile();
                    
                }
                else
                {
                    textLoadingbar.IsIndeterminate = false;
                    textLoadingPanel.Visibility = Visibility.Collapsed;
                    stackPanel1.Visibility = Visibility.Visible;
                    textloading = false;
                    readText();
                }

            };
            //textLoadingbar.IsIndeterminate = true;
            textLoadingPanel.Visibility = Visibility.Visible;

            bw1.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw1.RunWorkerAsync();
            IsolatedStorageSettings.ApplicationSettings[this.fileName + "textfontsize"] = (int)textviewblock.FontSize;
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //string loadingmsg = "Loading..";
            
            if (fontsizechanging && !App._filesaving)
            {
                textLoadingMessage.Text = "Font size change.." + e.ProgressPercentage.ToString() + "%";
            }
            
            textLoadingbar.Value = e.ProgressPercentage;
        }

        private void changeLineforFont(BackgroundWorker bw)
        {
            int bytecount = 0;
            int charcnt = 0;
            int stringIndex = 0;
            int linecnt = 0;
            string tmpstring = "";
            for (int i = 0; i < totalString.Length; i++)
            {
                string templine = "";
                int bytecnt = Encoding.UTF8.GetByteCount(totalString[i].ToString());
                if (bytecnt == 3)
                {
                    //한글인경우 2글자로 취급
                    bytecount += 2;
                }
                else
                {
                    bytecount++;
                }
                charcnt++;

                if (totalString[i].ToString() == "\n" || (bytecount / LINEWIDTHSIZE) > 0)
                {
                    try
                    {

                        templine = totalString.Substring(stringIndex, charcnt);
                        
                    }
                    catch (ArgumentOutOfRangeException)
                    {

                        templine = totalString.Substring(stringIndex, totalString.Length - stringIndex);
                    
                    }
                    stringIndex += charcnt;
                    tmpstring += templine;
                    if (totalString[i].ToString() != "\n")
                    {
                        tmpstring += "\n";
                    }
                    charcnt = 0;
                    bytecount = 0;
                    linecnt++;
                }
                if (linecnt >= LINECOUNT)
                {

                    pages.Add(tmpstring);
                    tmpstring = "";
                    linecnt = 0;
                }
                if(i%10000 == 0)
                bw.ReportProgress(((i + 1) * 100) / totalString.Length);
            }

            pages.Add(tmpstring+totalString.Substring(stringIndex, totalString.Length - stringIndex));
            //totalString = "";
            //string line;
            //string lineString = "";
            //int linecnt = 0;
            
            //string[] lines = Regex.Split(totalString, "\r\n");
            // totalString = "";
            //int cnt = 0;
            //int charcnt2 = 0;
            //int ccnt = 0;
            //int linecnt2 = 0;
            //string temp = "";
            //int stringIndex = 0;
            //while (true)
            //{
            //    string dddd = "";
            //    if (cnt >= totalString.Length)
            //    {
            //        pages.Add(temp);
            //        break;
            //    }

                
               
            //    int bytecnt = Encoding.UTF8.GetByteCount(totalString[cnt].ToString());
            //    if (bytecnt == 3)
            //    {
            //        //한글인경우 2글자로 취급
            //        charcnt2 += 2;

            //    }
            //    else
            //    {
            //        charcnt2++;
            //    }
            //    ccnt++; 
            //    if (totalString[cnt].ToString() == "\n")
            //    {
            //        try
            //        {

            //            dddd = totalString.Substring(stringIndex,  ccnt);
            //            int jj = 0;
            //        }
            //        catch (ArgumentOutOfRangeException)
            //        {

            //            dddd = totalString.Substring(stringIndex, totalString.Length - stringIndex);
            //            break;
            //        }
            //        stringIndex += ccnt;
            //        temp += dddd;
            //        ccnt = 0;
            //        charcnt2 = 0;
            //        linecnt2++;
            //    }
            //    else if ((charcnt2 / LINEWIDTHSIZE) > 0 )
            //    {

                        
            //        try
            //        {

            //            dddd = totalString.Substring(stringIndex, ccnt);
            //            int jj = 0;
            //        }
            //        catch (ArgumentOutOfRangeException)
            //        {
                            
            //            dddd = totalString.Substring(stringIndex, totalString.Length - stringIndex);
            //            break;
            //        }
            //        stringIndex += ccnt;
            //        temp += dddd;
            //        temp += "\n";
            //        charcnt2 = 0;
            //        ccnt = 0;
            //        linecnt2++;
            //    }
               
            //    if (linecnt2 >= LINECOUNT)
            //    {

            //        pages.Add(temp);
            //        temp = "";
            //        //int aa = totalString.Length / cnt;
            //        linecnt2 = 0;
            //    }
            //    cnt++;
                
               
            //}
            //return;

            //for (int i = 0; i < lines.Length; i++)
            //{
            //    int charcnt = 0;
            //    line = lines[i];
            //    for (int j = 0; j < line.Length; j++)
            //    {

            //        int bytecnt = Encoding.UTF8.GetByteCount(line[j].ToString());
            //        if (bytecnt == 3)
            //        {
            //            //한글인경우 2글자로 취급
            //            charcnt += 2;
            //        }
            //        else
            //        {
            //            charcnt++;
            //        }

            //        if ((charcnt / LINEWIDTHSIZE) > 0)
            //        {

            //            line = line.Insert(j + 1, "\r\n");
            //            charcnt = 0;
            //            j++;
            //        }

            //    }

            //    string[] tmplines = Regex.Split(line, "\r\n");
            //    //linecnt += tmplines.Length;
            //    for (int k = 0; k < tmplines.Length; k++)
            //    {
            //        lineString += tmplines[k] + "\r\n";
            //        //if (k == tmplines.Length - 1 && tmplines.Length > 1)
            //        //{
            //        //    //
            //        //}
            //        //else
            //        //{
            //        //    lineString += "\n";
            //        //}

            //        linecnt++;
            //        if (linecnt >= LINECOUNT)
            //        {
            //            pages.Add(lineString);
            //            linecnt = 0;
            //            lineString = "";
            //        }
            //    }

            //    if (linecnt >= LINECOUNT)
            //    {
            //        pages.Add(lineString);
            //        linecnt = 0;
            //        lineString = "";
            //    }
            //    bw.ReportProgress(((i + 1) * 100) / lines.Length);
            //}
            //pages.Add(lineString);
        }

        public void resavefile()
        {
            textloading = true;
            App._filesaving = true;
            //var bw = new BackgroundWorker();
            bw2.WorkerReportsProgress = true;
            bw2.WorkerSupportsCancellation = true;
            textLoadingMessage.Text = "File saving..";
            textLoadingbar.IsIndeterminate = false;
            textLoadingbar.Value = 0;
            textLoadingbar.Maximum = 100;
            
            try { bw2.CancelAsync(); }
            catch (Exception) { }
            bw2.DoWork += (s, a) =>
            {
                IsolatedStorageFile file;
                file = IsolatedStorageFile.GetUserStoreForApplication();

                string[] dirs = file.GetDirectoryNames("DownloadFiles\\ResavedFiles");
                if (dirs.Length == 0)
                {
                    file.CreateDirectory("DownloadFiles\\ResavedFiles");
                }
                try
                {
                    StreamWriter sw = new StreamWriter(new IsolatedStorageFileStream("DownloadFiles\\ResavedFiles\\" + this.fileName + "_resave", FileMode.Create, file));

                    String totalString = "";
                    for (int i = 0; i < pages.Count; i++)
                    {
                        totalString += pages[i].ToString();
                    
                        bw2.ReportProgress(((i + 1) * 100) / pages.Count);
                    }

                    sw.Write(totalString);
                    sw.Close();
                
                }
                catch (Exception)
                {
                }
            };

            bw2.RunWorkerCompleted += (s, a) =>
            {
                
                //MessageBox.Show("Resave completed");
                textLoadingbar.IsIndeterminate = false;
                textLoadingPanel.Visibility = Visibility.Collapsed;
                textviewblock.Visibility = Visibility.Visible;
                stackPanel1.Visibility = Visibility.Visible;
                textloading = false;
                fontsizechanging = false;
                App._filesaving = false;
                readText();
            };

            textviewblock.Visibility = Visibility.Collapsed;
            bw2.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw2.RunWorkerAsync();
            
        }

        //public void makepage()
        //{
        //    Dispatcher.BeginInvoke(() =>
        //    {
                
        //        //textviewblock.FontSize = 20;
        //        string line;
        //        string lineString = "";
        //        int linecnt = 0;
        //        string[] lines = Regex.Split(totalString, "\r\n");

        //        for(int i = 0; i<lines.Length; i++)
        //        {
        //            int charcnt = 0;
        //            line = lines[i];
        //            //int insertindex = 0;
        //            //int linebyte = Encoding.UTF8.GetByteCount(line);

        //            //if (linebyte > line.Length)
        //            //{
        //            //    //유니코드 문자열
        //            //    //insertindex = (44 + (4 * (20 - (int)textviewblock.FontSize)))/2;
        //            //    insertindex = 22;
        //            //}
        //            //else
        //            //{
        //            //    insertindex = 44;
        //            //}
        //            //for (int cnt = 1; insertindex*cnt <= line.Length; cnt ++)
        //            //{
        //            //    line = line.Insert(insertindex*cnt, "\n");
        //            //}
                    

        //            for (int j = 0; j < line.Length; j++)
        //            {

        //                int bytecnt = Encoding.UTF8.GetByteCount(line[j].ToString());
        //                if (bytecnt == 3)
        //                {
        //                    //한글인경우 2글자로 취급
        //                    charcnt += 2;
        //                }
        //                else
        //                {
        //                    charcnt++;
        //                }

        //                if ((charcnt / (44 + (4 * (20 - (int)textviewblock.FontSize))) > 0))
        //                {
        //                    line = line.Insert(j + 1, "\r\n");
        //                    charcnt = 0;
        //                    j++;
        //                }
        //            }

        //            string[] tmplines = Regex.Split(line, "\r\n");
        //            //linecnt += tmplines.Length;
        //            for (int k = 0; k < tmplines.Length; k++)
        //            {
        //                lineString += tmplines[k] + "\r\n";
        //                //if (k == tmplines.Length - 1 && tmplines.Length > 1)
        //                //{
        //                //    //
        //                //}
        //                //else
        //                //{
        //                //    lineString += "\n";
        //                //}

        //                linecnt++;
        //                if (linecnt >= (28 + (2 * (20 - (int)textviewblock.FontSize))))
        //                {
        //                    pages.Add(lineString);
        //                    linecnt = 0;
        //                    lineString = "";
        //                }
        //            }

        //            if (linecnt >= (28 + (2 * (20 - (int)textviewblock.FontSize))))
        //            {
        //                pages.Add(lineString);
        //                linecnt = 0;
        //                lineString = "";
        //            }

        //        }
        //        pages.Add(lineString);



        //        //int readsr = 0;

        //        //string totalString = sr.ReadToEnd();


        //        ////totalString = totalString.Replace("\n", "");
        //        ////totalString = totalString.Replace("  \r\n", "\r\n");
        //        ////totalString = totalString.Replace("\r\n", "");
        //        ////totalString = totalString.Replace("\r\n\r\n", "\r\n");
        //        //while (totalString.Contains("  \r\n"))
        //        //{
        //        //    totalString = totalString.Replace("  \r\n", "\r\n");
        //        //}
        //        //while (totalString.Contains(" \r\n"))
        //        //{
        //        //    totalString = totalString.Replace(" \r\n", "\r\n");
        //        //}
        //        //while (totalString.Contains("\r\n\r\n"))
        //        //{
        //        //    totalString = totalString.Replace("\r\n\r\n", "\r\n");
        //        //}



        //        //int stringIndex = 0;

        //        //while (true)
        //        //{
        //        //    char[] data = new char[400];

        //        //    try
        //        //    {
        //        //        totalString.CopyTo(stringIndex, data, 0, data.Length);


        //        //        pages.Add(new String(data));
        //        //        stringIndex += 400;
        //        //        if (stringIndex > totalString.Length)
        //        //        {
        //        //            break;
        //        //        }
        //        //    }
        //        //    catch (ArgumentOutOfRangeException)
        //        //    {
        //        //        totalString.CopyTo(stringIndex, data, 0, totalString.Length - stringIndex);
        //        //        pages.Add(new String(data));
        //        //        break;
        //        //    }

        //        //}


        //        /////////////////
        //        //do
        //        //{

        //        //    readsr = sr.ReadBlock(data, 0, data.Length);
        //        //    pages.Add(new String(data));

        //        //}
        //        //while (readsr > 0);
        //        ////sr.ReadBlock(data, textindex, 450);

        //        ////페이지수가 결정됐으니까 슬라이더에 설정해놓음
        //        //pageslider.Maximum = pages.Count;
        //        //pageslider.Minimum = 1;

        //        ////textLoadingbar.IsIndeterminate = false;
        //        ////textLoadingPanel.Visibility = Visibility.Collapsed;

        //        //stackPanel1.Visibility = Visibility.Visible;

        //        ////마지막에 봤던 페이지 있나
        //        //readsetting();

        //        //readText();
     
               
        //    });
        //}

        //public static byte[] StringToAscii(string s)
        //{
        //    byte[] retval = new byte[s.Length];
        //    for (int ix = 0; ix < s.Length; ++ix)
        //    {
        //        char ch = s[ix];
        //        if (ch <= 0x7f) retval[ix] = (byte)ch;
        //        else retval[ix] = (byte)'?';
        //    }
        //    return retval;
        //}

        private void readsetting()
        {
            try
            {
                readcount = (int)IsolatedStorageSettings.ApplicationSettings[this.fileName + "readcount"];
                textindex = (int)IsolatedStorageSettings.ApplicationSettings[this.fileName];
                textfontsize = (int)IsolatedStorageSettings.ApplicationSettings[this.fileName + "textfontsize"];
                
                    
                

            }
            catch (Exception)
            {
                textindex = 0;
            }

        }

        private void readText()
        {
            try
            {
                ContentPanel.Background = BrushFromColorName(IsolatedStorageSettings.ApplicationSettings["Appsetting_backgroundcolor"].ToString());
                textviewblock.Foreground = BrushFromColorName(IsolatedStorageSettings.ApplicationSettings["Appsetting_fontcolor"].ToString());
            }
            catch (Exception)
            {
            }
            if (pages.Count > textindex && textindex >= 0)
            {
                textviewblock.Text = pages[textindex].ToString();
                setpagevalue();

                if (textindex != 0)
                {
                    IsolatedStorageSettings.ApplicationSettings[this.fileName] = textindex;
                }
            }
        }

        private void TextBlock_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (e.DeltaManipulation.Translation.Y < 0)
            {
                //MessageBox.Show("scroll down");
                pagemove = 1;
               
            }

            if (e.DeltaManipulation.Translation.Y > 0)
            {
                pagemove = -1;
                //MessageBox.Show("scroll up");
                
            }

        }

        private void textviewblock_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (pagemove == 1)
            {
                pageDown();
            }
            else if(pagemove == -1)
            {
                pageUp();
            }

            pagemove = 0;
        }

        private void pageDown()
        {
            if (textindex < pages.Count)
            {
                textindex++;
                readText();
            }
        }

        private void pageUp()
        {
            if (textindex > 0)
            {
                textindex--;
                readText();
            }
        }

        private void textviewblock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
           
        }

        private void ContentPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
           
        }

        private void LayoutRoot_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //로딩중이면 무시
            if (textloading) return;

            if (stackPanel1.Visibility == Visibility.Visible)
            {
                menudownani.Begin();
                menudownani.Completed += new EventHandler(menudownani_Completed);
                return;
            }

            if (touchY < LayoutRoot.ActualHeight / 3)
            {
                pageUp();
            }
            else if (touchY > (LayoutRoot.ActualHeight / 3) * 2)
            {
                pageDown();
            }
            else
            { 
                if (stackPanel1.Visibility == Visibility.Collapsed)
                {
                    //setpagevalue();
                    stackPanel1.Visibility = Visibility.Visible;
                    menuupani.Begin();
                }
            }
        }

        void setpagevalue()
        {
            int tmpindex = textindex + 1;
            pageTextblock.Text = tmpindex.ToString() + "/" + pages.Count.ToString();
            pageslider.Value = tmpindex;
        }

        void menudownani_Completed(object sender, EventArgs e)
        {
            stackPanel1.Visibility = Visibility.Collapsed;
        }

        private void LayoutRoot_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!fontsizechanging)
            {
                IsolatedStorageSettings.ApplicationSettings["LAST_VIEW_TEXT"] = this.fileName;
                IsolatedStorageSettings.ApplicationSettings[this.fileName] = textindex;
                IsolatedStorageSettings.ApplicationSettings[this.fileName + "readcount"] = ++readcount;
                IsolatedStorageSettings.ApplicationSettings[this.fileName + "textfontsize"] = (int)textviewblock.FontSize;
            }
            else
            {
                //MessageBox.Show("!");
                IsolatedStorageSettings.ApplicationSettings["LAST_VIEW_TEXT"] = this.fileName;
                IsolatedStorageSettings.ApplicationSettings[this.fileName + "readcount"] = 0;

                try { bw1.CancelAsync(); }
                catch (Exception) { }
                try { bw2.CancelAsync(); }
                catch (Exception) { }
               
            }


            App._selectedtextfile = "";
            
            Touch.FrameReported -= new TouchFrameEventHandler(Touch_FrameReported);
            isnewpage = false;
        }

        private void pageslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //slider value changed
            textindex = (int)pageslider.Value -1;
            readText();
        }

        private void btnclosetext_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, CancelEventArgs e)
        {
            try { bw1.CancelAsync(); }
            catch (Exception) { }
            try { bw2.CancelAsync(); }
            catch (Exception) { }
        }

        public SolidColorBrush BrushFromColorName(string colorName)
        {
            string s;
            s = "<SolidColorBrush xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'";
            s += " Color='" + colorName + "'/>";

            return System.Windows.Markup.XamlReader.Load(s) as SolidColorBrush;
        } 
       

 
    }
}
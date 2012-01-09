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
using System.IO.IsolatedStorage;
using System.IO;
using System.Text;
using EUCKR_Unicode_Library;
using System.Text.RegularExpressions;
using System.Threading;
using System.ComponentModel;

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

        private bool isnewpage = false;
        private bool firstloading = false;

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

                //저장장소파일
                IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication();
                
                string[] dirs = file.GetDirectoryNames("DownloadFiles");
                if (dirs.Length != 0)
                {
                    try
                    {
                        

                        String readfile = "DownloadFiles\\" + this.fileName;

                        StreamReader sr = new StreamReader(new IsolatedStorageFileStream(readfile, FileMode.Open, file), Encoding.UTF8);
                        totalString = sr.ReadToEnd();
                        sr.Close();

                        
                    }
                    catch (IsolatedStorageException)
                    {
                        NavigationService.GoBack();
                        return;
                    }
                }
            }
            else
            {
                MessageBox.Show("Fail: Delete File");
                //popupmessage("Read File error");
            }

            if (isnewpage)
            {
                isnewpage = false;
                makepage();
            }
        }
        public void makepage()
        {
            textLoadingbar.IsIndeterminate = false;
            textLoadingbar.Value = 0;
            textLoadingbar.Maximum = 100;
            var bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += (s, a) =>
            {
                //셋팅 읽어옴
                readsetting();
                if (readcount > 0)
                {
                    //처음 읽는게 아님
                    int linecnt = 0;
                    string tmppage = "";
                    string[] lines = Regex.Split(totalString, "\r\n");
                    for (int i = 0; i < lines.Length; i++)
                    {
                        tmppage += lines[i]+"\n";
                        linecnt++;

                        if (linecnt >= 28)
                        {
                            pages.Add(tmppage);
                            tmppage = "";
                            linecnt = 0;
                        }
                        
                        bw.ReportProgress(((i + 1) * 100) / lines.Length);
                    }
                    pages.Add(tmppage);
                }
                else
                {   //처음 읽었을때
                    firstloading = true;
                    //textviewblock.FontSize = 20;
                    string line;
                    string lineString = "";
                    int linecnt = 0;
                    Dispatcher.BeginInvoke(() =>
                    {
                        textLoadingMessage.Text = "First Loading..";
                    });
                    string[] lines = Regex.Split(totalString, "\r\n");
                    totalString = "";

                    for (int i = 0; i < lines.Length; i++)
                    {
                        int charcnt = 0;
                        line = lines[i];
                        for (int j = 0; j < line.Length; j++)
                        {

                            int bytecnt = Encoding.UTF8.GetByteCount(line[j].ToString());
                            if (bytecnt == 3)
                            {
                                //한글인경우 2글자로 취급
                                charcnt += 2;
                            }
                            else
                            {
                                charcnt++;
                            }

                            if ((charcnt / 44) > 0)
                            {

                                line = line.Insert(j + 1, "\r\n");
                                charcnt = 0;
                                j++;
                            }

                        }

                        string[] tmplines = Regex.Split(line, "\r\n");
                        //linecnt += tmplines.Length;
                        for (int k = 0; k < tmplines.Length; k++)
                        {
                            lineString += tmplines[k] + "\r\n";
                            //if (k == tmplines.Length - 1 && tmplines.Length > 1)
                            //{
                            //    //
                            //}
                            //else
                            //{
                            //    lineString += "\n";
                            //}

                            linecnt++;
                            if (linecnt >= 28)
                            {
                                pages.Add(lineString);
                                linecnt = 0;
                                lineString = "";
                            }
                        }

                        if (linecnt >= 28)
                        {
                            pages.Add(lineString);
                            linecnt = 0;
                            lineString = "";
                        }
                        bw.ReportProgress(((i + 1) * 100) / lines.Length);
                    }
                    pages.Add(lineString);
                    firstloading = false;
                }
            };
            bw.RunWorkerCompleted += (s, a) =>
            {
                
                
                //페이지수가 결정됐으니까 슬라이더에 설정해놓음
                pageslider.Maximum = pages.Count;
                pageslider.Minimum = 1;

                //textLoadingbar.IsIndeterminate = false;
                //textLoadingPanel.Visibility = Visibility.Collapsed;

                


                //셋팅 읽어옴
                readsetting();

                //처음 보는거면 파일 다시 저장
                if (readcount == 0)
                {
                    // Thread savenewfile = new Thread(resavefile);
                    resavefile();
                }
                else
                {
                    textLoadingbar.IsIndeterminate = false;
                    textLoadingPanel.Visibility = Visibility.Collapsed;
                    stackPanel1.Visibility = Visibility.Visible;
                    readText();
                }

                
     

            };
            //textLoadingbar.IsIndeterminate = true;
            textLoadingPanel.Visibility = Visibility.Visible;

            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerAsync();
            
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //string loadingmsg = "Loading..";
            if (firstloading)
            {
                
                textLoadingMessage.Text = "First Loading.." + e.ProgressPercentage.ToString() + "%";
            }

            textLoadingbar.Value = e.ProgressPercentage;
        }

        public void resavefile()
        {
            var bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            textLoadingMessage.Text = "File saving..";
            textLoadingbar.IsIndeterminate = false;
            textLoadingbar.Value = 0;
            textLoadingbar.Maximum = 100;

            bw.DoWork += (s, a) =>
            {
                IsolatedStorageFile file;
                file = IsolatedStorageFile.GetUserStoreForApplication();
                StreamWriter sw = new StreamWriter(new IsolatedStorageFileStream("DownloadFiles\\" + this.fileName, FileMode.Create, file));
                String totalString = "";

                for (int i = 0; i < pages.Count; i++)
                {
                    totalString += pages[i].ToString();
                    bw.ReportProgress(((i + 1) * 100) / pages.Count);
                }

                sw.Write(totalString);
                sw.Close();
            };

            bw.RunWorkerCompleted += (s, a) =>
            {
                //MessageBox.Show("Resave completed");
                textLoadingbar.IsIndeterminate = false;
                textLoadingPanel.Visibility = Visibility.Collapsed;
                textviewblock.Visibility = Visibility.Visible;
                stackPanel1.Visibility = Visibility.Visible;
                readText();
            };

            textviewblock.Visibility = Visibility.Collapsed;
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerAsync();
            
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
                
            }
            catch (Exception)
            {
                textindex = 0;
            }

        }

        private void readText()
        {
            if (pages.Count > textindex && textindex >= 0)
            {
                textviewblock.Text = pages[textindex].ToString();
                setpagevalue();
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

        private void textviewblock_Tap(object sender, GestureEventArgs e)
        {
           
        }

        private void ContentPanel_Tap(object sender, GestureEventArgs e)
        {
           
        }

        private void LayoutRoot_Tap(object sender, GestureEventArgs e)
        {
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
            if (!firstloading)
            {
                IsolatedStorageSettings.ApplicationSettings["LAST_VIEW_TEXT"] = this.fileName;
                IsolatedStorageSettings.ApplicationSettings[this.fileName] = textindex;
                IsolatedStorageSettings.ApplicationSettings[this.fileName + "readcount"] = ++readcount;
            }
           
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

    

 
    }
}
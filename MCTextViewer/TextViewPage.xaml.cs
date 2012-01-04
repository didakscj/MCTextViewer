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


namespace MCTextViewer
{

    public partial class TextViewPage : PhoneApplicationPage
    {

        private String fileName = "";
        private int textindex = 0;
        private List<string> pages = new List<string>();
        private int pagemove = 0;
        private double touchY = 0;

        public TextViewPage()
        {
            InitializeComponent();
            Touch.FrameReported += new TouchFrameEventHandler(Touch_FrameReported);
        }

        void Touch_FrameReported(object sender, TouchFrameEventArgs e)
        {
            touchY = e.GetPrimaryTouchPoint(this).Position.Y;
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
                   
                    
                        //int readsr = 0;
                    
                        String totalString = sr.ReadToEnd();
                        int stringIndex = 0;
                       
                        while (true)
                        {
                            char[] data = new char[400];
                             
                            try
                            {
                                totalString.CopyTo(stringIndex, data, 0, data.Length);
                           

                                pages.Add(new String(data));
                                stringIndex += 400;
                                if (stringIndex > totalString.Length)
                                {
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                totalString.CopyTo(stringIndex, data, 0, totalString.Length-stringIndex);
                                pages.Add(new String(data));
                                break;
                            }

                        }

                    
                    
                        //do
                        //{
                        
                        //    readsr = sr.ReadBlock(data, 0, data.Length);
                        //    pages.Add(new String(data));
                       
                        //}
                        //while (readsr > 0);
                        ////sr.ReadBlock(data, textindex, 450);
                        sr.Close();

                        //페이지수가 결정됐으니까 슬라이더에 설정해놓음
                        pageslider.Maximum = pages.Count;
                        pageslider.Minimum = 1;

                        //마지막에 봤던 페이지 있나
                        readsetting();

                        readText();
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
        }

        

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
            IsolatedStorageSettings.ApplicationSettings["LAST_VIEW_TEXT"] = this.fileName;
            IsolatedStorageSettings.ApplicationSettings[this.fileName] = textindex;

            Touch.FrameReported -= new TouchFrameEventHandler(Touch_FrameReported);
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
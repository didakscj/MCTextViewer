using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MCTextViewer.ViewModels
{
    public class LibraryDataList
    {
        public string Name { get; set; }
        public string Accessdate { get; set; }
        public string ImagePath { get; set; }
        public LibraryDataList(string name, string accessdate, string imagepath)
        {
            this.Name = name;
            this.Accessdate = accessdate;
            this.ImagePath = "/MCTextViewer;component/Images/" + imagepath;
        }
    }
}


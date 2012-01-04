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
    public class DropBoxDataList
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool Is_Dir { get; set; }

        public DropBoxDataList(string name, String path, bool is_dir)
        {
            this.Name = name;
            this.Path = path;
            this.Is_Dir = is_dir;
        }
    }
}

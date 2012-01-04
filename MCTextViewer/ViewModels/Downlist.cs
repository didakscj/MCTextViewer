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
using System.ComponentModel;

namespace MCTextViewer.ViewModels
{
    public class Downlist:INotifyPropertyChanged
    {
        public string Name { get; set; }

        private double _progressbarval;
        public double Progressbarval 
        {
            get
            {
                return _progressbarval;
            }
            set
            {
                if(value != _progressbarval)
                {
                    _progressbarval = value;
                    NotifyPropertyChanged("Progressbarval");
                }
            }
        }


        public Downlist(string name, double progressbarval)
        {
            this.Name = name;
            this.Progressbarval = progressbarval;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if(null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }


    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Xamarin.Forms;

namespace BluetoothLETest.ViewModels
{
    public class PowerViewModel : INotifyPropertyChanged
    {
        private string statusText = "OFF";


        public string Status
        {
            get { return statusText; }
            set
            {
                statusText = value;
                OnPropertyChanged();
            }
        }
        public Command DisplayStatus
        {
            get
            {
                return new Command(() =>
                {
                    Status = "On";
                });
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string caller = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }
    }
}

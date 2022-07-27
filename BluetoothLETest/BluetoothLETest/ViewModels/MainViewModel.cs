using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Xamarin.Forms;

namespace BluetoothLETest.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string labelText = "Hello";

        public string LabelText
        {
            get { return labelText; }
            set
            {
                labelText = value;
                RaisePropertyChanged();
            }
        }
        public MainViewModel()
        {
        }
        private Command changeTextCommand { get; set; }
        public Command ChangeTextCommand
        {
            get
            {
                return changeTextCommand ?? (changeTextCommand = new Command(() => { LabelText = "GoddBye"; }));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string caller = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }

    }
}

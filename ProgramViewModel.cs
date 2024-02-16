using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HoldKey
{
    public class ProgramViewModel : INotifyPropertyChanged
    {
        public string WindowTitle { get { return $"Hold Key v{Assembly.GetExecutingAssembly().GetName().Version}"; } }

        private bool isSoundEnabled = false;
        public bool IsSoundEnabled
        {
            get { return this.isSoundEnabled; }
            set
            {
                this.isSoundEnabled = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged("ButtonImage");
            }
        }

        public Image ButtonImage
        {
            get
            {
                return this.IsSoundEnabled ? Properties.Resources.fa_mute_off : Properties.Resources.fa_mute_on;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

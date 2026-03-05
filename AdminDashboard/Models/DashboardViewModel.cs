using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdminDashboard.Models
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private float _downloadSpeed;
        private float _uploadSpeed;
        private int _standardClients;
        private int _adminClients;

        public float DownloadSpeed
        {
            get => _downloadSpeed;
            set { _downloadSpeed = value; OnPropertyChanged(); }
        }

        public float UploadSpeed
        {
            get => _uploadSpeed;
            set { _uploadSpeed = value; OnPropertyChanged(); }
        }

        public int StandardClients
        {
            get => _standardClients;
            set { _standardClients = value; OnPropertyChanged(); }
        }

        public int AdminClients
        {
            get => _adminClients;
            set { _adminClients = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
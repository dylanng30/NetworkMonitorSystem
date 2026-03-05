using System.Windows;
using AdminDashboard.Controllers;
using AdminDashboard.Models;
using SharedLibrary.Models;

namespace AdminDashboard
{
    public partial class MainWindow : Window
    {
        private AdminClientController _controller;
        private DashboardViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // Khởi tạo ViewModel và gán DataContext cho UI Binding
            _viewModel = new DashboardViewModel();
            this.DataContext = _viewModel;

            // Khởi tạo Controller
            _controller = new AdminClientController();

            // Đăng ký sự kiện: Khi Controller nhận data, update ViewModel
            _controller.OnMetricsReceived += UpdateDashboard;

            // Kết nối đến Server ở IP local, port 8888 (Phải chạy MonitorServer trước)
            _controller.Connect("127.0.0.1", 8888);
        }

        private void UpdateDashboard(NetworkMetrics metrics)
        {
            // Vì sự kiện được gọi từ Thread mạng ngầm (Background Thread), 
            // ta phải dùng Dispatcher để đưa thao tác cập nhật UI về UI Thread chính
            Application.Current.Dispatcher.Invoke(() =>
            {
                _viewModel.DownloadSpeed = metrics.DownloadSpeedKbps;
                _viewModel.UploadSpeed = metrics.UploadSpeedKbps;
                _viewModel.StandardClients = metrics.ActiveStandardClients;
                _viewModel.AdminClients = metrics.ActiveAdminClients;
            });
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // Ngắt kết nối socket an toàn khi tắt app
            _controller.Disconnect();
        }
    }
}
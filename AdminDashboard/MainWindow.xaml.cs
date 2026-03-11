using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
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

            _viewModel = new DashboardViewModel();
            this.DataContext = _viewModel;

            _controller = new AdminClientController();

            // 1. Đăng ký nhận dữ liệu theo thời gian thực (Clients)
            _controller.OnMetricsReceived += UpdateDashboard;

            // 2. Đăng ký nhận dữ liệu Cảnh báo (Alerts)
            _controller.OnAlertsReceived += UpdateAlerts;

            // 3. Đăng ký nhận dữ liệu Nhật ký kết nối (Logs)
            _controller.OnConnectionLogsReceived += UpdateLogs;

            _viewModel.AddEventLog("Hệ thống khởi động. Đang kết nối tới Monitor Server...");
            _controller.Connect();
            _viewModel.AddEventLog("Đã kết nối thành công. Đang chờ dữ liệu mạng...");

            // Tùy chọn: Tự động yêu cầu load dữ liệu Alerts và Logs ngay khi vừa khởi động UI
            // _controller.RequestAlertsData();
            // _controller.RequestConnectionLogsData();
        }

        private void UpdateDashboard(List<ClientNetworkInfo> clients)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _viewModel.UpdateData(clients);
            });
        }

        private void UpdateAlerts(List<AlertInfo> alerts)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _viewModel.UpdateAlerts(alerts);
            });
        }

        private void UpdateLogs(List<ConnectionLogInfo> logs)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _viewModel.UpdateLogs(logs);
            });
        }

        private void DgClients_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DgClients.SelectedItem is ClientNetworkInfo selectedClient)
            {
                string details = $"THÔNG TIN CHI TIẾT NODE MẠNG\n" +
                                 $"---------------------------\n" +
                                 $"ID Thiết Bị: {selectedClient.ClientId}\n" +
                                 $"IP Address : {selectedClient.IpAddress}:{selectedClient.Port}\n" +
                                 $"Tải Xuống  : {selectedClient.DownloadSpeedKbps:F2} KB/s\n" +
                                 $"Tải Lên    : {selectedClient.UploadSpeedKbps:F2} KB/s\n" +
                                 $"Cập nhật lúc: {selectedClient.LastUpdated:HH:mm:ss}";

                MessageBox.Show(details, "Drill-down: " + selectedClient.ClientId, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _viewModel.AddEventLog("Ngắt kết nối từ Server. Đóng ứng dụng.");
            _controller.Disconnect();
        }

        private void BtnRefreshAlerts_Click(object sender, RoutedEventArgs e)
        {
            _controller.RequestAlertsData();
        }

        private void BtnRefreshLogs_Click(object sender, RoutedEventArgs e)
        {
            _controller.RequestConnectionLogsData();
        }
    }
}
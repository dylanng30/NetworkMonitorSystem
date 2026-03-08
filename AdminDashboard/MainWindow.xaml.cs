using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using AdminDashboard.Controllers;
using SharedLibrary.Models;

namespace AdminDashboard
{
    public partial class MainWindow : Window
    {
        private AdminClientController _controller;

        // Dùng ObservableCollection để Binding UI tự động cập nhật
        private ObservableCollection<ClientNetworkInfo> _clientsData;

        public MainWindow()
        {
            InitializeComponent();

            _clientsData = new ObservableCollection<ClientNetworkInfo>();
            DgClients.ItemsSource = _clientsData;

            _controller = new AdminClientController();
            _controller.OnMetricsReceived += UpdateDashboard;

            // Kết nối Server sử dụng thông tin trong file Constants thay vì hardcode
            _controller.Connect();
        }

        private void UpdateDashboard(List<ClientNetworkInfo> clients)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                TxtTotalClients.Text = $"Tổng số Standard Clients đang hoạt động: {clients.Count}";

                _clientsData.Clear();
                foreach (var client in clients)
                {
                    _clientsData.Add(client);
                }
            });
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _controller.Disconnect();
        }
    }
}
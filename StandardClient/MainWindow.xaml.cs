using System;
using System.Windows;
using StandardClient.Controllers;

namespace StandardClient
{
    public partial class MainWindow : Window
    {
        private StandardClientController _controller;

        public MainWindow()
        {
            InitializeComponent();
            _controller = new StandardClientController();
            _controller.OnStatusChanged += UpdateStatus;
            _controller.OnSpeedUpdated += UpdateSpeed;
        }

        private void UpdateStatus(string message) => Dispatcher.Invoke(() => TxtStatus.Text = $"Trạng thái: {message}");

        private void UpdateSpeed(double download, double upload) => Dispatcher.Invoke(() =>
            TxtSpeed.Text = $"Download: {download:F2} KB/s | Upload: {upload:F2} KB/s");

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            _controller.Connect();
            BtnConnect.IsEnabled = false;
            BtnDisconnect.IsEnabled = true;
        }

        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _controller.Disconnect();
            BtnConnect.IsEnabled = true;
            BtnDisconnect.IsEnabled = false;
        }

        private void Window_Closed(object sender, EventArgs e) => _controller.Disconnect();
    }
}
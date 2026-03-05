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

            // Lắng nghe sự kiện thay đổi trạng thái từ Controller để update text
            _controller.OnStatusChanged += UpdateStatus;
        }

        private void UpdateStatus(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                TxtStatus.Text = $"Trạng thái: {message}";
            });
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            _controller.Connect("127.0.0.1", 8888);

            BtnConnect.IsEnabled = false;
            BtnDisconnect.IsEnabled = true;
            BtnStartTraffic.IsEnabled = true;
        }

        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _controller.Disconnect();

            BtnConnect.IsEnabled = true;
            BtnDisconnect.IsEnabled = false;
            BtnStartTraffic.IsEnabled = false;
            BtnStopTraffic.IsEnabled = false;
        }

        private void BtnStartTraffic_Click(object sender, RoutedEventArgs e)
        {
            // Truyền tham số tốc độ (càng cao gửi càng nhanh, ví dụ truyền 10)
            _controller.StartTrafficGenerator(10);

            BtnStartTraffic.IsEnabled = false;
            BtnStopTraffic.IsEnabled = true;
        }

        private void BtnStopTraffic_Click(object sender, RoutedEventArgs e)
        {
            _controller.StopTrafficGenerator();

            BtnStartTraffic.IsEnabled = true;
            BtnStopTraffic.IsEnabled = false;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _controller.Disconnect();
        }
    }
}
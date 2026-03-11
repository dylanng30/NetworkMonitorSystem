using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using SharedLibrary.Models;

namespace AdminDashboard.Models
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private double _totalDownloadSpeed;
        private double _totalUploadSpeed;
        private int _activeClientsCount;
        private int _alertCount;

        // Các chỉ số thẻ Tổng quan
        public double TotalDownloadSpeed { get => _totalDownloadSpeed; set { _totalDownloadSpeed = value; OnPropertyChanged(); } }
        public double TotalUploadSpeed { get => _totalUploadSpeed; set { _totalUploadSpeed = value; OnPropertyChanged(); } }
        public int ActiveClientsCount { get => _activeClientsCount; set { _activeClientsCount = value; OnPropertyChanged(); } }
        public int AlertCount { get => _alertCount; set { _alertCount = value; OnPropertyChanged(); } }

        public ObservableCollection<ClientNetworkInfo> Clients { get; set; } = new ObservableCollection<ClientNetworkInfo>();
        public ObservableCollection<string> EventLogs { get; set; } = new ObservableCollection<string>();

        // [THÊM MỚI] Danh sách lưu trữ Alerts và Logs cho UI
        public ObservableCollection<AlertInfo> Alerts { get; set; } = new ObservableCollection<AlertInfo>();
        public ObservableCollection<ConnectionLogInfo> Logs { get; set; } = new ObservableCollection<ConnectionLogInfo>();

        // --- CHART PROPERTIES ---
        private const int MaxPoints = 60;
        private Queue<double> _downloadHistory = new Queue<double>();
        private Queue<double> _uploadHistory = new Queue<double>();

        public PointCollection DownloadPoints { get; set; } = new PointCollection();
        public PointCollection UploadPoints { get; set; } = new PointCollection();

        public void UpdateData(List<ClientNetworkInfo> newClients)
        {
            Clients.Clear();
            double tempDown = 0;
            double tempUp = 0;
            int currentAlerts = 0;

            foreach (var client in newClients)
            {
                Clients.Add(client);
                tempDown += client.DownloadSpeedKbps;
                tempUp += client.UploadSpeedKbps;

                if (client.DownloadSpeedKbps > 8000 || client.UploadSpeedKbps > 8000)
                {
                    currentAlerts++;
                    AddEventLog($"[CẢNH BÁO] Client {client.ClientId} băng thông bất thường!");
                }
            }

            TotalDownloadSpeed = tempDown;
            TotalUploadSpeed = tempUp;
            ActiveClientsCount = newClients.Count;
            AlertCount = currentAlerts;

            // Cập nhật biểu đồ
            UpdateCharts(tempDown, tempUp);
        }

        // [THÊM MỚI] Hàm cập nhật Alerts
        public void UpdateAlerts(List<AlertInfo> newAlerts)
        {
            Alerts.Clear();
            foreach (var alert in newAlerts)
            {
                Alerts.Add(alert);
            }
        }

        // [THÊM MỚI] Hàm cập nhật Logs
        public void UpdateLogs(List<ConnectionLogInfo> newLogs)
        {
            Logs.Clear();
            foreach (var log in newLogs)
            {
                Logs.Add(log);
            }
        }

        private void UpdateCharts(double currentDown, double currentUp)
        {
            // Cập nhật hàng đợi
            _downloadHistory.Enqueue(currentDown);
            if (_downloadHistory.Count > MaxPoints) _downloadHistory.Dequeue();

            _uploadHistory.Enqueue(currentUp);
            if (_uploadHistory.Count > MaxPoints) _uploadHistory.Dequeue();

            // Render tọa độ (Giả sử Canvas cao 100px, rộng 400px)
            double canvasWidth = 400;
            double canvasHeight = 100;
            double maxSpeed = Math.Max(1000, Math.Max(_downloadHistory.Max(), _uploadHistory.Max()));

            PointCollection dlPoints = new PointCollection();
            PointCollection ulPoints = new PointCollection();

            int i = 0;
            foreach (var dl in _downloadHistory)
            {
                double x = i * (canvasWidth / (MaxPoints - 1));
                double y = canvasHeight - ((dl / maxSpeed) * canvasHeight);
                dlPoints.Add(new Point(x, y));
                i++;
            }

            i = 0;
            foreach (var ul in _uploadHistory)
            {
                double x = i * (canvasWidth / (MaxPoints - 1));
                double y = canvasHeight - ((ul / maxSpeed) * canvasHeight);
                ulPoints.Add(new Point(x, y));
                i++;
            }

            DownloadPoints = dlPoints;
            UploadPoints = ulPoints;

            OnPropertyChanged(nameof(DownloadPoints));
            OnPropertyChanged(nameof(UploadPoints));
        }

        public void AddEventLog(string message)
        {
            var timeStr = DateTime.Now.ToString("HH:mm:ss");
            EventLogs.Insert(0, $"[{timeStr}] {message}");
            if (EventLogs.Count > 50) EventLogs.RemoveAt(EventLogs.Count - 1);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using SharedLibrary.Models;

namespace MonitorServer.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString = "Server=localhost\\SQLEXPRESS;Database=NetworkMonitor;Integrated Security=True;TrustServerCertificate=True;";

        public List<AlertInfo> GetRecentAlerts(int limit = 50)
        {
            var alerts = new List<AlertInfo>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT TOP (@Limit) Id, ClientId, AlertType, Message, CreatedAt FROM Alerts ORDER BY CreatedAt DESC";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Limit", limit);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        alerts.Add(new AlertInfo
                        {
                            Id = (long)reader["Id"],
                            ClientId = reader["ClientId"].ToString(),
                            AlertType = reader["AlertType"].ToString(),
                            Message = reader["Message"].ToString(),
                            CreatedAt = (DateTime)reader["CreatedAt"]
                        });
                    }
                }
            }
            return alerts;
        }

        public List<ConnectionLogInfo> GetRecentLogs(int limit = 50)
        {
            var logs = new List<ConnectionLogInfo>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT TOP (@Limit) Id, ClientId, Role, EventType, EventTime FROM ConnectionLogs ORDER BY EventTime DESC";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Limit", limit);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        logs.Add(new ConnectionLogInfo
                        {
                            Id = (long)reader["Id"],
                            ClientId = reader["ClientId"].ToString(),
                            Role = reader["Role"].ToString(),
                            EventType = reader["EventType"].ToString(),
                            EventTime = (DateTime)reader["EventTime"]
                        });
                    }
                }
            }
            return logs;
        }

        public void SaveMetricsBatch(IEnumerable<ClientNetworkInfo> metrics)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var metric in metrics)
                        {
                            string query = @"INSERT INTO ClientNetworkHistory 
                                           (ClientId, IpAddress, Port, DownloadSpeedKbps, UploadSpeedKbps, RecordTime) 
                                           VALUES (@ClientId, @IpAddress, @Port, @Download, @Upload, @RecordTime)";

                            using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ClientId", metric.ClientId);
                                cmd.Parameters.AddWithValue("@IpAddress", metric.IpAddress);
                                cmd.Parameters.AddWithValue("@Port", metric.Port);
                                cmd.Parameters.AddWithValue("@Download", metric.DownloadSpeedKbps);
                                cmd.Parameters.AddWithValue("@Upload", metric.UploadSpeedKbps);
                                cmd.Parameters.AddWithValue("@RecordTime", metric.LastUpdated == default ? DateTime.Now : metric.LastUpdated);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"[Lỗi Database] {ex.Message}");
                    }
                }
            }
        }

        public void SaveSystemMetrics(float totalDownload, float totalUpload, int activeStandard, int activeAdmin)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO SystemNetworkMetrics 
                               (TotalDownloadSpeed, TotalUploadSpeed, ActiveStandardClients, ActiveAdminClients, RecordTime) 
                               VALUES (@Download, @Upload, @Standard, @Admin, GETDATE())";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Download", totalDownload);
                cmd.Parameters.AddWithValue("@Upload", totalUpload);
                cmd.Parameters.AddWithValue("@Standard", activeStandard);
                cmd.Parameters.AddWithValue("@Admin", activeAdmin);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void LogConnectionEvent(string clientId, string role, string eventType)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO ConnectionLogs (ClientId, Role, EventType, EventTime) 
                                 VALUES (@ClientId, @Role, @EventType, GETDATE())";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                cmd.Parameters.AddWithValue("@Role", role);
                cmd.Parameters.AddWithValue("@EventType", eventType);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void SaveAlert(string clientId, string alertType, string message)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO Alerts (ClientId, AlertType, Message, IsResolved, CreatedAt) 
                                 VALUES (@ClientId, @AlertType, @Message, 0, GETDATE())";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                cmd.Parameters.AddWithValue("@AlertType", alertType);
                cmd.Parameters.AddWithValue("@Message", message);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateClientStatus(string clientId, string ipAddress, string status)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    IF EXISTS (SELECT 1 FROM Clients WHERE ClientId = @ClientId)
                    BEGIN
                        UPDATE Clients 
                        SET LastKnownIp = ISNULL(@IpAddress, LastKnownIp), 
                            Status = @Status, 
                            LastSeenAt = GETDATE()
                        WHERE ClientId = @ClientId
                    END
                    ELSE
                    BEGIN
                        INSERT INTO Clients (ClientId, DisplayName, LastKnownIp, Status, FirstConnectedAt, LastSeenAt)
                        VALUES (@ClientId, @ClientId, @IpAddress, @Status, GETDATE(), GETDATE())
                    END";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                cmd.Parameters.AddWithValue("@IpAddress", string.IsNullOrEmpty(ipAddress) ? (object)DBNull.Value : ipAddress);
                cmd.Parameters.AddWithValue("@Status", status);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
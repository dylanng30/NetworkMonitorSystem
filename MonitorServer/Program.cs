using MonitorServer.Controllers;
using MonitorServer.Services;
using SharedLibrary.Interfaces;

namespace MonitorServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Monitor Server";
            IMetricCollector metricCollector = new PerformanceCounterService();
            ServerController server = new ServerController("0.0.0.0", 8888, metricCollector);

            server.Start();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLibrary.Models;

namespace SharedLibrary.Interfaces
{
    public interface IMetricCollector
    {
        NetworkMetrics GetCurrentMetrics(int standardClientsCount, int adminClientsCount);
    }
}

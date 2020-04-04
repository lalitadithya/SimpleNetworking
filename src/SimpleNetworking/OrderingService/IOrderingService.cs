using SimpleNetworking.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetworking.OrderingService
{
    public interface IOrderingService
    {
        Task<List<Packet>> GetNextPacket(Packet packet);
    }
}

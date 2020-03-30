using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetworking.Models
{
    public class Header
    {
        public long SequenceNumber { get; set; }
        public string IdempotencyToken { get; set; }
        public string ClassType { get; set; }
    }
}

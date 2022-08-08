using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.Models
{
    public class User : ITableEntity
    {
        public string? Name { get; set; }
        public string? Password { get; set; }
        public int Privilege { get; set; }

        //properties form ITableEntity:
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}

using System;
using System.Runtime.Serialization;

namespace EarthLat.Backend.Core.Exceptions
{
    [Serializable]
    public class DataProcessException : Exception
    {
        public DataProcessException()
        {
        }

        public DataProcessException(string message) : base(message)
        {
        }

        public DataProcessException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DataProcessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

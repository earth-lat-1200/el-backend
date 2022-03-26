using System;
using System.Runtime.Serialization;

namespace EarthLat.Backend.Core.Exceptions
{

    [Serializable]
    public class ValidationException : ArgumentException
    {
        private readonly string _nestedProperty;
        protected ValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
        public ValidationException(string message, string property, string nestedProperty) : base(message, property)
        {
            _nestedProperty = nestedProperty;
        }
        public ValidationException(string message, string property) : base(message, property)
        {
        }
        public override string Message
        {
            get
            {
                return String.IsNullOrWhiteSpace(_nestedProperty) 
                    ? base.Message 
                    : base.Message + $" (Nested property: {_nestedProperty})";
            }
        }
    }
}



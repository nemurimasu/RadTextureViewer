using System;
using System.Runtime.Serialization;

namespace RadTextureViewer.Core
{
    [Serializable]
    public class OpenJpegException : Exception
    {
        public OpenJpegException()
        {
        }

        public OpenJpegException(string message) : base(message)
        {
        }

        public OpenJpegException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected OpenJpegException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
using System;
using System.Runtime.Serialization;

namespace weatherd
{
    [Serializable]
    public class RecordIngestForbiddenException : Exception
    {
        public RecordIngestForbiddenException()
        {
        }

        public RecordIngestForbiddenException(string message) : base(message)
        {
        }

        public RecordIngestForbiddenException(string message, Exception inner) : base(message, inner)
        {
        }

        protected RecordIngestForbiddenException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}

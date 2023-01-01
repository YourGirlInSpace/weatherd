using System;
using System.Runtime.Serialization;

namespace weatherd
{
    [Serializable]
    public class StationConfigurationException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public StationConfigurationException()
        {
        }

        public StationConfigurationException(string message) : base(message)
        {
        }

        public StationConfigurationException(string message, Exception inner) : base(message, inner)
        {
        }

        protected StationConfigurationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}

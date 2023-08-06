using System;
using System.Runtime.Serialization;

namespace OpenRPG.Exceptions
{
    internal class NoClanMemberException: Exception
    {
        public NoClanMemberException()
        {
        }

        public NoClanMemberException(string message)
                : base(message)
        {
        }

        public NoClanMemberException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected NoClanMemberException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

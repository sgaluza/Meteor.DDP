using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meteor.DDP
{
    public class DdpMethodResultEventArgs
    {
        public class MethodError
        {
            public Int32 ErrorCode { get; private set; }
            public String Reason { get; private set; }
            public String Message { get; private set; }
            public String ErrorType { get; private set; }
            

            internal MethodError(Int32 errorCode, String reason, String message, String errorType)
            {
                this.Reason = reason;
                this.Message = message;
                this.ErrorType = errorType;
            }

            public override string ToString()
            {
                return String.Format("Error: {0} Reason: {1} ErrorType: {2} Message: {3}", ErrorCode, Reason, ErrorType, Message);
            }
        }

        internal DdpMethodResultEventArgs(String callId, MethodError error, String result)
        {
            this.CallId = callId;
            this.Error = error;
            this.Result = result;
        }

        public String CallId { get; private set; }
        public MethodError Error { get; private set; }
        public String Result { get; private set;  }
    }
}


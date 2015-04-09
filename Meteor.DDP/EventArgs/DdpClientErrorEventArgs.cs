using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meteor.DDP
{
    public class DdpClientErrorEventArgs
    {
        public Exception Exception { get; private set; }
        internal DdpClientErrorEventArgs(Exception x)
        {
            this.Exception = x;
        }

    }
}

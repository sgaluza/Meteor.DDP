using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meteor.DDP.EventArgs
{
    public class DdpEventArgs
    {
        public dynamic Data { get; private set; }

        internal DdpEventArgs(dynamic data)
        {
            this.Data = data;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meteor.DDP
{
    public class DdpMethodUpdatedEventArgs
    {
        internal DdpMethodUpdatedEventArgs(String[] callIds)
        {
            this.CallIds = callIds;
        }

        public String[] CallIds { get; private set; }
    }
}

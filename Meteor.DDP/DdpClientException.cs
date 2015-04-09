using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meteor.DDP
{
    [Serializable]
    public class DdpClientException : ApplicationException
    {
        public DdpClientException(String message) : base(message) { }
    }
}

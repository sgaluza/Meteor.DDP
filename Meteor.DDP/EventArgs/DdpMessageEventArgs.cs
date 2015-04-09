using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meteor.DDP
{
    public class DdpMessageEventArgs
    {
        public dynamic Data { get; private set; }
        public String Collection { get; private set; }
        public String Id { get; private set; }
        public String Method { get; private set; }

        internal DdpMessageEventArgs(String id, String method, String collection, dynamic data)
        {
            this.Id = id;
            this.Collection = collection;
            this.Data = data;
            this.Method = method;
        }
    }
}

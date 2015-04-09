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

        public override string ToString()
        {
            return String.Format("Collection: {0} Id: {1} Method: {2} Data: {3}", this.Collection, this.Id, this.Method, this.Data);
        }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meteor.DDP.Workbench
{
    class Program
    {
        static void Main(string[] args)
        {
            using(DdpClient client = new DdpClient("localhost:3000"))
            {
                client.Message += OnMessage;
                client.MeteorError += OnError;
                client.MethodResult += OnMessageResult;
                client.MethodUpdated += OnMessageUpdated;
                client.ClientError += OnClientError;


                client.Connect();
                String callId = Guid.NewGuid().ToString("N");
                client.Subscribe(Guid.NewGuid().ToString("N"), "MeteorPublished");
                client.Publish(callId, "MeteorMethod", new
                {
                    Type = "test",
                    @event = "Hello, Meteor!!!"
                });
                Console.WriteLine("Called method with callId: " + callId);
                Console.ReadLine();
            }
        }

        static void OnClientError(object sender, DdpClientErrorEventArgs e)
        {
            Console.WriteLine("Fatal Error: {0}", e.Exception.Message);
            Console.WriteLine("Stack Trace: {1}", e.Exception.StackTrace);
        }

        static void OnMessageUpdated(object sender, DdpMethodUpdatedEventArgs e)
        {
            Console.WriteLine("Method call updated:" + e.CallIds.Aggregate("",(a, x) => a += " " + x));
        }

        static void OnMessageResult(object sender, DdpMethodResultEventArgs e)
        {
            Console.WriteLine("Method call result:{0}:  {1}", e.CallId, e.Result);
            if (e.Error != null)
                Console.WriteLine("Error: {0}", e.Error);
        }

        static void OnError(object sender, DdpMeteorErrorEventArgs e)
        {
            Console.Error.WriteLine("Error: " + e.Reason.ToString());
            Console.Error.WriteLine("Original Message: " + e.OriginalMessage.ToString());
        }

        static void OnMessage(object sender, DdpMessageEventArgs e)
        {
            Console.WriteLine("Message for item {0}::{1}:  {2}", e.Collection, e.Id, e.Method);
            Console.WriteLine(e.Data);
        }
    }
}

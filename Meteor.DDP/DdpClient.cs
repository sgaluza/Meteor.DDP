using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Meteor.DDP
{
    public class DdpClient : IDisposable
    {
        public event EventHandler<DdpMessageEventArgs> Message;
        public event EventHandler<DdpMeteorErrorEventArgs> MeteorError;
        public event EventHandler<DdpClientErrorEventArgs> ClientError;
        public event EventHandler<DdpMethodUpdatedEventArgs> MethodUpdated;
        public event EventHandler<DdpMethodResultEventArgs> MethodResult;

        private ClientWebSocket _socket;
        private Uri _uri;
        private String _sessionId;

        public DdpClient(String url)
        {
            this._socket = new ClientWebSocket();
            this._uri = new Uri(String.Format("ws://{0}/websocket", url));

        }

        public Task ConnectAsync()
        {
            return this._socket.ConnectAsync(this._uri, CancellationToken.None)
                .ContinueWith(s =>
                {
                    this.Send(new
                    {
                        msg = "connect",
                        version = "1",
                        support = new string[] { "1", "pre1" }
                    });
                })
                .ContinueWith(s =>
                {
                    return SubscriberLoop();
                });

        }

        public Task Publish(String callId, String method, params dynamic[] args)
        {
            return this.Send(new
            {
                msg = "method",
                method = method,
                @params = args,
                id = callId
            });
        }

        public Task Subscribe(String subId, String method, params dynamic[] args)
        {
            return this.Send(new
                {
                    msg = "sub",
                    name = method,
                    @params = args,
                    id = subId
                }
            );
        }

        private Task Send(dynamic message)
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));
            return this._socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task SubscriberLoop()
        {
            Int32 bufferSize = 1024 * 32;
            byte[] buffer = new byte[bufferSize];
            MemoryStream stream = new MemoryStream(bufferSize);
            Int32 start = 0;

            try
            {
                while (this._socket.State == WebSocketState.Open)
                {
                    String msg = "";
                    ArraySegment<byte> segment = new ArraySegment<byte>(buffer);
                    WebSocketReceiveResult result = await this._socket.ReceiveAsync(segment, CancellationToken.None);
                    if (!result.EndOfMessage)
                    {
                        stream.Write(buffer, start, result.Count);
                        start += result.Count;
                    }
                    else
                    {
                        stream.Write(buffer, start, result.Count);
                        msg = Encoding.UTF8.GetString(stream.ToArray());
                        dynamic message = JsonConvert.DeserializeObject<dynamic>(msg);

                        if (this._sessionId == null)
                            this._sessionId = message.session;
                        else
                        {
                            String msgType = message.msg.ToString();
                            switch (msgType)
                            {
                                case "ping":
                                    await this.Send(new { msg = "pong" });
                                    break;
                                case "error":
                                    if (this.MeteorError != null)
                                        this.MeteorError(this, new DdpMeteorErrorEventArgs(message.reason.ToString(), message.offendingMessage));
                                    break;
                                case "updated":
                                    if (this.MethodUpdated != null)
                                    {
                                        List<String> methods = new List<string>();
                                        foreach (var m in message.methods)
                                            methods.Add(m.ToString());
                                        this.MethodUpdated(this, new DdpMethodUpdatedEventArgs(methods.ToArray()));

                                    }
                                    break;
                                case "ready":
                                    break;
                                case "result":
                                    if (this.MethodResult != null)
                                    {
                                        Meteor.DDP.DdpMethodResultEventArgs.MethodError error = null;
                                        if (message.error != null)
                                        {
                                            error = new DdpMethodResultEventArgs.MethodError((Int32)message.error.error,
                                                message.error.reason.ToString(),
                                                message.error.message.ToString(),
                                                message.error.errorType.ToString());
                                        }
                                        this.MethodResult(this, new DdpMethodResultEventArgs(message.id.ToString(), error));
                                    }
                                    break;
                                default:
                                    if (this.Message != null)
                                        this.Message(this, new DdpMessageEventArgs(message.id.ToString(), message.msg.ToString(), message.collection.ToString(), message.fields));
                                    break;
                            }
                        }
                        stream.Dispose();
                        stream = new MemoryStream();

                    }

                }
            }
            catch (Exception x)
            {
                if (this.ClientError != null)
                    this.ClientError(this, new DdpClientErrorEventArgs(x));
            }
            finally
            {
                stream.Dispose();
            }
        }

        public void Dispose()
        {
            this._socket.Dispose();
        }
    }
}



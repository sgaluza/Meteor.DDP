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

        private Nito.AsyncEx.AsyncLock _sendLock = new Nito.AsyncEx.AsyncLock();

        public DdpClient(String url)
        {
            this._uri = new Uri(String.Format("ws://{0}/websocket", url));
        }

        public void Connect()
        {
            this.ConnectAsync().Wait();
        }

        public void Publish(String callId, String method, params dynamic[] args)
        {
            this.PublishAsync(callId, method, args).Wait();
        }

        public void Subscribe(String callId, String method, params dynamic[] args)
        {
            this.SubscribeAsync(callId, method, args).Wait();
        }

        public async Task ConnectAsync()
        {
            if (this._socket != null)
                this._socket.Dispose();
            this._socket = new ClientWebSocket();
            this._socket.Options.KeepAliveInterval = TimeSpan.FromMilliseconds(100);
            await this._socket.ConnectAsync(this._uri, CancellationToken.None)
                .ContinueWith(s =>
                {
                    return this.Send(new
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

        public async Task PublishAsync(String callId, String method, params dynamic[] args)
        {
            await this.Send(new
            {
                msg = "method",
                method = method,
                @params = args,
                id = callId
            });
        }

        public async Task SubscribeAsync(String subId, String method, params dynamic[] args)
        {
            await this.Send(new
                {
                    msg = "sub",
                    name = method,
                    @params = args,
                    id = subId
                }
            );
        }

        private async Task Send(dynamic message)
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));

            using(await this._sendLock.LockAsync())
            {
                await this._socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        private async Task SubscriberLoop()
        {
            Int32 bufferSize = 1024 * 32;
            byte[] buffer = new byte[bufferSize];
            MemoryStream stream = new MemoryStream(bufferSize);
            Int32 start = 0;
            String msg = "";

            try
            {
                while (this._socket.State == WebSocketState.Open)
                {
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
                        else if(message == null || message.msg == null)
                        {
                            if(message.server_id == null)
                                throw new DdpClientException(String.Format("Unknown message: {0}", message));
                        }
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
                                    if (message.id != null && message.msg != null && message.collection != null && message.fields != null)
                                    {
                                        if (this.Message != null)
                                            this.Message(this, new DdpMessageEventArgs(message.id.ToString(), message.msg.ToString(), message.collection.ToString(), message.fields));
                                    }
                                    else
                                        throw new DdpClientException("Unrecognized message: " + message.ToString());
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
                this._sessionId = null;
                stream.Dispose();
            }
        }

        public void Dispose()
        {
            if (this._socket != null) 
            {
                this._socket.Dispose();
            }
        }

        public WebSocketState WebSocketState
        {
            get { return this._socket.State; }
        }
    }
}



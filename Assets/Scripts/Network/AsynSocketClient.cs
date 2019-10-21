using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System;
using System.Linq;
using UnityEditor;

/// <summary>
///    ————————————————
///    版权声明：本文为CSDN博主「大洋彼岸789」的原创文章，遵循 CC 4.0 BY-SA 版权协议，转载请附上原文出处链接及本声明。
///    原文链接：https://blog.csdn.net/elie_yang/article/details/89162885
/// </summary>
public class AsynSocketClient
    {
        #region private
        /// <summary>
        /// 用于控制异步接收消息
        /// </summary>
        // private ManualResetEvent doReceive = new ManualResetEvent(false);
        private TcpClient tcpClient;
 
        //标识客户端是否接收数据
        private bool isStopWork = false;

        private static int BUFF_SIZE;

        private bool LogEnabled;
        #endregion

        /// <summary>
        /// 是否释放对象了
        /// </summary>
        public bool IsClosed
        {
            get { return isStopWork; }
            set { isStopWork = value;}
        }
 
        #region 事件定义
        /// <summary>
        /// 客户端连接完成、发送完成、连接异常或者服务端关闭触发的事件
        /// </summary>
        public event Action<TcpClient, SocketAction, string> Completed;
        
        /// <summary>
        /// 客户端接收消息触发的事件
        /// </summary>
        public event Action<DataEventArgs> Received;
 
        #endregion
        
        public AsynSocketClient(int buff_size)
        {
            tcpClient = new TcpClient();
            BUFF_SIZE = buff_size;
            LogEnabled = true;
        }
 
        #region 连接
        /// <summary>
        /// 异步连接
        /// </summary>
        /// <param name="ip">要连接的服务器的ip地址</param>
        /// <param name="port">要连接的服务器的端口</param>
        public void ConnectAsync(string ip, int port)
        {
            IPAddress ipAddress = null;
            try
            {
                ipAddress = IPAddress.Parse(ip);
            }
            catch (Exception e)
            {
                string err = $"Exception - ConnectAsync() - ip地址格式不正确，请使用正确的ip地址！- {e}";
                OnComplete(tcpClient, SocketAction.Error, err);
            }
            try
            {
                if (!tcpClient.Connected)
                {
                    tcpClient.BeginConnect(ipAddress, port, ConnectCallBack, tcpClient);
                }
                else if (isStopWork)
                {
                    isStopWork = false;
                }
            }
            catch (Exception e)
            {
                string err = $"Exception - ConnectAsync() - {e}";
                OnComplete(tcpClient, SocketAction.Error, err);
            }           
        }
 
        /// <summary>
        /// 异步连接的回调函数
        /// </summary>
        /// <param name="ar"></param>
        private void ConnectCallBack(IAsyncResult ar)
        {
            TcpClient client = ar.AsyncState as TcpClient;
            if (client == null)
                return;
            try
            {
                client.EndConnect(ar);
                string msg = "Client connected to server.";
                OnComplete(client, SocketAction.Connect, msg);
            }
            catch (Exception e)
            {
                string err = $"Exception - ConnectCallBack() - {e}";
                OnComplete(client, SocketAction.Error, err);
            }           
        }
        #endregion
 
        #region 接收数据
        /// <summary>
        /// 异步接收消息
        /// </summary>
        private void ReceiveAsync()
        {
            StateObject obj = new StateObject();
            obj.TcpClient = tcpClient;
            tcpClient.Client.BeginReceive(obj.ListData, 0, obj.ListData.Length, SocketFlags.None, ReceiveCallBack, obj);
        }
 
        private void ReceiveCallBack(IAsyncResult ar)
        {
            StateObject state = ar.AsyncState as StateObject;
            if (state == null)
                return;
            int count = -1;
            try
            {
                if (isStopWork)
                {
                    return;
                }
                count = state.TcpClient.Client.EndReceive(ar);
            }
            catch (Exception ex)
            {
                //如果发生异常，说明客户端失去连接，触发关闭事件
                Stop();
                string err = $"Exception - ReceiveCallBack() - {ex}";
                OnComplete(state.TcpClient, SocketAction.Close, err);
            }

            try
            {
                if (count > 0)
                {
                    // 回调处理接收后的消息
                    DataEventArgs data = new DataEventArgs()
                    {
                        Data = state.ListData.Take<byte>(count).ToArray(),
                    };
                    string msg = $"Receive a message : {data.Data.Length} bytes";
                    OnComplete(state.TcpClient, SocketAction.Receive, msg);
                    Received?.Invoke(data);
                    
                    
//                    // 真正的互联网环境下会有消息包被截断的情况，所以发送的时候必须在开始定义4个字节的包长度，目前是测试阶段，暂时不开放。
//                    //读取数据  
//                    byte[] data = new byte[e.BytesTransferred];
//                    Log($"Server Found data received - {e.BytesTransferred} byts");
//                    Array.Copy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);  
//                    lock (m_buffer)  
//                    {  
//                        m_buffer.AddRange(data);  
//                    }  
//  
//                    do  
//                    {  
//                        //注意: 这里是需要和服务器有协议的,我做了个简单的协议,就是一个完整的包是包长(4字节)+包数据,便于处理,当然你可以定义自己需要的;   
//                        //判断包的长度,前面4个字节.  
//                        byte[] lenBytes = m_buffer.GetRange(0, 4).ToArray();  
//                        int packageLen = BitConverter.ToInt32(lenBytes, 0);  
//                        if (packageLen <= m_buffer.Count - 4)  
//                        {  
//                            //包够长时,则提取出来,交给后面的程序去处理  
//                            byte[] rev = m_buffer.GetRange(4, packageLen).ToArray();  
//                            //从数据池中移除这组数据,为什么要lock,你懂的  
//                            lock (m_buffer)  
//                            {  
//                                m_buffer.RemoveRange(0, packageLen + 4);  
//                            }  
//                            //将数据包交给前台去处理  
//                            Completed?.Invoke(e, ServerSocketAction.Receive);
//                            receiveCallBack?.Invoke(e, rev, 0, rev.Length);
//                        }  
//                        else  
//                        {   //长度不够,还得继续接收,需要跳出循环  
//                            break;  
//                        }  
//                    } while (m_buffer.Count > 4);  
                }
            }
            catch (Exception ex)
            {
                //如果发生异常，说明客户端失去连接，触发关闭事件
                string err = $"Exception - ReceiveCallBack() - {ex}";
                OnComplete(state.TcpClient, SocketAction.Error, err);
            }

            // 接续接收消息
            ReceiveAsync();            
        }
        #endregion
 
        #region 发送数据
        /// <summary>
        /// 异步发送消息
        /// </summary>
        /// <param name="msg"></param>
        public void SendAsync(string msg)
        {
            byte[] listData = Encoding.UTF8.GetBytes(msg);
            SendAsync(listData);
        }
 
        public void SendAsync(byte[] bytes)
        {
            try
            {
                if (tcpClient.Client == null)
                {
                    throw new Exception("连接已经断开");
                }
                if (isStopWork)
                {
                    return;
                }

                // 真正的互联网环境下会有消息包被截断的情况，所以发送的时候必须在开始定义4个字节的包长度，目前是测试阶段，暂时不开放。
                byte[] bytesRealSend = new byte[bytes.Length+4];
                byte[] bytesHeader = System.BitConverter.GetBytes(bytes.Length);
                Array.Copy(bytesHeader, 0, bytesRealSend, 0, 4);
                Array.Copy(bytes, 0, bytesRealSend, 4, bytes.Length);
                StateObject state = new StateObject
                {
                    TcpClient = tcpClient,
                    ListData = bytesRealSend,
                };
                tcpClient.Client.BeginSend(bytesRealSend, 0, bytesRealSend.Length, SocketFlags.None, SendCallBack, state);
                //tcpClient.Client.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, SendCallBack, state);
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == (int)SocketError.ConnectionAborted)
                {
                    string err = $"Exceptioin - SendAsync() - 连接已经放弃! - {ex}";
                    OnComplete(tcpClient, SocketAction.Error, err);
                }
                else
                {
                    string err = $"Exceptioin - SendAsync() - {ex}";
                    OnComplete(tcpClient, SocketAction.Error, err);
                }
            }
          
        }
 
        private void SendCallBack(IAsyncResult ar)
        {
            StateObject stateObj = ar.AsyncState as StateObject;
            TcpClient client = stateObj?.TcpClient;
            if (client == null)
                return;
            try
            {
                client.Client.EndSend(ar);
                string msg = $"Send a message : {stateObj.ListData.Length} bytes";
                OnComplete(client, SocketAction.Send, msg);
            }
            catch (Exception ex)
            {
                //如果发生异常，说明客户端失去连接，触发关闭事件
                Stop();
                string err = $"Exception - SendCallBack() - {ex}";
                OnComplete(client, SocketAction.Error, err);
            }
        }
        #endregion
 
        #region OnComoplete
        public virtual void OnComplete(TcpClient client, SocketAction action, string msg)
        {
            Completed?.Invoke(client, action, msg);
            if (action == SocketAction.Connect)
            {
                // 接收数据
                ThreadPool.QueueUserWorkItem(x =>
                {
                    //while (!isStopWork) // 原始代码这里写错了。。。这里会导致几秒钟之内内存占用超过10G
                    {
                        try
                        {
                            ReceiveAsync();
                            //Thread.Sleep(20);
                        }
                        catch (Exception ex)
                        {
                            Stop();
                            string err = $"Exception - OnComplete() - {ex}";
                            OnComplete(client, SocketAction.Error, err);
                        }
                    }
                });
            }
            else if (action == SocketAction.Send)
            {
            }
            else if (action == SocketAction.Receive)
            {
            }
            else if (action == SocketAction.Close)
            {
                try
                {
                    Log("socket closed.");
                    this.Received = null;
                    tcpClient.Close();
                }
                catch(Exception e)
                {
                    string err = $"Exception - OnComplete() - {e}";
                    OnComplete(client, SocketAction.Error, err);
                }
            }
        }
        #endregion
        
         #region 关闭
        void Stop()
        {
            isStopWork = true;
        }
 
        public void Close()
        {
            if (tcpClient != null)
            {
                try
                {
                    Stop();
                    IsClosed = true;
                    Completed = null;
                    Received = null;
                    tcpClient.GetStream().Close();
                 
                }
                catch(Exception e)
                {
                    string err = $"Exception - Close() - {e}";
                    OnComplete(tcpClient, SocketAction.Error, err);
                }
            }

            string msg = $"Client Closed.";
            OnComplete(tcpClient, SocketAction.Close, msg);
        }
        #endregion
        
        public class StateObject
        {
            public TcpClient TcpClient { get; set; }
            private byte[] listData = new byte[BUFF_SIZE];
            /// <summary>
            /// 接收的数据
            /// </summary>
            public byte[] ListData
            {
                get
                {
                    return listData;
                }
                set
                {
                    listData = value;
                }
            }
        }

        public void Log(string msg)
        {
            if(LogEnabled)
                Debug.Log(msg);
        }
    }
 
    /// <summary>
    /// 接收socket的行为
    /// </summary>
    public enum SocketAction
    {
        /// <summary>
        /// socket发生连接
        /// </summary>
        Connect = 1,
        /// <summary>
        /// socket发送数据
        /// </summary>
        Send = 2,
        /// <summary>
        /// socket发送数据
        /// </summary>
        Receive = 3,
        /// <summary>
        /// socket关闭
        /// </summary>
        Close = 4,
        /// <summary>
        /// socket关闭
        /// </summary>
        Error = 9,
    }
    
    public class DataEventArgs : EventArgs
    {
        public byte[] Data
        {
            get;
            set;
        }
    }

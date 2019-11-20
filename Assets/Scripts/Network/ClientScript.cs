using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading;

public class ClientScript : MonoBehaviour
{
    public string _address = "127.0.0.1";
    public int _port = 9999;

    private const int BUFF_SIZE = 1024;
    
    private AsynSocketClient _client;

    public event Action<byte[]> Received;
    public event Action<SocketAction, string> Completed;

    private object _lockObj;
    private List<byte[]> _recvMsgList;

    private List<SocketEvent> _socketEventList;

    protected void Start()
    {
        _client = new AsynSocketClient(BUFF_SIZE);
        _client.Received += OnReceiveMsg;
        _client.Completed += OnComplete;

        _lockObj = new System.Object();
        lock (_lockObj)
        {
            _recvMsgList = new List<byte[]>();
            _socketEventList = new List<SocketEvent>();
        }
        Log("Client started!");
    }

    void OnDestroy()
    {
        if (_client != null)
        {
            _client.Received -= OnReceiveMsg;
            _client.Completed -= OnComplete;
            _client.Close();
        }

        Log("Client closed!");
    }

    protected void Update()
    {
        lock (_lockObj)
        {
            while (_recvMsgList.Count > 0)
            {
                byte[] data = _recvMsgList[0];
                Received?.Invoke(data);
                _recvMsgList.RemoveAt(0);
            }

            while (_socketEventList.Count > 0)
            {
                SocketEvent se = _socketEventList[0];
                Completed?.Invoke(se._action, se._msg);
                _socketEventList.RemoveAt(0);
            }

        }
    }

    public void Log(string msg)
    {
        _client.Log(msg);
    }

    public void Connect()
    {
        Log($"Begin connecting server - {_address}:{_port} ...");
        _client.ConnectAsync(_address, _port);
    }

    public void SendMsg(byte[] data)
    {
        _client.SendAsync(data);
    }

    public void SendMsg(string data)
    {
        _client.SendAsync(data);
    }

    void OnReceiveMsg(DataEventArgs args)
    {
        lock (_lockObj)
        {
            _recvMsgList.Add(args.Data);
        }
    }

    void OnComplete(TcpClient client, SocketAction action, string msg)
    {
        lock (_lockObj)
        {
            SocketEvent se = new SocketEvent()
            {
                _action = action,
                _msg = msg,
            };
            _socketEventList.Add(se);
        }
    }

    class SocketEvent
    {
        public SocketAction _action;
        public string _msg;
    }
}
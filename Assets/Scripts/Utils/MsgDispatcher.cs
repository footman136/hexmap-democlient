using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameUtils
{

    public class MsgDispatcher
    {
        private static readonly Dictionary<int, List<Action<byte[]>>> _msgHandlers =
            new Dictionary<int, List<Action<byte[]>>>();

        public static void RegisterMsg(int msgId, Action<byte[]> func)
        {
            List<Action<byte[]>> funcList = null;
            if (_msgHandlers.ContainsKey(msgId))
            {
                funcList = _msgHandlers[msgId];
            }
            else
            {
                funcList = new List<Action<byte[]>>();
                _msgHandlers.Add(msgId, funcList);
            }

            if (funcList.Contains(func))
            {
                Debug.LogError("重复的注册了消息解析 id: " + msgId);
                funcList.Remove(func);
            }

            funcList.Add(func);
        }

        public static void UnRegisterMsg(int msgId, Action<byte[]> func)
        {
            if (_msgHandlers.ContainsKey(msgId))
            {
                var funcList = _msgHandlers[msgId];
                foreach (var func0 in funcList)
                {
                    if (func0 == func)
                    {
                        funcList.Remove(func);
                        break;
                    }
                }
            }
        }

        public static void ProcessMsg(byte[] bytes, int size)
        {
            if (size < 4)
            {
                Debug.Log($"MsgDispatcher - ProcessMsg Error - invalid data size:{size}");
                return;
            }

            int msgId = ParseMsgId(bytes);
            if (_msgHandlers.ContainsKey(msgId))
            {
                byte[] recvData = new byte[size - 4];
                Array.Copy(bytes, 4, recvData, 0, size - 4);
                var funcList = _msgHandlers[msgId];
                foreach (var func in funcList)
                {
                    func?.Invoke(recvData);
                }
            }
        }

        private static int ParseMsgId(byte[] bytes)
        {
            byte[] recvHeader = new byte[4];
            Array.Copy(bytes, 0, recvHeader, 0, 4);
            int msgId = BitConverter.ToInt32(recvHeader, 0);
            return msgId;
        }

    }
}
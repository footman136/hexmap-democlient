using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Google.Protobuf;
using JetBrains.Annotations;
// https://github.com/LitJSON/litjson
using LitJson;
using Main;
// https://blog.csdn.net/u014308482/article/details/52958148
using Protobuf.Room;
using System.IO;

public class RoomMsgReply
{

    public static void ProcessMsg(byte[] data, int size)
    {
        if (size < 4)
        {
            Debug.Log($"ROOM - ProcessMsg Error - invalid data size:{size}");
            return;
        }

        byte[] recvHeader = new byte[4];
        Array.Copy(data, 0, recvHeader, 0, 4);
        byte[] recvData = new byte[size - 4];
        Array.Copy(data, 4, recvData, 0, size - 4);

        int msgId = System.BitConverter.ToInt32(recvHeader,0);
        switch ((ROOM_REPLY) msgId)
        {
            case ROOM_REPLY.PlayerEnterReply:
                PLAYER_ENTER_REPLY(recvData);
                break;
            case ROOM_REPLY.UploadMapReply:
                UPLOAD_MAP_REPLY(recvData);
                break;
            case ROOM_REPLY.DownloadMapReply:
                DOWNLOAD_MAP_REPLY(recvData);
                break;
        }
    }

    private static void PLAYER_ENTER_REPLY(byte[] bytes)
    {
        PlayerEnterReply input = PlayerEnterReply.Parser.ParseFrom(bytes);
        if (input.Ret)
        {
            if(ClientManager.Instance != null)
                ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.CONNECTED_ROOM);
        }
        else
        {
            ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.LOBBY);
            string msg = "玩家进入房间失败！！！";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            GameRoomManager.Instance.Log(msg);
            ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.START);
        }
    }

    private static void UPLOAD_MAP_REPLY(byte[] bytes)
    {
        UploadMapReply input = UploadMapReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            GameRoomManager.Instance.Log("MSG: 上传地图失败！");
            return;
        }

        if (input.IsLastPackage)
        {
            long roomId = input.RoomId;
            DownloadMap output = new DownloadMap()
            {
                RoomId = roomId,
                RoomName = input.RoomName,
            };
            GameRoomManager.Instance.SendMsg(ROOM.DownloadMap, output.ToByteArray());
            GameRoomManager.Instance.Log($"MSG: 上传地图成功！RoomID:{roomId}");
        }
    }

    private static List<byte[]> mapDataBuffers = new List<byte[]>();
    private static void DOWNLOAD_MAP_REPLY(byte[] bytes)
    {
        DownloadMapReply input = DownloadMapReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            GameRoomManager.Instance.Log("MSG: 下载地图失败！");
            return;
        }

        EnterRoomData roomData = new EnterRoomData();
        roomData.RoomName = input.RoomName;
        roomData.RoomId = input.RoomId;
        roomData.IsCreateByMe = input.IsCreatedByMe;
        roomData.MaxPlayerCount = input.MaxPlayerCount;
        
        if (input.PackageIndex == 0)
        {// 第一条此类消息
            mapDataBuffers.Clear();                        
        }
        mapDataBuffers.Add(input.MapData.ToByteArray());
        
        bool ret = false;
        if (input.IsLastPackage)
        {// 最后一条此类消息了

            int totalSize = 0;
            foreach (var package in mapDataBuffers)
            {
                totalSize += package.Length;
            }
            // 同时确保文件名的唯一性和可读性
            string mapName = $"{roomData.RoomName}_{roomData.RoomId}";
            
            BinaryWriter writer = GameRoomManager.Instance.hexmapHelper.BeginSaveBuffer(mapName);
            if (writer == null)
            {
                 return;
            }

            foreach (var package in mapDataBuffers)
            {
                GameRoomManager.Instance.hexmapHelper.SaveBuffer(writer, package);                
            }

            GameRoomManager.Instance.hexmapHelper.EndSaveBuffer(ref writer);
            GameRoomManager.Instance.Log($"MSG: 下载地图成功！地图名：{mapName} - Total Size:{totalSize}");
        }
    }
}

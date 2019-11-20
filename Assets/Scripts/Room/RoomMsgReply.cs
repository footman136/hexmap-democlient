using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Google.Protobuf;
// https://github.com/LitJSON/litjson
using LitJson;
using Main;
// https://blog.csdn.net/u014308482/article/details/52958148
using Protobuf.Room;
using System.IO;
using GameUtils;
using Google.Protobuf.Collections;

public class RoomMsgReply
{

    public static void ProcessMsg(byte[] bytes, int size)
    {
        if (size < 4)
        {
            Debug.Log($"ROOM - ProcessMsg Error - invalid data size:{size}");
            return;
        }

        byte[] recvHeader = new byte[4];
        Array.Copy(bytes, 0, recvHeader, 0, 4);
        byte[] recvData = new byte[size - 4];
        Array.Copy(bytes, 4, recvData, 0, size - 4);

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
            case ROOM_REPLY.EnterRoomReply:
                ENTER_ROOM_REPLY(recvData);
                break;
            case ROOM_REPLY.LeaveRoomReply:
                LEAVE_ROOM_REPLY(recvData);
                break;
            case ROOM_REPLY.DownloadCitiesReply:
                DOWNLOAD_CITIES_REPLY(recvData);
                break;
            case ROOM_REPLY.DownloadActorsReply:
                break;
            case ROOM_REPLY.DownloadResCellReply:
                DOWNLOAD_RESCELL_REPLY(recvData);
                break;
            default:
                // 通用消息处理器，别的地方要想响应找个消息，应该调用MsgDispatcher.RegisterMsg()来注册消息处理事件
                MsgDispatcher.ProcessMsg(bytes, size);
                break;
        }
    }

    private static void PLAYER_ENTER_REPLY(byte[] bytes)
    {
        PlayerEnterReply input = PlayerEnterReply.Parser.ParseFrom(bytes);
        if (input.Ret)
        {
            if (ClientManager.Instance != null)
            {
                ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.CONNECTED_ROOM);
            }

            string msg = "玩家成功加入战场服务器!";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Success);
            GameRoomManager.Instance.Log($"MSG: ROOM PLAYER_ENTER_REPLY OK - " + msg);
            
            //载入地图(调试Only)
            if (ClientManager.Instance == null)
            {
                GameRoomManager.Instance.LoadMap();
            }
        }
        else
        {
            string msg = "玩家加入战场服务器失败！";
            // 不能使用SystemTips,因为会切换场景(scene),切换场景的时候,SystemTips无法显示
            //UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            UIManager.Instance.MessageBox("错误", msg, (int)PanelMessageBox.BUTTON.OK, OnClickPlayerEnterFailed);
            GameRoomManager.Instance.Log($"MSG: ROOM PLAYER_ENTER_REPLY Error - " + msg);
        }
    }
    
    static void OnClickPlayerEnterFailed(int index)
    {
        if (ClientManager.Instance != null)
        {
            ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.LOBBY);
        }
        else
        {
            Application.Quit();
        }
    }
    
    private static void UPLOAD_MAP_REPLY(byte[] bytes)
    {
        UploadMapReply input = UploadMapReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            string msg = "上传地图失败！";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            GameRoomManager.Instance.Log("MSG: UPLOAD_MAP_REPLY Error - " + msg);
            return;
        }

        if (input.IsLastPackage)
        {
            GameRoomManager.Instance.Log($"MSG: UPLOAD_MAP_REPLY OK - 上传地图成功！RoomID:{input.RoomId}");
            // 发出EnterRoom消息，进入房间
            EnterRoom output = new EnterRoom()
            {
                RoomId = input.RoomId,
            };
            GameRoomManager.Instance.SendMsg(ROOM.EnterRoom, output.ToByteArray());
            GameRoomManager.Instance.Log($"MSG: UPLOAD_MAP_REPLY OK - 申请进入战场：{input.RoomName}");
        }
    }

    private static List<byte[]> mapDataBuffers = new List<byte[]>();
    private static void DOWNLOAD_MAP_REPLY(byte[] bytes)
    {
        DownloadMapReply input = DownloadMapReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            string msg = "下载地图失败！";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            GameRoomManager.Instance.Log("MSG: DOWNLOAD_MAP_REPLY Error - " + msg);
            return;
        }

        if (input.PackageIndex == 0)
        {// 第一条此类消息
            mapDataBuffers.Clear();
            GameRoomManager.Instance.Log($"MSG: DOWNLOAD_MAP_REPLY - 开始下载地图！地图名：{input.RoomName}");
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
            string mapName = $"{input.RoomName}_{input.RoomId}";
            
            // 把服务器传过来的地图数据写入本地文件
            BinaryWriter writer = GameRoomManager.Instance.HexmapHelper.BeginSaveBuffer(mapName);
            if (writer == null)
            {
                 return;
            }

            foreach (var package in mapDataBuffers)
            {
                GameRoomManager.Instance.HexmapHelper.SaveBuffer(writer, package);                
            }

            GameRoomManager.Instance.HexmapHelper.EndSaveBuffer(ref writer);
            GameRoomManager.Instance.Log($"MSG: DOWNLOAD_MAP_REPLY - 下载地图成功！地图名：{mapName} - Total Map Size:{totalSize}");

            // 从本地文件读取地图，并显示出来
            GameRoomManager.Instance.HexmapHelper.Load(mapName);
            GameRoomManager.Instance.Log($"MSG: DOWNLOAD_MAP_REPLY - 显示地图！地图名：{mapName}");
            
            // 设置房间ID和名字
            GameRoomManager.Instance.RoomId = input.RoomId;
            GameRoomManager.Instance.RoomName = input.RoomName;
            string msg = $"进入战场 - {input.RoomName}";
            GameRoomManager.Instance.Log("MSG: DOWNLOAD_MAP_REPLY OK - " + msg);
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Success);
            
            // 2-补充内容，获取城市信息
            DownloadCities output = new DownloadCities()
            {
                RoomId = input.RoomId,
            };
            GameRoomManager.Instance.SendMsg(ROOM.DownloadCities, output.ToByteArray());
            
            // 3-补充内容，获取单位信息
            DownloadActors output2 = new DownloadActors()
            {
                RoomId = input.RoomId,
            };
            GameRoomManager.Instance.SendMsg(ROOM.DownloadActors, output2.ToByteArray());
            
            // 4-刷新玩家身上的资源
            UpdateRes output3 = new UpdateRes()
            {
                RoomId = input.RoomId,
                OwnerId = GameRoomManager.Instance.CurrentPlayer.TokenId,
            };
            GameRoomManager.Instance.SendMsg(ROOM.UpdateRes, output3.ToByteArray());
            
            // 5-刷新玩家身上的行动点
            UpdateActionPoint output4 = new UpdateActionPoint()
            {
                RoomId = input.RoomId,
                OwnerId = GameRoomManager.Instance.CurrentPlayer.TokenId,
            };
            GameRoomManager.Instance.SendMsg(ROOM.UpdateActionPoint, output4.ToByteArray());
        }
    }

    private static void ENTER_ROOM_REPLY(byte[] bytes)
    {
        EnterRoomReply input = EnterRoomReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            string msg = "进入战场失败：" + input.ErrMsg;
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            GameRoomManager.Instance.Log("MSG: ENTER_ROOM_REPLY - " + msg);
            if (ClientManager.Instance)
            {
                ClientManager.Instance.StateMachine.TriggerTransition(
                    ConnectionFSMStateEnum.StateEnum.DISCONNECTED_ROOM);
            }

            return;
        }

        // 请求地图数据
        DownloadMap output = new DownloadMap()
        {
            RoomId = input.RoomId,
        };
        GameRoomManager.Instance.SendMsg(ROOM.DownloadMap, output.ToByteArray());
        
        // 请求地图上的资源变化数据
        DownloadResCell output2 = new DownloadResCell()
        {
            RoomId = input.RoomId,
        };
        GameRoomManager.Instance.SendMsg(ROOM.DownloadResCell, output2.ToByteArray());
        {
            string msg = "成功进入战场!";
            GameRoomManager.Instance.Log($"MSG: ENTER_ROOM_REPLY OK - " + msg);
        }
    }

    private static void LEAVE_ROOM_REPLY(byte[] bytes)
    {
        LeaveRoomReply input = LeaveRoomReply.Parser.ParseFrom(bytes);
        if (ClientManager.Instance == null)
        {
            Application.Quit();
        }
        else if(!input.IsKicked)
        {
            GameRoomManager.Instance.Log($"MSG: LEAVE_ROOM_REPLY OK - ");
        }
        else
        {
            GameRoomManager.Instance.Log($"MSG: LEAVE_ROOM_REPLY OK - Kicked out!");
        }
    }

    private static void DOWNLOAD_ACTORS_REPLY(byte[] bytes)
    {
        DownloadActorsReply input = DownloadActorsReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            string msg = $"查询单元信息失败！";
            GameRoomManager.Instance.Log("MSG: DOWNLOAD_ACTORS_REPLY Error - " + msg);
            return;
        }

        {
            string msg = $"查询单元信息成功！";
            GameRoomManager.Instance.Log("MSG: DOWNLOAD_ACTORS_REPLY OK - " + msg +
                                         $"City Count:{input.MyCount}/{input.TotalCount}");
        }
    }

    private static void DOWNLOAD_CITIES_REPLY(byte[] bytes)
    {
        DownloadCitiesReply input = DownloadCitiesReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            string msg = $"查询城市信息失败！";
            GameRoomManager.Instance.Log("MSG: DOWNLOAD_CITIES_REPLY Error - " + msg);
            return;
        }
        GameRoomManager.Instance.Log($"MSG: DOWNLOAD_CITIES_REPLY OK - 城市个数:{input.MyCount}/{input.TotalCount}");

        // 如果我一个城市都没有，则主动创建一个城市
        if (input.MyCount == 0)
        {
            UrbanCity city = GameRoomManager.Instance.RoomLogic.UrbanManager.CreateRandomCity();
            if (city == null)
            {
                string msg = "自动创建城市失败！";
                UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
                GameRoomManager.Instance.Log("MSG: DOWNLOAD_CITIES_REPLY - " + msg);
            }
            else
            {
                CityAdd output = new CityAdd()
                {
                    RoomId = city.RoomId,
                    OwnerId = city.OwnerId,
                    CityId = city.CityId,
                    PosX = city.PosX,
                    PosZ = city.PosZ,
                    CellIndex= city.CellIndex,
                    CityName = city.CityName,
                    CitySize = city.CitySize,
                };
                GameRoomManager.Instance.SendMsg(ROOM.CityAdd, output.ToByteArray());
                GameRoomManager.Instance.Log("MSG: DOWNLOAD_CITIES_REPLY OK - 申请创建城市...");
            }
        }

        {
            string msg = $"查询城市信息成功！";
            GameRoomManager.Instance.Log("MSG: DOWNLOAD_CITIES_REPLY OK - " + msg + $"City Count:{input.MyCount}/{input.TotalCount}");
        }
        // 进入房间整体流程完成
        UIManager.Instance.EndLoading();
    }

    static int resCount = 0;
    private static void DOWNLOAD_RESCELL_REPLY(byte[] bytes)
    {
        DownloadResCellReply input = DownloadResCellReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            string msg = "下载资源数据失败! - " + input.ErrMsg;
            GameRoomManager.Instance.Log($"MSG: DOWNLOAD_RES_REPLY OK - " + msg);
            return;
        }
        List<HexGridChunk> chunkList = new List<HexGridChunk>(); 
        for (int i = 0; i < input.InfoCount; ++i)
        {
            HexCell cell = GameRoomManager.Instance.HexmapHelper.GetCell(input.ResInfo[i].CellIndex);
            HexResource hr = cell.Res;
            hr.ResType = (HexResource.RESOURCE_TYPE) input.ResInfo[i].ResType;
            hr.SetAmount(hr.ResType, input.ResInfo[i].ResAmount);
            cell.UpdateFeatureLevelFromRes();
            if (!chunkList.Contains(cell.chunk))
            {
                chunkList.Add(cell.chunk);
            }
        }
        // 刷新模型
        foreach (var chunk in chunkList)
        {
            chunk.Refresh();
        }

        if (input.PackageIndex == 0 && input.PackageIndex < input.PackageCount - 1)
        {
            resCount = input.InfoCount;
            string msg = "开始下载资源数据...";
            GameRoomManager.Instance.Log($"MSG: DOWNLOAD_RES_REPLY - " + msg + $"PackageCount:{input.PackageCount}");
        }
        else if(input.PackageIndex == input.PackageCount - 1)
        {
            resCount += input.InfoCount;
            string msg = "下载资源数据成功!";
            GameRoomManager.Instance.Log($"MSG: DOWNLOAD_RES_REPLY OK - " + msg + $"PackageCount:{input.PackageCount} - Res Count:{resCount}");
        }
    }
}

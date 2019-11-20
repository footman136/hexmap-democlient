using UnityEngine;
using System;
using Google.Protobuf;
// https://github.com/LitJSON/litjson
using LitJson;
using Main;
// https://blog.csdn.net/u014308482/article/details/52958148
using Protobuf.Lobby;
using Google.Protobuf.Collections;

public class LobbyMsgReply
{

    public static void ProcessMsg(byte[] bytes, int size)
    {
        if (size < 4)
        {
            Debug.Log($"LOBBY - ProcessMsg Error - invalid data size:{size}");
            return;
        }

        byte[] recvHeader = new byte[4];
        Array.Copy(bytes, 0, recvHeader, 0, 4);
        byte[] recvData = new byte[size - 4];
        Array.Copy(bytes, 4, recvData, 0, size - 4);

        int msgId = System.BitConverter.ToInt32(recvHeader,0);
        switch ((LOBBY_REPLY) msgId)
        {
            case LOBBY_REPLY.PlayerEnterReply:
                PLAYER_ENTER_REPLY(recvData);
                break;
            case LOBBY_REPLY.PlayerLeaveReply:
                PLAYER_LEAVE_REPLY(recvData);
                break;
            case LOBBY_REPLY.AskRoomListReply:
                ASK_ROOM_LIST_REPLY(recvData);
                break;
            case LOBBY_REPLY.AskCreateRoomReply:
                ASK_CREATE_ROOM_REPLY(recvData);
                break;
            case LOBBY_REPLY.AskJoinRoomReply:
                ASK_JOIN_ROOM_REPLY(recvData);
                break;
            case LOBBY_REPLY.DestroyRoomReply:
                DESTROY_ROOM_REPLY(recvData);
                break;
        }
    }

    static void PLAYER_ENTER_REPLY(byte[] bytes)
    {
        PlayerEnterReply input = PlayerEnterReply.Parser.ParseFrom(bytes);
        if (input.Ret)
        {
            string msg = "玩家成功加入大厅服务器!";
            ClientManager.Instance.LobbyManager.Log($"MSG: LOBBY PLAYER_ENTER_REPLY OK - " + msg);
            ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.CONNECTED);
        }
        else
        {
            string msg = "玩家进入大厅失败！！！";
            // 不能使用SystemTips,因为会切换场景(scene),切换场景的时候,SystemTips无法显示
            //UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            UIManager.Instance.MessageBox("错误", msg, (int)PanelMessageBox.BUTTON.OK, OnClickPlayerEnterFailed);
            ClientManager.Instance.LobbyManager.Log("MSG: LOBBY PLAYER_ENTER_REPLY Error - " + msg);
        }
    }

    static void OnClickPlayerEnterFailed(int index)
    {
        ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.DISCONNECTED);
    }

    static void PLAYER_LEAVE_REPLY(byte[] bytes)
    {
        PlayerLeaveReply input = PlayerLeaveReply.Parser.ParseFrom(bytes);
        if (input.TokenId != ClientManager.Instance.Player.TokenId)
        {
            string msg = "不是自己!";
            ClientManager.Instance.LobbyManager.Log("MSG: LOBBY PLAYER_LEAVE_REPLY Error - " + msg);
            return;
        }

        if (input.IsKicked)
        {
            string msg = $"本用户在其他地方登录, 请确认是您的账号安全.";
            UIManager.Instance.MessageBox("警告", msg, (int)PanelMessageBox.BUTTON.OK, OnClickPlayerLeave);
            ClientManager.Instance.LobbyManager.Log("MSG: LOBBY PLAYER_LEAVE_REPLY OK - " + msg);
        }
    }
    
    static void OnClickPlayerLeave(int index)
    {
        ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.DISCONNECTED);
    }

    static void ASK_ROOM_LIST_REPLY(byte[] bytes)
    {
        AskRoomListReply input = AskRoomListReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            string msg = "获取房间信息失败！";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            ClientManager.Instance.LobbyManager.Log("MSG: ASK_ROOM_LIST_REPLY Error - " + msg);
            return;
        }
        ClientManager.Instance.LobbyManager.Log($"MSG: ASK_ROOM_LIST_REPLY - Room Count:{input.Rooms.Count}");
        
        PanelLobbyMain.Instance.ClearRoomList();
        foreach (var roomInfo in input.Rooms)
        {
            PanelLobbyMain.Instance.AddRoomInfo(roomInfo);
            ClientManager.Instance.LobbyManager.Log($"MSG: ASK_ROOM_LIST_REPLY - {roomInfo}");
        }
    }

    static void ASK_CREATE_ROOM_REPLY(byte[] bytes)
    {
        UIManager.Instance.EndConnecting();
        AskCreateRoomReply input = AskCreateRoomReply.Parser.ParseFrom(bytes);
        if (input.Ret)
        {
            // 根据大厅传递回来的RoomServer的地址，链接RoomServer
            // 这个类是Room场景初始化的时候,GameRoomManager需要的数据，因为跨场景了，所以需要一个全局的地方进行传递
            EnterRoomData roomData = new EnterRoomData()
            {
                Address = input.RoomServerAddress,
                Port = input.RoomServerPort,
                MaxPlayerCount = input.MaxPlayerCount,
                RoomName = input.RoomName,
                IsCreatingRoom = true, // 创建房间
                RoomId = 0,
            };
            ClientManager.Instance.EnterRoom = roomData;
            
            // 正式进入房间了。。。加载Room场景
            ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.CONNECTING_ROOM);
            ClientManager.Instance.LobbyManager.Log($"MSG: ASK_CREATE_ROOM_REPLY OK - 大厅回复可以创建房间。RoomServer:{roomData.Address}:{roomData.Port} - Room Name:{roomData.RoomName}");
        }
        else
        {
            string msg = $"大厅发现没有多余的房间服务器可以分配！";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            ClientManager.Instance.LobbyManager.Log("MSG: ASK_CREATE_ROOM_REPLY Error - " + msg);
        }
    }
    static void ASK_JOIN_ROOM_REPLY(byte[] bytes)
    {
        UIManager.Instance.EndConnecting();
        AskJoinRoomReply input = AskJoinRoomReply.Parser.ParseFrom(bytes);
        if (input.Ret)
        {
            // 根据大厅传递回来的RoomServer的地址，链接RoomServer
            // 这个类是Room场景初始化的时候,GameRoomManager需要的数据，因为跨场景了，所以需要一个全局的地方进行传递
            EnterRoomData roomData = new EnterRoomData()
            {
                Address = input.RoomServerAddress,
                Port = input.RoomServerPort,
                RoomId = input.RoomId,
                IsCreatingRoom = false, // 加入房间
            };
            ClientManager.Instance.EnterRoom = roomData;
            
            // 正式进入房间了。。。加载Room场景
            ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.CONNECTING_ROOM);
            ClientManager.Instance.LobbyManager.Log($"MSG: ASK_JOIN_ROOM_REPLY OK - 大厅回复可以加入房间。RoomServer:{roomData.Address}:{roomData.Port} - Room Name:{roomData.RoomId}");
        }
        else
        {
            string msg = $"大厅发现没有多余的房间服务器可以分配！";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            ClientManager.Instance.LobbyManager.Log("MSG: ASK_JOIN_ROOM_REPLY - Error " + msg);
        }
    }

    static void DESTROY_ROOM_REPLY(byte[] bytes)
    {
        DestroyRoomReply input = DestroyRoomReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            ClientManager.Instance.LobbyManager.Log("MSG: DESTROY_ROOM_REPLY Error - 删除房间失败！");
            return;
        }

        string msg = $"删除房间成功！{input.RoomName}";
        UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Success);
        ClientManager.Instance.LobbyManager.Log("MSG: DESTROY_ROOM_REPLY OK - " + msg);

        AskRoomList output = new AskRoomList();
        ClientManager.Instance.LobbyManager.SendMsg(LOBBY.AskRoomList, output.ToByteArray());
    }
}

using System.Collections;
using System.Collections.Generic;
using Animation;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;

public class CmdBuildCity : MonoBehaviour, ICommand
{
    public GameObject Cmd{set;get;}

    public int CanRun ()
    {
        return 1;
    }
    public void Run()
    {
        var pi = CommandManager.Instance.CurrentExecuter;
        if (pi == null || !pi.CurrentActor)
        {
            string msg = $"没有选中任何部队!";
            UIManager.Instance.SystemTips(msg,PanelSystemTips.MessageType.Error);
            GameRoomManager.Instance.Log("CmdBuildCity Error - " + msg);
            return;
        }

        long creatorId = 0;
        var av = pi.CurrentActor;
        if (av != null)
        {
            creatorId = av.ActorId;
        }
        else
        {
            string msg = "没有找到开拓者,无法创建城市!";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            GameRoomManager.Instance.Log("CmdBuildCity Error - " + msg);
            return;
        }

        UrbanCity city = GameRoomManager.Instance.RoomLogic.UrbanManager.CreateCityHere(av.HexUnit.Location);
        if (city == null)
        {
            string msg = "这个位置无法创建城市!";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            GameRoomManager.Instance.Log("CmdBuildCity Error - " + msg);
            return;
        }
        // 看看行动点够不够
        if (!CmdAttack.IsActionPointGranted())
        {
            string msg = "行动点数不够, 本操作无法执行! ";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            Debug.Log("CmdBuildCity Run Error - " + msg);
            return;
        }
        
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
                CreatorId = creatorId,
            };
            GameRoomManager.Instance.SendMsg(ROOM.CityAdd, output.ToByteArray());
            GameRoomManager.Instance.Log("AskBuildCity - 申请创建城市...");
            // 消耗行动点 
            CmdAttack.TryCommand();
        }
    }
    public void Tick()
    {
    }
    public void Stop()
    {
    }
}

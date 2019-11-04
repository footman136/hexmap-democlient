using System.Collections;
using System.Collections.Generic;
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
        if (pi == null || !pi.CurrentUnit)
        {
            string msg = $"没有选中任何部队!";
            UIManager.Instance.SystemTips(msg,PanelSystemTips.MessageType.Error);
            GameRoomManager.Instance.Log("CmdBuildCity - " + msg);
            return;
        }

        HexCell cell = pi.CurrentCell;
        if (cell && cell.Unit)
        {
            string msg = $"这里有部队存在,不能创建城市!";
            UIManager.Instance.SystemTips(msg,PanelSystemTips.MessageType.Error);
            GameRoomManager.Instance.Log("CmdBuildCity - " + msg);
            return;
        }
        UrbanCity city = GameRoomManager.Instance.RoomLogic.UrbanManager.CreateCityHere(cell);
        if (city == null)
        {
            string msg = "这个位置无法创建城市!";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            GameRoomManager.Instance.Log("CmdBuildCity - " + msg);
            return;
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
            GameRoomManager.Instance.Log("AskBuildCity - 申请创建城市...");
        }
    }
    public void Stop()
    {
    }
}

using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;

public class CmdAbandonCity : MonoBehaviour, ICommand
{
    public GameObject Cmd{set;get;}

    public int CanRun ()
    {
        return 1;
    }
    public void Run()
    {
        // 看看行动点够不够
        if (!CmdAttack.IsActionPointGranted())
        {
            string msg = "行动点数不够, 本操作无法执行! ";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            Debug.Log("CmdAbandonCity Run Error - " + msg);
            Stop();
            return;
        }
        string title = "提示";
        string content = "你要废弃这座城市吗?"; 
        UIManager.Instance.MessageBox(title, content, (int)PanelMessageBox.BUTTON.YES|(int)PanelMessageBox.BUTTON.NO, OnConfirm);
    }
    public void Tick()
    {
    }
    public void Stop()
    {
    }

    public void OnConfirm(int button)
    {
        switch ((PanelMessageBox.BUTTON)button)
        {
            case PanelMessageBox.BUTTON.YES:
            {
                var pi = CommandManager.Instance.CurrentExecuter;
                if (pi != null)
                {
                    var city = pi.CurrentCity;
                    CityRemove output = new CityRemove()
                    {
                        RoomId = city.RoomId,
                        OwnerId = city.OwnerId,
                        CityId = city.CityId,
                    }; 
                    GameRoomManager.Instance.SendMsg(ROOM.CityRemove, output.ToByteArray());
                    // 消耗行动点 
                    long roomId = city.RoomId;
                    long ownerId = city.OwnerId;
                    long cityId = city.CityId;
                    int commandId = (int) CommandManager.Instance.RunningCommandId;
                    int actionPointCost = CommandManager.Instance.RunningCommandActionPoint;
                    CmdAttack.TryCommand(roomId, ownerId, cityId, commandId, actionPointCost);
                }
            }
                break;
            case PanelMessageBox.BUTTON.NO:
                break;
        }

        Stop();
    }
}

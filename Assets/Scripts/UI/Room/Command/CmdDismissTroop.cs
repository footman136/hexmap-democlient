using System.Collections;
using System.Collections.Generic;
using Animation;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;

public class CmdDismissTroop : MonoBehaviour, ICommand
{
    public GameObject Cmd{set;get;}

    public int CanRun ()
    {
        return 1;
    }
    public void Run()
    {
        string title = "提示";
        string content = "你要解散这支部队吗?"; 
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
                    var av = pi.CurrentActor;
                    if (av)
                    {
                        ActorRemove output = new ActorRemove()
                        {
                            RoomId = av.RoomId,
                            OwnerId = av.OwnerId,
                            ActorId = av.ActorId,
                        };
                        GameRoomManager.Instance.SendMsg(ROOM.ActorRemove, output.ToByteArray());
                    }
                }
            }
                break;
            case PanelMessageBox.BUTTON.NO:
                break;
        }

        Stop();
    }
}

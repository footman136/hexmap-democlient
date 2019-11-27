using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FSMStateActor;

public class CmdHalt : MonoBehaviour, ICommand
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
            return;
        }
        
        var avMe = CommandManager.Instance.CurrentExecuter.CurrentActor;
        if (!avMe)
            return;
        CmdAttack.SendAiStateHigh(avMe.OwnerId, avMe.ActorId, StateEnum.IDLE);
    }
    public void Tick()
    {
    }
    public void Stop()
    {
    }
}

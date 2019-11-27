using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;
using static FSMStateActor;

public class CmdHarvest : MonoBehaviour, ICommand
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
        if (avMe == null)
            return;

        var currentCell = avMe.HexUnit.Location;
        var resType = currentCell.Res.ResType;
        int resAmount = currentCell.Res.GetAmount(resType);
        float durationTime = resAmount * 3.0f;
        if (resAmount <= 0)
        {
            string msg = $"本地没有任何资源,请去其他地方采集!";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Warning);
            return;
        }

        // 看看行动点够不够
        if (!CmdAttack.IsActionPointGranted())
        {
            string msg = "行动点数不够, 本操作无法执行! ";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            Debug.Log("CmdHarvest Run Error - " + msg);
            Stop();
            return;
        }

        {
            HarvestStart output = new HarvestStart()
            {
                RoomId = avMe.RoomId,
                OwnerId = avMe.OwnerId,
                ActorId = avMe.ActorId,
                CellIndex = avMe.CellIndex,
                ResType = (int)resType,
                ResRemain = resAmount,
                DurationTime = durationTime,
            };
            GameRoomManager.Instance.SendMsg(ROOM.HarvestStart, output.ToByteArray());

            CmdAttack.SendAiStateHigh(avMe.OwnerId, avMe.ActorId, StateEnum.HARVEST, 0, 0, durationTime, durationTime);
            // 消耗行动点 
            CmdAttack.TryCommand();
        }
    }
    public void Tick()
    {
    }
    public void Stop()
    {
        var av = CommandManager.Instance.CurrentExecuter.CurrentActor;
        if (av == null)
            return;
        var ab = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(av.ActorId);
        ab?.StateMachine.TriggerTransition(FSMStateActor.StateEnum.IDLE);
    }
}

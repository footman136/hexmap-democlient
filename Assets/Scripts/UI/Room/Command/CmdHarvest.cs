using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;

public class CmdHarvest : MonoBehaviour, ICommand
{
    public GameObject Cmd{set;get;}

    public int CanRun ()
    {
        return 1;
    }
    public void Run()
    {
        var av = CommandManager.Instance.CurrentExecuter.CurrentActor;
        if (av == null)
            return;
        var ab = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(av.ActorId);
        if (ab == null)
            return;

        var currentCell = av.HexUnit.Location;
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
                RoomId = av.RoomId,
                OwnerId = av.OwnerId,
                ActorId = av.ActorId,
                CellIndex = av.CellIndex,
                ResType = (int)resType,
                ResRemain = resAmount,
                DurationTime = durationTime,
            };
            GameRoomManager.Instance.SendMsg(ROOM.HarvestStart, output.ToByteArray());

            ab.StateMachine.TriggerTransition(FSMStateActor.StateEnum.HARVEST, 0, durationTime);
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

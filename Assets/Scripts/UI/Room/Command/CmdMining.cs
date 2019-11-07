using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;

public class CmdMining : MonoBehaviour, ICommand
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
        var durationTime = resAmount * 0.1f;
        if (resAmount > 0)
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

            ab.StateMachine.TriggerTransition(FSMStateActor.StateEnum.HARVEST, null, durationTime);
        }
        else
        {
            string msg = $"本地没有任何资源,请去其他地方采集!";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Warning);
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

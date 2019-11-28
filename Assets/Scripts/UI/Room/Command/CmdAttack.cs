using System.Collections;
using System.Collections.Generic;
using AI;
using GameUtils;
using Google.Protobuf;
using Microsoft.Applications.Events.DataModels;
using Protobuf.Room;
using UnityEngine;
using static FSMStateActor;

public class CmdAttack : MonoBehaviour, ICommand
{
    public GameObject Cmd { set; get; }

    public int CanRun()
    {
        return 1;
    }

    public void Run()
    {
        CursorManager.Instance.ShowCursor(CursorManager.CURSOR_TYPE.ATTACK);
        CommandManager.Instance.CommandTargetSelected += OnCommandTargetSelected;
        if (Cmd)
        {
            var ci = Cmd.GetComponent<CommandItem>();
            if (ci)
                ci.Select(true);
        }
    }

    public void Tick()
    {
    }

    public void Stop()
    {
        CursorManager.Instance.RestoreCursor();
        CommandManager.Instance.CommandTargetSelected -= OnCommandTargetSelected;
        if (!Cmd) return;
        var ci = Cmd.GetComponent<CommandItem>();
        if (ci)
            ci.Select(false);
    }

    private void OnCommandTargetSelected(PickInfo piTarget)
    {
        // 执行
        DoAttack(piTarget);
        TryCommand();
        Stop();
    }

    public static bool IsActionPointGranted()
    {
        return GameRoomManager.Instance.CurrentPlayer.ActionPoint >= CommandManager.Instance.RunningCommandActionPoint;
    }

    public static void TryCommand()
    {
        long roomId = GameRoomManager.Instance.RoomId;
        long ownerId = GameRoomManager.Instance.CurrentPlayer.TokenId;
        if (CommandManager.Instance.CurrentExecuter != null && CommandManager.Instance.CurrentExecuter.CurrentActor != null)
        {
            long actorId = CommandManager.Instance.CurrentExecuter.CurrentActor.ActorId;
            int commandId = (int) CommandManager.Instance.RunningCommandId;
            int actionPointCost = CommandManager.Instance.RunningCommandActionPoint;
            TryCommand(roomId, ownerId, actorId, commandId, actionPointCost);
        }
    }

    public static void TryCommand(long roomId, long ownerId, long actorId, int commandId, int actionPointCost)
    {
        // 正规流程,应该是先向服务器申请行动点是否足够,等待服务器确认以后再真正地执行
        // 但是这样会导致服务器反应较为迟钝,而且客户端逻辑相对复杂,所有指令都要经过这样"申请/确认"的流程
        // 所以,这里先再客户端自己确认行动点是否足够以后,就先执行了,然后再发送执行消耗行动点
        // 如果服务器返回失败,则停止刚才的行为. 当然,这样做可能会导致之前的行动被打断,但是理论上服务器被驳回的几率较小,可以忽略
        TryCommand output = new TryCommand()
        {
            RoomId = roomId,
            OwnerId = ownerId,
            ActorId = actorId,
            CommandId = commandId,
            ActionPointCost = actionPointCost,
        };
    
        GameRoomManager.Instance.SendMsg(ROOM.TryCommand, output.ToByteArray());
    }

    public static void SendAiStateHigh(long playerId, long actorId, StateEnum aiState, int targetCellIndex = 0, long targetId = 0, float durationTime = 0f, float totalTime = 0f)
    {
        ActorAiStateHigh output = new ActorAiStateHigh()
        {
            RoomId = GameRoomManager.Instance.RoomId, // 肯定是在同一个房间里
            OwnerId = playerId,
            ActorId = actorId,
            HighAiState = (int)aiState,
            HighAiCellIndexTo = targetCellIndex,
            HighAiTargetId = targetId,
            HighAiDurationTime = durationTime,
            HighAiTotalTime = totalTime,
        };
        GameRoomManager.Instance.SendMsg(ROOM.ActorAiStateHigh, output.ToByteArray());
    }

    private void DoAttack(PickInfo piTarget)
    {
        // 看看行动点够不够
        if (!IsActionPointGranted())
        {
            string msg = "行动点数不够, 本操作无法执行! ";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            Debug.Log("CmdAttack DoAttack Error - " + msg);
            return;
        }

        var pi = CommandManager.Instance.CurrentExecuter;
        if (pi == null || !pi.CurrentActor)
        {
            string msg = $"没有选中任何部队!";
            UIManager.Instance.SystemTips(msg,PanelSystemTips.MessageType.Error);
            Debug.Log("CmdAttack DoAttack Error - " + msg);
            return;
        }
        
        var avMe = CommandManager.Instance.CurrentExecuter.CurrentActor;
        if (!avMe)
            return;
        var avTarget = piTarget.CurrentActor;
        
        HexCell cellTarget = piTarget.CurrentCell;
        if (!cellTarget)
            return;
        
        var abMe = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(avMe.ActorId);
        if (abMe == null)
            return;
        
        ////////////////////
        // 如果目标点在射程以内,则直接攻击
        if (avTarget)
        {
            if (avMe.IsEnemyInRange(avTarget) && avMe.OwnerId != avTarget.OwnerId)
            {
                // 这里其实应该发送TroopAiState消息到服务器,而不是直接操作状态机,但是因为状态机目前均行在本地,所以就直接调用了
                abMe.IsCounterAttack = false; // 这是主动攻击, 不是反击, 记录在自己身上, Stop的时候用
                abMe.StateMachine.TriggerTransition(StateEnum.FIGHT, cellTarget.Index, piTarget.CurrentActor.ActorId, abMe.AttackDuration);
                return;
            }
        }
        
        ////////////////////
        // 敌人不在附近,则需要找过去 
        var hexmapHelper = GameRoomManager.Instance.HexmapHelper;
        var cellMe = avMe.HexUnit.Location;
        cellTarget = hexmapHelper.TryFindADest(avMe.HexUnit.Location, cellTarget);
        
        GameRoomManager.Instance.HexmapHelper.hexGrid.FindPath(cellMe, cellTarget, avMe.HexUnit);
        if (!hexmapHelper.hexGrid.HasPath)
        {// 如果选中的是一个单位，则需要走到该单位的相邻点上去
            Debug.Log($"CmdAttack DoAttack Error - Cannot go to target position:<{cellMe.coordinates.X},{cellMe.coordinates.Z}> ");
            return;
        }

        if ( avTarget && avTarget.OwnerId != avMe.OwnerId)
        {// 目标点是一支部队,且是敌人的部队,则盯住这支部队猛打
            //abMe.StateMachine.TriggerTransition(StateEnum.WALKFIGHT, cellTarget.Index, avTarget.ActorId);
            SendAiStateHigh(avMe.OwnerId, avMe.ActorId, StateEnum.WALKFIGHT, cellTarget.Index, avTarget.ActorId);
        }
        else
        {// 目标点仅仅是一个位置坐标,则在行军过程中,搜索进攻,发现任意敌人就停下来打它
            //abMe.StateMachine.TriggerTransition(StateEnum.WALKFIGHT, cellTarget.Index);
            SendAiStateHigh(avMe.OwnerId, avMe.ActorId, StateEnum.WALKFIGHT, cellTarget.Index);
        }
        
        Debug.Log($"CmdAttack DoAttack OK - From<{avMe.PosX},{avMe.PosZ}> - Dest Pos<{cellTarget.coordinates.X},{cellTarget.coordinates.Z}>");
    }
}

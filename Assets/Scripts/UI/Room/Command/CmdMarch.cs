using System.Collections;
using System.Collections.Generic;
using Animation;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;

public class CmdMarch : MonoBehaviour, ICommand
{
    public GameObject Cmd{set;get;}

    public int CanRun ()
    {
        return 1;
    }
    public void Run()
    {
        PanelRoomMain.Instance.ShowCursor(PanelRoomMain.CURSOR_TYPE.FIND_PATH);
        CommandManager.Instance.CommandTargetSelected += OnCommandTargetSelected;
        if (Cmd)
        {
            var ci = Cmd.GetComponent<CommandItem>();
            if (ci)
                ci.Select(true);
        }
    }
    public void Stop()
    {
        PanelRoomMain.Instance.ShowCursor(PanelRoomMain.CURSOR_TYPE.NONE);
        CommandManager.Instance.CommandTargetSelected -= OnCommandTargetSelected;
        if (Cmd)
        {
            var ci = Cmd?.GetComponent<CommandItem>();
            if (ci)
                ci.Select(false);
        }
    }

    private void OnCommandTargetSelected(PickInfo piTarget)
    {
        HexUnit whoMove = CommandManager.Instance.CurrentExecuter.CurrentUnit;
        if (!whoMove)
            return;
        HexCell cellTarget = piTarget.CurrentCell;
        if (!cellTarget)
            return;
        {
            var currentCell = whoMove.Location;
            GameRoomManager.Instance.HexmapHelper.hexGrid.FindPath(currentCell, cellTarget, whoMove);
            var hexmapHelper = GameRoomManager.Instance.HexmapHelper;
            if (!hexmapHelper.hexGrid.HasPath)
                return;
            var av = whoMove.GetComponent<ActorVisualizer>();
            if (av == null)
                return;

//            TroopMove output = new TroopMove()
//            {
//                RoomId = GameRoomManager.Instance.RoomId,
//                OwnerId = GameRoomManager.Instance.CurrentPlayer.TokenId,
//                ActorId = av.ActorId,
//                PosFromX = av.PosX,
//                PosFromZ = av.PosZ,
//                PosToX = cellTarget.coordinates.X,
//                PosToZ = cellTarget.coordinates.Z,
//            };
//            GameRoomManager.Instance.SendMsg(ROOM.TroopMove, output.ToByteArray());
            
            var ab = GameRoomManager.Instance.RoomLogic.ActorManager.GetPlayer(av.ActorId);
            if (ab == null)
                return;
            HexCell newCell = hexmapHelper.hexGrid.GetCell(currentCell.coordinates.X, currentCell.coordinates.Z);
            HexCell newCell2 = hexmapHelper.hexGrid.GetCell(currentCell.Position);
            if (newCell.Position != currentCell.Position)
            {
                Debug.LogError($"Fuck Hexmap!!! - Orgin<{currentCell.coordinates.X},{currentCell.coordinates.Z}> - New<{newCell.coordinates.X},{newCell.coordinates.Z}>");
            }
            if (newCell2.Position != currentCell.Position)
            {
                Debug.LogError($"Fuck Hexmap 2!!! - Orgin<{currentCell.coordinates.X},{currentCell.coordinates.Z}> - New2<{newCell2.coordinates.X},{newCell2.coordinates.Z}>");
            }
            ab.SetTarget(cellTarget.Position);
        
            Debug.Log($"MY BY MYSELF - Dest<{cellTarget.coordinates.X},{cellTarget.coordinates.Z}> - Dest Pos<{ab.TargetPosition.x},{ab.TargetPosition.z}>");
            ab.StateMachine.TriggerTransition(FSMStateActor.StateEnum.WALK); 
        }
        Stop();
    }
}

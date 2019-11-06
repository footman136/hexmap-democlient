using System.Collections;
using System.Collections.Generic;
using Animation;
using Google.Protobuf;
using Protobuf.Room;
using UnityEditor.Experimental.GraphView;
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
        CursorManager.Instance.ShowCursor(CursorManager.CURSOR_TYPE.FIND_PATH);
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
        if (!CommandManager.Instance)
            return;
        var whoMove = CommandManager.Instance.CurrentExecuter.CurrentActor;
        if (!whoMove)
            return;
        if (whoMove.CurrentAiState == FSMStateActor.StateEnum.WALK
            || whoMove.CurrentAiState == FSMStateActor.StateEnum.FIGHT)
        {
            for (int i = 0; i < PanelRoomMain.Instance.CommandContainer.childCount; ++i)
            {
                var ci = PanelRoomMain.Instance.CommandContainer.GetChild(i).GetComponent<CommandItem>();
                if (ci && ci.CmdId < CommandManager.CommandID.March)
                {
                    ci.Enable(false);
                }
            }
        }
        else if (whoMove.CurrentAiState == FSMStateActor.StateEnum.HARVEST)
        {
            for (int i = 0; i < PanelRoomMain.Instance.CommandContainer.childCount; ++i)
            {
                var ci = PanelRoomMain.Instance.CommandContainer.GetChild(i).GetComponent<CommandItem>();
                if (ci && ci.CmdId != CommandManager.CommandID.Halt)
                {
                    ci.Enable(false);
                }
            }
        }
        else
        {
            for (int i = 0; i < PanelRoomMain.Instance.CommandContainer.childCount; ++i)
            {
                var ci = PanelRoomMain.Instance.CommandContainer.GetChild(i).GetComponent<CommandItem>();
                if (ci)
                {
                    if (ci.CmdId == CommandManager.CommandID.Lumberjack)
                    {
                        var cell = whoMove.HexUnit.Location;
                        if (cell && cell.Res.GetAmount(HexResource.RESOURCE_TYPE.WOOD) > 0)
                        {
                            ci.Enable(true);
                        }
                        else
                        {
                            ci.Enable(false);    
                        }
                    }
                    else if (ci.CmdId == CommandManager.CommandID.Harvest)
                    {
                        var cell = whoMove.HexUnit.Location;
                        if (cell && cell.Res.GetAmount(HexResource.RESOURCE_TYPE.FOOD) > 0)
                        {
                            ci.Enable(true);
                        }
                        else
                        {
                            ci.Enable(false);    
                        }
                    }
                    else if (ci.CmdId == CommandManager.CommandID.Mining)
                    {
                        
                        var cell = whoMove.HexUnit.Location;
                        if (cell && cell.Res.GetAmount(HexResource.RESOURCE_TYPE.IRON) > 0)
                        {
                            ci.Enable(true);
                        }
                        else
                        {
                            ci.Enable(false);    
                        }
                    }
                    else if (ci.CmdId == CommandManager.CommandID.BuildBridge)
                    {
                        var cell = whoMove.HexUnit.Location;
                        if (cell && cell.HasRiver && !cell.HasBridge)
                        {
                            ci.Enable(true);
                        }
                        else
                        {
                            ci.Enable(false);
                        }
                    }
                    else
                    {
                        ci.Enable(true);
                    }
                }
            }
        }
    }
    public void Stop()
    {
        CursorManager.Instance.ShowCursor(CursorManager.CURSOR_TYPE.NONE);
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
        var whoMove = CommandManager.Instance.CurrentExecuter.CurrentActor;
        if (!whoMove)
            return;
        HexCell cellTarget = piTarget.CurrentCell;
        if (!cellTarget)
            return;
        {
            var currentCell = whoMove.HexUnit.Location;
            GameRoomManager.Instance.HexmapHelper.hexGrid.FindPath(currentCell, cellTarget, whoMove.HexUnit);
            var hexmapHelper = GameRoomManager.Instance.HexmapHelper;
            if (!hexmapHelper.hexGrid.HasPath)
                return;
            var av = whoMove;

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
            
            var ab = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(av.ActorId);
            if (ab == null)
                return;
            HexCell newCell = hexmapHelper.hexGrid.GetCell(currentCell.coordinates.X, currentCell.coordinates.Z);
            HexCell newCell2 = hexmapHelper.hexGrid.GetCell(currentCell.Position);
            if (newCell.Position != currentCell.Position)
            {
                Debug.LogError($"OhNo Hexmap!!! - Orgin<{currentCell.coordinates.X},{currentCell.coordinates.Z}> - New<{newCell.coordinates.X},{newCell.coordinates.Z}>");
            }
            if (newCell2.Position != currentCell.Position)
            {
                Debug.LogError($"OhNo Hexmap 2!!! - Orgin<{currentCell.coordinates.X},{currentCell.coordinates.Z}> - New2<{newCell2.coordinates.X},{newCell2.coordinates.Z}>");
            }
            ab.SetTarget(cellTarget.Position);
        
            Debug.Log($"MY BY MYSELF - Dest<{cellTarget.coordinates.X},{cellTarget.coordinates.Z}> - Dest Pos<{ab.TargetPosition.x},{ab.TargetPosition.z}>");
            ab.StateMachine.TriggerTransition(FSMStateActor.StateEnum.WALK);
        }
        Stop();
    }
}

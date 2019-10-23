using System.Collections;
using System.Collections.Generic;
using Animation;
using Google.Protobuf;
using Main;
using Protobuf.Room;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;

public class PanelRoomMain : MonoBehaviour
{
    [SerializeField] private HexGrid hexGrid;
    [SerializeField] private Texture2D _curCreateActor;
    [SerializeField] private Texture2D _curDestroyActor;
    [SerializeField] private Texture2D _curFindPath;
    
    HexCell currentCell;
    HexUnit selectedUnit;
    
    public enum CommandType
    {
        CMD_NONE = 0,
        CMD_CREATE_ACTOR = 1,
        CMD_DESTROY_ACTOR = 2,
        CMD_FIND_PATH = 3,
    };

    public CommandType _commandType;
    
    // Start is called before the first frame update
    void Start()
    {
        // 这一行，查了两个小时。。。如果没有，打包客户端后，地表看不到任何颜色，都是灰色。
        Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
        _commandType = CommandType.CMD_NONE;
    }

    #region 鼠标操作
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            SetCommand(CommandType.CMD_NONE, false);
        }

        // 本函数用来判定，是否点击到了界面，只有没有点击界面，才处理战场内的事件
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        if (_commandType == CommandType.CMD_CREATE_ACTOR)
        {
            if (Input.GetMouseButtonUp(0))
            {
                var ret = AskCreateUnit("Troop_Cityguard");
                if (!ret)
                    SetCommand(CommandType.CMD_NONE);
                selectedUnit = null;
            }
        }
        else if (_commandType == CommandType.CMD_DESTROY_ACTOR)
        {
            if (Input.GetMouseButtonUp(0))
            {
                var ret = AskDestroyUnit();
                if (!ret)
                    SetCommand(CommandType.CMD_NONE);
                selectedUnit = null;
            }
        }
        if(_commandType == CommandType.CMD_NONE)
        {
            if (Input.GetMouseButtonUp(0))
            {
                DoSelection();
            }
            else if (selectedUnit)
            {
                if (Input.GetMouseButtonUp(1)) 
                {
                    DoMove();
                }
                else 
                {
                    DoPathfinding();
                }
            }
        }
    }

    HexCell GetCellUnderCursor () {
        return
            hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
    }

    HexCell GetCell(int posX, int posZ)
    {
        return hexGrid.GetCell(new HexCoordinates(posX, posZ));
    }

    bool AskCreateUnit(string unitName)
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && !cell.Unit)
        {
            CreateATroop output = new CreateATroop()
            {
                RoomId = GameRoomManager.Instance.RoomId,
                OwnerId = GameRoomManager.Instance.CurrentPlayer.TokenId,
                ActorId = GameUtils.Utils.GuidToLongId(),
                PosX = cell.coordinates.X,
                PosZ = cell.coordinates.Z,
                Orientation = Random.Range(0f, 360f),
                Species = unitName, // 预制件的名字
            };
            GameRoomManager.Instance.SendMsg(ROOM.CreateAtroop, output.ToByteArray());
            return true;
        }

        return false;
    }

    bool AskDestroyUnit()
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && cell.Unit)
        {
            HexUnit hu = cell.Unit;
            var av = hu.gameObject.GetComponent<ActorVisualizer>();
            if (av != null)
            {
                DestroyATroop output = new DestroyATroop()
                {
                    RoomId = GameRoomManager.Instance.RoomId,
                    OwnerId = GameRoomManager.Instance.CurrentPlayer.TokenId,
                    ActorId = av.ActorId,
                };
                GameRoomManager.Instance.SendMsg(ROOM.DestroyAtroop, output.ToByteArray());
                return true;
            }
        }

        return false;
    }

    void DoSelection () {
        hexGrid.ClearPath();
        UpdateCurrentCell();
        if (currentCell) {
            selectedUnit = currentCell.Unit;
        }
    }

    void DoPathfinding () {
        if (UpdateCurrentCell()) {
            if (currentCell && selectedUnit.IsValidDestination(currentCell)) {
                hexGrid.FindPath(selectedUnit.Location, currentCell, selectedUnit);
            }
            else {
                hexGrid.ClearPath();
            }
        }
    }

    void DoMove () {
        if (hexGrid.HasPath) {
            selectedUnit.Travel(hexGrid.GetPath());
            hexGrid.ClearPath();
        }
    }

    bool UpdateCurrentCell () {
        HexCell cell =
            hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
        if (cell != currentCell) {
            currentCell = cell;
            return true;
        }
        return false;
    }
    #endregion

    #region 状态改变

        void SetCommand(CommandType command, bool bSetCursor = true)
    {
        _commandType = command;
        if (!bSetCursor)
            return;
        switch (command)
        {
            case CommandType.CMD_NONE:
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                break;
            case CommandType.CMD_CREATE_ACTOR:
                Cursor.SetCursor(_curCreateActor, Vector2.zero, CursorMode.Auto);
                break;
            case CommandType.CMD_DESTROY_ACTOR:
                Cursor.SetCursor(_curDestroyActor, Vector2.zero, CursorMode.Auto);
                break;
            case CommandType.CMD_FIND_PATH:
                Cursor.SetCursor(_curFindPath, Vector2.zero, CursorMode.Auto);
                break;
        }
    }
    #endregion

    #region 事件处理
    public void OnClickExit()
    {
        LeaveRoom output = new LeaveRoom()
        {
            RoomId = GameRoomManager.Instance.RoomId,
            ReleaseIfNoUser = true,
        };
        if(GameRoomManager.Instance)
            GameRoomManager.Instance.SendMsg(ROOM.LeaveRoom, output.ToByteArray());
    }

    public void OnClickCreateActor()
    {
        SetCommand(CommandType.CMD_CREATE_ACTOR);
    }
    
    public void OnClickDestroyActor()
    {
        SetCommand(CommandType.CMD_DESTROY_ACTOR);
    }
    #endregion
}

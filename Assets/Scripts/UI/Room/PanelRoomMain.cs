using System.Collections;
using System.Collections.Generic;
using Animation;
using AI;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
using Toggle = UnityEngine.UI.Toggle;

public class PanelRoomMain : MonoBehaviour
{
    [SerializeField] private HexmapHelper hexmapHelper;
    [SerializeField] private Texture2D _curCreateActor;
    [SerializeField] private Texture2D _curDestroyActor;
    [SerializeField] private Texture2D _curFindPath;
    
    [SerializeField] private Toggle _togShowGrid;
    [SerializeField] private Toggle _togShowLabel;
    [SerializeField] private Toggle _togAi;
    [SerializeField] private Toggle _togFollowCamera;
    [SerializeField] private Material terrainMaterial;
    
    HexCell currentCell;
    HexUnit selectedUnit;

    private bool _isFollowCamera;
    [SerializeField] private GameObject _selectObjTemplate;
    private GameObject _selectObj;
    [SerializeField] private GameObject _hitGroundTemplate;
    private GameObject _hitGround;
    
    
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
        _togShowGrid.isOn = false;
        _togShowLabel.isOn = false;
        _togAi.isOn = true;

        _selectObj = Instantiate(_selectObjTemplate);
        _selectObj.SetActive(false);
        _hitGround = Instantiate(_hitGroundTemplate);
        _hitGround.SetActive(false);
    }

    #region 鼠标操作

    private int soldierIndex = 0;
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
                string[] soldierNames = new string [24]
                {
                    "Horse_BLUE_CC", "Horse_GREEN_CC", "Horse_RED_CC", "Horse_YELLOW_CC", 
                    "Hunter_BLUE_CC", "Hunter_GREEN_CC", "Hunter_RED_CC", "Hunter_YELLOW_CC", 
                    "Knight_BLUE_CC", "Knight_GREEN_CC", "Knight_RED_CC", "Knight_YELLOW_CC", 
                    "LanceKnight_BLUE_CC", "LanceKnight_GREEN_CC", "LanceKnight_RED_CC", "LanceKnight_YELLOW_CC", 
                    "Leader_BLUE_CC", "Leader_GREEN_CC", "Leader_RED_", "Leader_YELLOW_CC",  
                    "SwordsMan_BLUE_CC", "SwordsMan_GREEN_CC", "SwordsMan_RED_CC", "SwordsMan_YELLOW_CC", 
                };
                var ret = AskCreateUnit(soldierNames[soldierIndex++]);
                soldierIndex = soldierIndex % 24;
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
                if (selectedUnit)
                {
                    //AskMove();
                    if (currentCell && currentCell.Unit == null)
                    {
                        ShowHitGround(Input.mousePosition);
                        MoveByMyself();
                    }
                    else
                    {
                        DoSelection();    
                    }
                }
                else
                {
                    DoSelection();    
                }
            }
            else
            {
                DoPathfinding();
            }
        }
    }

    HexCell GetCellUnderCursor () {
        return
            hexmapHelper.hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
    }

    HexCell GetCell(int posX, int posZ)
    {
        return hexmapHelper.hexGrid.GetCell(new HexCoordinates(posX, posZ));
    }
    
    bool UpdateCurrentCell () {
        HexCell cell =
            hexmapHelper.hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
        if (cell != currentCell) {
            currentCell = cell;
            return true;
        }
        return false;
    }

    private void ShowHitGround(Vector3 position)
    {
        var ray = Camera.main.ScreenPointToRay(position);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            Vector3 point = hit.point;
            point.y += 0.2f;
            _hitGround.transform.position = point;
            _hitGround.SetActive(false);
            _hitGround.SetActive(true);
        }
    }

    private void ShowSelector( HexUnit unit, bool bShow)
    {
        if(unit)
        {
            _selectObj.transform.parent = unit.transform;
            _selectObj.transform.localPosition = Vector3.up * 0.2f;
        }
        _selectObj.SetActive(bShow);
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
                CellIndex = cell.Index,
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
        hexmapHelper.hexGrid.ClearPath();
        UpdateCurrentCell();
        if (currentCell)
        {
            if(selectedUnit)
                hexmapHelper.EnableFollowCamera(selectedUnit, false);
            else
            {
                ShowSelector(null, false);
            }
            selectedUnit = currentCell.Unit;
            if(_isFollowCamera && selectedUnit)
                hexmapHelper.EnableFollowCamera(selectedUnit, true);
            if (selectedUnit)
            {
                ShowSelector(selectedUnit, true);
            }
        }
        else
        {
            ShowSelector(null, false);
        }
    }

    void DoPathfinding (bool calc = false) {
        if (UpdateCurrentCell() || calc) {
            if (currentCell && selectedUnit && selectedUnit.IsValidDestination(currentCell)) {
                hexmapHelper.hexGrid.FindPath(selectedUnit.Location, currentCell, selectedUnit);
            }
            else {
                hexmapHelper.hexGrid.ClearPath();
            }
        }
    }

    void AskMove()
    {
        if (!hexmapHelper.hexGrid.HasPath)
            return;
        if (currentCell == null || selectedUnit == null)
            return;
        var av = selectedUnit.GetComponent<ActorVisualizer>();
        if (av == null)
            return;

        TroopMove output = new TroopMove()
        {
            RoomId = GameRoomManager.Instance.RoomId,
            OwnerId = GameRoomManager.Instance.CurrentPlayer.TokenId,
            ActorId = av.ActorId,
            PosFromX = av.PosX,
            PosFromZ = av.PosZ,
            PosToX = currentCell.coordinates.X,
            PosToZ = currentCell.coordinates.Z,
            Speed = 0.5f,
        };
        GameRoomManager.Instance.SendMsg(ROOM.TroopMove, output.ToByteArray());
    }

    private void MoveByMyself()
    {
        DoPathfinding(true);
        
        if (!hexmapHelper.hexGrid.HasPath)
            return;
        if (currentCell == null || selectedUnit == null)
            return;
        var av = selectedUnit.GetComponent<ActorVisualizer>();
        if (av == null)
            return;
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
        ab.SetTarget(currentCell.Position);
        
        Debug.Log($"MY BY MYSELF - Dest<{currentCell.coordinates.X},{currentCell.coordinates.Z}> - Dest Pos<{ab.TargetPosition.x},{ab.TargetPosition.z}>");
        ab.StateMachine.TriggerTransition(FSMStateActor.StateEnum.WALK); 
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

    public void ToggleShowGrid()
    {
        bool visible = _togShowGrid.isOn;
        if (visible) {
            terrainMaterial.EnableKeyword("GRID_ON");
        }
        else {
            terrainMaterial.DisableKeyword("GRID_ON");
        }
    }
    public void ToggleShowLabel()
    {
        bool visible = _togShowLabel.isOn;
        hexmapHelper.hexGrid.showLabel = visible;
        hexmapHelper.hexGrid.OnShowLabels(visible);
    }
    public void ToggleAI(bool isOnOn)
    {
        bool isOn = _togAi.isOn;
        GameRoomManager.Instance.IsAiOn = isOn;
        if (isOn)
        {
            Debug.Log("AI is On!!!");
        }
        else
        {
            Debug.Log("AI is Off!!!");
        }
    }

    public void ToggleFollowCamera()
    {
        bool bFollow = _togFollowCamera.isOn;
        if (selectedUnit)
        {
            hexmapHelper.EnableFollowCamera(selectedUnit, bFollow);
        }

        _isFollowCamera = bFollow;
    }

    #endregion
}

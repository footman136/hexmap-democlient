using System.Collections;
using System.Collections.Generic;
using Animation;
using AI;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;
using UnityEngine.EventSystems;
using static PanelCommands;
using Cursor = UnityEngine.Cursor;
using Toggle = UnityEngine.UI.Toggle;

public class PanelRoomMain : MonoBehaviour
{
    [SerializeField] private HexmapHelper hexmapHelper;
    [SerializeField] private Texture2D _curCreateActor;
    [SerializeField] private Texture2D _curDestroyActor;
    [SerializeField] private Texture2D _curFindPath;
    [SerializeField] private Texture2D _curBuildCity;
    
    [SerializeField] private Toggle _togShowGrid;
    [SerializeField] private Toggle _togShowLabel;
    [SerializeField] private Toggle _togAi;
    [SerializeField] private Toggle _togFollowCamera;
    [SerializeField] private Toggle _togShowRes;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private PanelCommands _commands;
    
    private HexCell currentCell;

    private bool _isFollowCamera;
    [SerializeField] private GameObject _selectObjTemplate;
    private GameObject _selectObj;
    [SerializeField] private GameObject _hitGroundTemplate;
    private GameObject _hitGround;

    public PickInfo _pickInfoMaster;// 发动指令的对象
    public PickInfo _pickInfoApprentice; // 被发动指令的对象
    
    // Start is called before the first frame update
    void Start()
    {
        // 这一行，查了两个小时。。。如果没有，打包客户端后，地表看不到任何颜色，都是灰色。
        Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
        _togShowGrid.isOn = false;
        _togShowLabel.isOn = false;
        _togAi.isOn = true;

        _selectObj = Instantiate(_selectObjTemplate);
        _selectObj.SetActive(false);
        _hitGround = Instantiate(_hitGroundTemplate);
        _hitGround.SetActive(false);
        _commands.gameObject.SetActive(false);
        
        _pickInfoMaster = new PickInfo();
        _pickInfoApprentice = new PickInfo();
    }

    #region 鼠标操作

    private int soldierIndex = 0;
    // Update is called once per frame
    void Update()
    {
        // 本函数用来判定，是否点击到了界面，只有没有点击界面，才处理战场内的事件
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonUp(0))
        {
            DoSelection();
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
        _selectObj.SetActive(bShow);
        if(unit)
        {
            bShow = bShow & _isFollowCamera;
            _selectObj.transform.parent = unit.transform;
            _selectObj.transform.localPosition = Vector3.up * 0.2f;
            hexmapHelper.EnableFollowCamera(unit, bShow);
            SelectCircle sc = _selectObj.GetComponent<SelectCircle>();
            if (sc)
            {
                sc.SetSize(2);
            }
        }
    }

    private void ShowSelectorCity( UrbanCity city, bool bShow)
    {
        _selectObj.SetActive(bShow);
        if(city != null)
        {
            HexCell cell = hexmapHelper.GetCell(city.CellIndex);
            if (cell)
            {
                _selectObj.transform.parent = cell.transform;
                _selectObj.transform.localPosition = Vector3.up * 0.2f;
                SelectCircle sc = _selectObj.GetComponent<SelectCircle>();
                if (sc)
                {
                    if(city.CitySize == 1)
                        sc.SetSize(20);
                    else
                        sc.SetSize(8);
                }
            }
        }
    }

    bool AskCreateUnit(int actorInfoId, string unitName)
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
                ActorInfoId = actorInfoId,
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

    bool AskBuildCity()
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && cell.Unit)
        {
            string msg = $"这里有部队存在,不能创建城市!";
            UIManager.Instance.SystemTips(msg,PanelSystemTips.MessageType.Error);
            GameRoomManager.Instance.Log("AskBuildCity - " + msg);
            return false;
        }
        UrbanCity city = GameRoomManager.Instance.RoomLogic.UrbanManager.CreateCityHere(cell);
        if (city == null)
        {
            string msg = "无法创建城市!";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
            GameRoomManager.Instance.Log("AskBuildCity - " + msg);
            return false;
        }
        else
        {
            CityAdd output = new CityAdd()
            {
                RoomId = city.RoomId,
                OwnerId = city.OwnerId,
                CityId = city.CityId,
                PosX = city.PosX,
                PosZ = city.PosZ,
                CellIndex= city.CellIndex,
                CityName = city.CityName,
                CitySize = city.CitySize,
            };
            GameRoomManager.Instance.SendMsg(ROOM.CityAdd, output.ToByteArray());
            GameRoomManager.Instance.Log("AskBuildCity - 申请创建城市...");
        }
        return true;
    }

    void DoSelection () {
        hexmapHelper.hexGrid.ClearPath();
        UpdateCurrentCell();
        _pickInfoMaster.Clear();
        if (currentCell)
        {
            _pickInfoMaster.CurrentCell = currentCell;
            if (currentCell.Unit)
            {
                _pickInfoMaster.CurrentUnit = currentCell.Unit;
            }
            else
            { //选择城市
                if (currentCell.UrbanLevel > 0)
                {
                    var city = GameRoomManager.Instance.RoomLogic.UrbanManager.FindCity(currentCell);
                    _pickInfoMaster.CurrentCity = city;
                }
            }
        }

        ShowSelector(null, false);
        _commands.SetSelector(_pickInfoMaster);
        if (_pickInfoMaster.CurrentCity != null)
        {
            ShowSelectorCity(_pickInfoMaster.CurrentCity, true);            
        }
        else if (_pickInfoMaster.CurrentUnit)
        {
            ShowSelector(_pickInfoMaster.CurrentUnit, true);    
        }
    }

    void DoPathfinding (bool calc = false) {
        if (UpdateCurrentCell() || calc) {
            if (currentCell && _pickInfoMaster.CurrentUnit && _pickInfoMaster.CurrentUnit.IsValidDestination(currentCell)) {
                hexmapHelper.hexGrid.FindPath(_pickInfoMaster.CurrentUnit.Location, currentCell, _pickInfoMaster.CurrentUnit);
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
        if (currentCell == null || _pickInfoMaster.CurrentUnit == null)
            return;
        var av = _pickInfoMaster.CurrentUnit.GetComponent<ActorVisualizer>();
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
        };
        GameRoomManager.Instance.SendMsg(ROOM.TroopMove, output.ToByteArray());
    }

    private void MoveByMyself()
    {
        DoPathfinding(true);
        
        if (!hexmapHelper.hexGrid.HasPath)
            return;
        if (currentCell == null || _pickInfoMaster.CurrentUnit == null)
            return;
        var av = _pickInfoMaster.CurrentUnit.GetComponent<ActorVisualizer>();
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
        hexmapHelper.hexGrid.showLabel = visible?1:0;
        hexmapHelper.hexGrid.OnShowLabels(visible?1:0);
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
        if (_pickInfoMaster.CurrentUnit)
        {
            hexmapHelper.EnableFollowCamera(_pickInfoMaster.CurrentUnit, bFollow);
        }

        _isFollowCamera = bFollow;
    }

    public void ToggleShowRes()
    {
        bool visible = _togShowRes.isOn;
        hexmapHelper.hexGrid.showLabel = visible?2:0;
        hexmapHelper.hexGrid.OnShowLabels(visible?2:0);
    }

    #endregion
}

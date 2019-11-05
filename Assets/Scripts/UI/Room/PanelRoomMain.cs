using System.Collections;
using System.Collections.Generic;
using Animation;
using AI;
using Google.Protobuf;
using PlayFab.MultiplayerModels;
using Protobuf.Room;
using UnityEngine;
using UnityEngine.EventSystems;
using static PanelCommands;
using Cursor = UnityEngine.Cursor;
using Toggle = UnityEngine.UI.Toggle;

public class PanelRoomMain : MonoBehaviour
{
    [SerializeField] private HexmapHelper hexmapHelper;
    
    [SerializeField] private Toggle _togShowGrid;
    [SerializeField] private Toggle _togShowLabel;
    [SerializeField] private Toggle _togAi;
    [SerializeField] private Toggle _togFollowCamera;
    [SerializeField] private Toggle _togShowRes;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private PanelCommands _commands;
    
    public Transform CommandContainer => _commands.Container;
    
    private HexCell currentCell;

    private bool _isFollowCamera;
    [SerializeField] private GameObject _selectObjTemplate;
    private GameObject _selectObj;
    [SerializeField] private GameObject _hitGroundTemplate;
    private GameObject _hitGround;

    public PickInfo _pickInfoMaster;// 发动指令的对象,主语
    public PickInfo _pickInfoTarget; // 被发动指令的对象,宾语
    
    private static PanelRoomMain _instance;
    public static PanelRoomMain Instance => _instance;

    void Awake()
    {
        if (_instance)
        {
            Debug.LogError("PanelRoomMain is singlon, cannot be initialized more than once!");
        }
        _instance = this;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        // 这一行，查了两个小时。。。如果没有，打包客户端后，地表看不到任何颜色，都是灰色。
        Shader.EnableKeyword("HEX_MAP_EDIT_MODE");

        _selectObj = Instantiate(_selectObjTemplate);
        _selectObj.SetActive(false);
        _hitGround = Instantiate(_hitGroundTemplate);
        _hitGround.SetActive(false);
        _commands.gameObject.SetActive(false);
        
        _pickInfoMaster = new PickInfo();
        _pickInfoTarget = new PickInfo();
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

        if (CommandManager.Instance.IsCommandRunning())
        {
            DoPathfinding();
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
    
    #endregion;
    
    #region 选中特效

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

    private void ShowSelector( ActorVisualizer av, bool bShow)
    {
        _selectObj.SetActive(bShow);
        if(av)
        {
            bShow = bShow & _isFollowCamera;
            _selectObj.transform.parent = av.transform;
            _selectObj.transform.localPosition = Vector3.up * 0.2f;
            hexmapHelper.EnableFollowCamera(av, bShow);
            SelectCircle sc = _selectObj.GetComponent<SelectCircle>();
            if (sc)
            {
                sc.SetSize(2);
            }
        }
        else
        {
            _selectObj.transform.parent = null;
        }
    }

    public bool IsShowingSelector(ActorVisualizer av)
    {
        if (_selectObj.transform.parent == av.transform)
        {
            return true;
        }

        return false;
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

    #endregion
    
    #region 选中

    void DoSelection () {

        if (!CommandManager.Instance.IsCommandRunning())
        {
            hexmapHelper.hexGrid.ClearPath();
            UpdateCurrentCell();
            SetSelection(currentCell);
        }
        else
        {
            HexCell cell = GetCellUnderCursor();
            SetTarget(cell);
        }
        
    }

    public void SetSelection(HexCell cell)
    {
        _pickInfoMaster.Clear();
        if (cell)
        {
            _pickInfoMaster.CurrentCell = cell;
            if (cell.Unit)
            {
                //_pickInfoMaster.CurrentUnit = cell.Unit;
                var av = cell.Unit.GetComponent<ActorVisualizer>();
                if (av != null)
                {
                    _pickInfoMaster.CurrentActor = av;
                }
            }
            else
            {
                if (cell.UrbanLevel > 0)
                {
                    var city = GameRoomManager.Instance.RoomLogic.UrbanManager.FindCity(currentCell);
                    _pickInfoMaster.CurrentCity = city;
                }
            }
            CommandManager.Instance.CurrentExecuter = _pickInfoMaster;// 发送命令的单位
            ShowSelector(null, false);
            _commands.SetSelector(_pickInfoMaster);
            if (_pickInfoMaster.CurrentCity != null)
            {
                ShowSelectorCity(_pickInfoMaster.CurrentCity, true);            
            }
            else if (_pickInfoMaster.CurrentActor)
            {
                ShowSelector(_pickInfoMaster.CurrentActor, true);    
            }
        }
        else
        {
            hexmapHelper.hexGrid.ClearPath();
            ShowSelector(null, false);
            ShowSelectorCity(null, false);
            CursorManager.Instance.ShowCursor(CursorManager.CURSOR_TYPE.NONE);
        }
    }

    public void SetTarget(HexCell cell)
    {
        _pickInfoTarget.Clear();
        if (cell)
        {
            _pickInfoTarget.CurrentCell = cell;
            if (cell.Unit)
            {
                //_pickInfoTarget.CurrentUnit = cell.Unit;
                var av = cell.Unit.GetComponent<ActorVisualizer>();
                if (av != null)
                {
                    _pickInfoMaster.CurrentActor = av;
                }
            }
            else
            {
                if (cell.UrbanLevel > 0)
                {
                    var city = GameRoomManager.Instance.RoomLogic.UrbanManager.FindCity(cell);
                    _pickInfoTarget.CurrentCity = city;
                }
            }
                
            CommandManager.Instance.OnCommandTargetSelected(_pickInfoTarget); // 接受命令的单位
        }
    }

    public void RemoveSelection(long actorId)
    {
        // 这里有点复杂哈,ActorVisualizer是挂接在HexUnit上的,而ActorBehaviour是被ActorManager管理的
        // 所以,如果只知道ActorId的话,只能先找到ActorBehaviour,然后通过HexUnit找到ActorVisualizer
        var ab = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(actorId);
        if (ab != null)
        {
            var av = ab.HexUnit.GetComponent<ActorVisualizer>();
            if (av != null)
            {
                if (IsShowingSelector(av))
                {
                    SetSelection(null);
                }
            }
        }
    }
    
    void DoPathfinding (bool calc = false) {
        if (UpdateCurrentCell() || calc) {
            if (currentCell && _pickInfoMaster.CurrentActor && currentCell.IsValidDestination()) {
                hexmapHelper.hexGrid.FindPath(_pickInfoMaster.CurrentActor.HexUnit.Location, currentCell, _pickInfoMaster.CurrentActor.HexUnit);
            }
            else {
                hexmapHelper.hexGrid.ClearPath();
            }
        }
    }
    
    #endregion
    
    #region 指令
    
    public bool AskCreateUnit(UrbanCity city, int actorInfoId)
    {
        HexCell cellCenter = GameRoomManager.Instance.HexmapHelper.GetCell(city.CellIndex);// 城市中心地块
        if (cellCenter.Unit != null)
        {
            string msg = $"当前位置有一支部队，请把该部队移走，然后再生产部队！";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Warning);
            return false;
        }
        
        var actorInfoTable = CsvDataManager.Instance.GetTable("actor_info");
        string artPrefab = actorInfoTable.GetValue(10001, "ArtPrefab");
        
        CreateATroop output = new CreateATroop()
        {
            RoomId = GameRoomManager.Instance.RoomId,
            OwnerId = GameRoomManager.Instance.CurrentPlayer.TokenId,
            ActorId = GameUtils.Utils.GuidToLongId(),
            PosX = cellCenter.coordinates.X,
            PosZ = cellCenter.coordinates.Z,
            Orientation = Random.Range(0f, 360f),
            Species = artPrefab, // 预制件的名字
            CellIndex = city.CellIndex,
            ActorInfoId = actorInfoId,
        };
        GameRoomManager.Instance.SendMsg(ROOM.CreateAtroop, output.ToByteArray());
        return true;
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

//    void AskMove()
//    {
//        if (!hexmapHelper.hexGrid.HasPath)
//            return;
//        if (currentCell == null || _pickInfoMaster.CurrentUnit == null)
//            return;
//        var av = _pickInfoMaster.CurrentUnit.GetComponent<ActorVisualizer>();
//        if (av == null)
//            return;
//
//        TroopMove output = new TroopMove()
//        {
//            RoomId = GameRoomManager.Instance.RoomId,
//            OwnerId = GameRoomManager.Instance.CurrentPlayer.TokenId,
//            ActorId = av.ActorId,
//            PosFromX = av.PosX,
//            PosFromZ = av.PosZ,
//            PosToX = currentCell.coordinates.X,
//            PosToZ = currentCell.coordinates.Z,
//        };
//        GameRoomManager.Instance.SendMsg(ROOM.TroopMove, output.ToByteArray());
//    }
//
//    private void MoveByMyself()
//    {
//        DoPathfinding(true);
//        
//        if (!hexmapHelper.hexGrid.HasPath)
//            return;
//        if (currentCell == null || _pickInfoMaster.CurrentUnit == null)
//            return;
//        var av = _pickInfoMaster.CurrentUnit.GetComponent<ActorVisualizer>();
//        if (av == null)
//            return;
//        var ab = GameRoomManager.Instance.RoomLogic.ActorManager.GetPlayer(av.ActorId);
//        if (ab == null)
//            return;
//        HexCell newCell = hexmapHelper.hexGrid.GetCell(currentCell.coordinates.X, currentCell.coordinates.Z);
//        HexCell newCell2 = hexmapHelper.hexGrid.GetCell(currentCell.Position);
//        if (newCell.Position != currentCell.Position)
//        {
//            Debug.LogError($"Fuck Hexmap!!! - Orgin<{currentCell.coordinates.X},{currentCell.coordinates.Z}> - New<{newCell.coordinates.X},{newCell.coordinates.Z}>");
//        }
//        if (newCell2.Position != currentCell.Position)
//        {
//            Debug.LogError($"Fuck Hexmap 2!!! - Orgin<{currentCell.coordinates.X},{currentCell.coordinates.Z}> - New2<{newCell2.coordinates.X},{newCell2.coordinates.Z}>");
//        }
//        ab.SetTarget(currentCell.Position);
//        
//        Debug.Log($"MY BY MYSELF - Dest<{currentCell.coordinates.X},{currentCell.coordinates.Z}> - Dest Pos<{ab.TargetPosition.x},{ab.TargetPosition.z}>");
//        ab.StateMachine.TriggerTransition(FSMStateActor.StateEnum.WALK); 
//    }
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
        if (_pickInfoMaster.CurrentActor)
        {
            hexmapHelper.EnableFollowCamera(_pickInfoMaster.CurrentActor, bFollow);
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

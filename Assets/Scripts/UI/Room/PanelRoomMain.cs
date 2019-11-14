using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Animation;
using GameUtils;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

public class PanelRoomMain : MonoBehaviour
{
    [SerializeField] private HexmapHelper hexmapHelper;
    
    [SerializeField] private Toggle _togShowGrid;
    [SerializeField] private Toggle _togShowLabel;
    [SerializeField] private Toggle _togAi;
    [SerializeField] private Toggle _togFollowCamera;
    [SerializeField] private Toggle _togShowRes;
    [SerializeField] private GameObject _btnCreateActor;
    [SerializeField] private GameObject _btnRangeTest;
    [SerializeField] private GameObject _btnReturnCity;
    [SerializeField] private Text _txtPlayerName;
    [SerializeField] private Text _txtWood;
    [SerializeField] private Text _txtFood;
    [SerializeField] private Text _txtIron;
    
    
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
    
    #region 初始化

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

        _btnCreateActor.transform.Find("Select").gameObject.SetActive(false);
        _btnRangeTest.transform.Find("Select").gameObject.SetActive(false);
        AddListener();
    }

    void OnDestroy()
    {
        RemoveListener();
    }

    void AddListener()
    {
        MsgDispatcher.RegisterMsg((int)ROOM_REPLY.UpdateResReply, OnUpdateResReply);
        
    }

    void RemoveListener()
    {
        MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.UpdateResReply, OnUpdateResReply);
        
    }
    
    #endregion

    #region 鼠标操作

    private bool isMouseDown = false;
    private Vector3 vecLastMousePosition = Vector3.zero;
    
    // Update is called once per frame
    void Update()
    {
        // 本函数用来判定，是否点击到了界面，只有没有点击界面，才处理战场内的事件
        // 下面的这两种做法在手机上均无效,只有使用IsPointerOverUIObject()这个自己写的代码才能生效,原因不明.
        //    if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
        //        return;
        //    if (EventSystem.current.IsPointerOverGameObject())
        //        return;
        
        // 因为检测的是Up消息,所以延迟一帧
//        if (vecLastMousePosition == Vector3.zero)
//            vecLastMousePosition = Input.mousePosition;
        bool over = IsPointerOverUIObject(vecLastMousePosition);
        if (over)
        {
            vecLastMousePosition = Input.mousePosition;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isMouseDown = true;
        }

        vecLastMousePosition = Input.mousePosition;

        if (Input.GetMouseButtonUp(0) && isMouseDown)
        {
            isMouseDown = false;
            vecLastMousePosition = Vector3.zero;
            
            if (_isCreatingActor)
            {
                AskCreateUnit(GetCellUnderCursor(), 10010);
                _isCreatingActor = false;
                CursorManager.Instance.RestoreCursor();
                _btnCreateActor.transform.Find("Select").gameObject.SetActive(false);
            }
            else if (_isRangeTesting)
            {
                RangeTest();
                _isRangeTesting = false;
                CursorManager.Instance.RestoreCursor();
                _btnRangeTest.transform.Find("Select").gameObject.SetActive(false);
            }
            else
            {
                DoSelection();
            }
        }

        if (CommandManager.Instance.IsCommandRunning())
        {
            DoPathfinding();
        }
    }

    //    ————————————————
    //    版权声明：本文为CSDN博主「SunnyIncsdn」的原创文章，遵循 CC 4.0 BY-SA 版权协议，转载请附上原文出处链接及本声明。
    //    原文链接：https://blog.csdn.net/SunnyInCSDN/article/details/72470247
    private bool IsPointerOverUIObject(Vector3 mousePosition) {//判断是否点击的是UI，有效应对安卓没有反应的情况，true为UI
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(mousePosition.x, mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }


    HexCell GetCellUnderCursor () {
        return
            hexmapHelper.hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
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
            ShowHitGround(Input.mousePosition);
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
            Debug.Log($"Selector : <{cell.coordinates.X},{cell.coordinates.Z}>");
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
                    _pickInfoTarget.CurrentActor = av;
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
            Debug.Log($"Selector Targt : <{cell.coordinates.X},{cell.coordinates.Z}>");
        }
    }

    public void RemoveSelection(long actorId)
    {
        // 这里有点复杂哈,ActorVisualizer是挂接在HexUnit上的,而ActorBehaviour是被ActorManager管理的
        // 所以,如果只知道ActorId的话,只能先找到ActorBehaviour,然后通过HexUnit找到ActorVisualizer
        var av = GameRoomManager.Instance.GetActorVisualizer(actorId);
        if (av != null)
        {
            if (IsShowingSelector(av))
            {
                SetSelection(null);
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
        return AskCreateUnit(cellCenter, actorInfoId);
    }

    /// <summary>
    /// 请求创建一只部队
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="actorInfoId"></param>
    /// <returns></returns>
    public bool AskCreateUnit(HexCell cell, int actorInfoId)
    {
        if (cell.Unit != null)
        {
            string msg = $"当前位置有一支部队，请把该部队移走，然后再生产部队！";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Warning);
            return false;
        }
        
        var actorInfoTable = CsvDataManager.Instance.GetTable("actor_info");
        string artPrefab = actorInfoTable.GetValue(10001, "ArtPrefab");
        
        ActorAdd output = new ActorAdd()
        {
            RoomId = GameRoomManager.Instance.RoomId,
            OwnerId = GameRoomManager.Instance.CurrentPlayer.TokenId,
            ActorId = GameUtils.Utils.GuidToLongId(),
            PosX = cell.coordinates.X,
            PosZ = cell.coordinates.Z,
            Orientation = Random.Range(0f, 360f),
            Species = artPrefab, // 预制件的名字
            CellIndex = cell.Index,
            ActorInfoId = actorInfoId,
        };
        GameRoomManager.Instance.SendMsg(ROOM.ActorAdd, output.ToByteArray());
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
                ActorRemove output = new ActorRemove()
                {
                    RoomId = GameRoomManager.Instance.RoomId,
                    OwnerId = GameRoomManager.Instance.CurrentPlayer.TokenId,
                    ActorId = av.ActorId,
                };
                GameRoomManager.Instance.SendMsg(ROOM.ActorRemove, output.ToByteArray());
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

    private bool _isCreatingActor = false; 
    public void OnClickCreateActor()
    {
        SetSelection(null);
        _isCreatingActor = true;
        CursorManager.Instance.ShowCursor(CursorManager.CURSOR_TYPE.CRAETE_ACTOR);
        _btnCreateActor.transform.Find("Select").gameObject.SetActive(true);
    }

    private bool _isRangeTesting = false;
    public void OnClickRangeTest()
    {
        SetSelection(null);
        _isRangeTesting = true;
        CursorManager.Instance.ShowCursor(CursorManager.CURSOR_TYPE.CRAETE_ACTOR);
        _btnRangeTest.transform.Find("Select").gameObject.SetActive(true);
    }

    public void OnClickReturnCity()
    {
        Vector3 posCity = Vector3.zero;
        if (GameRoomManager.Instance.RoomLogic.UrbanManager.Cities.Count > 0)
        {
            UrbanCity city = null;
            foreach (var keyValue in GameRoomManager.Instance.RoomLogic.UrbanManager.Cities)
            {
                UrbanCity urban = keyValue.Value;
                if (urban != null)
                {
                    if (urban.OwnerId == GameRoomManager.Instance.CurrentPlayer.TokenId && urban.IsCapital)
                    {
                        city = urban;
                    }
                }
                
            }
            if (city != null)
            {
                var cell = GameRoomManager.Instance.HexmapHelper.GetCell(city.CellIndex);
                if (cell != null)
                {
                    posCity = cell.Position;
                }
            }
        }

        if (posCity != Vector3.zero)
        {
            StartCoroutine(ReturningCity(posCity));  
        }
    }

    IEnumerator ReturningCity(Vector3 posCity)
    {
        float dist = 99999;
        float speed = 10f;
        while (dist > 1f)
        {
            var posCamera = GameRoomManager.Instance.HexmapHelper.GetCameraPosition();
            var posNew = Vector3.Lerp(posCamera, posCity, speed * Time.deltaTime);
            GameRoomManager.Instance.HexmapHelper.SetCameraPosition(posNew);
            dist = Vector2.Distance(new Vector2(posNew.x, posNew.z), new Vector2(posCity.x, posCity.z));
            yield return null;
        }
    }

    private void RangeTest()
    {
        var current = GetCellUnderCursor();
        List<HexCell> findCells = hexmapHelper.GetCellsInRange(current, 5);
        int index = 0;
        foreach (var cell in findCells)
        {
            cell.SetLabel($"<color=#FF0000FF>{index.ToString()}</color>");
            index++;
        }
    }

    #endregion
    
    #region 消息处理
    
    private void OnUpdateResReply(byte[] bytes)
    {
        UpdateResReply input = UpdateResReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
            return;
        _txtPlayerName.text = GameRoomManager.Instance.CurrentPlayer.Account;
        _txtWood.text = input.Wood.ToString();
        _txtFood.text = input.Food.ToString();
        _txtIron.text = input.Iron.ToString();
    }

    
    #endregion
}

using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Animation;
using UnityEngine;
using UnityEngine.UI;
using GameUtils;

public class PanelCommands : MonoBehaviour
{
    [SerializeField] private CommandItem _cmdItemTemplate;
    [SerializeField] private Transform _container;
    [SerializeField] private Toggle _toggleRoot;
    [SerializeField] private Text _toggleText;

    public Transform Container => _container;
    
    private bool _isExpand;
    // Start is called before the first frame update
    void Start()
    {
        _isExpand = false;
    }

    // Update is called once per frame
    private const float TIME_DELAY = 0.03f; // 刷新间隔0.3秒(每秒30帧)  
    private float timeDelay = 0;
    void Update()
    {
        if (timeDelay < TIME_DELAY)
        {
            timeDelay += Time.deltaTime;
        }

        timeDelay = 0;
        
        TickFunc();

//        for(int i = 0; i < _container.childCount; ++i)
//        {
//            var child = _container.GetChild(i);
//            var ci = child.GetComponent<CommandItem>();
//            if (ci != null)
//            {
//                var cmdId = ci.CmdId;
//                if (CommandManager.Instance.Commands.ContainsKey(cmdId))
//                {
//                    CommandManager.Instance.Commands[cmdId].Func.Tick();
//                }
//            }
//        }
    }

    public void SetSelector(PickInfo pickInfo)
    {
        ClearCommands();

        GameRoomManager.Instance.CommandManager.CurrentExecuter = pickInfo;

        //从“command_set”表格中读取对应于该单位的指令菜单集
        CsvStreamReader CommandSet = CsvDataManager.Instance.GetTable("command_set");
        if (CommandSet == null)
            return;
        string strCmdSet = "";
        string toggleRootName = "";
        if (pickInfo.CurrentCity != null)
        {
            if (pickInfo.CurrentCity.OwnerId == GameRoomManager.Instance.CurrentPlayer.TokenId) // 只有是自己的城市,才会出现[命令菜单]
            {
                strCmdSet = CommandSet.GetValue(2001, "CommandSet");
            }
            toggleRootName = "城市";
        }
        else if (pickInfo.CurrentActor != null)
        {
            var av = pickInfo.CurrentActor;
            if (av.OwnerId == GameRoomManager.Instance.CurrentPlayer.TokenId) // 只有是自己的部队,才会出现[命令菜单]
            {
                strCmdSet = CommandSet.GetValue(av.ActorInfoId, "CommandSet");
            }
            CsvStreamReader csv = CsvDataManager.Instance.GetTable("actor_info");
            toggleRootName = csv.GetValue(av.ActorInfoId, "Name");
        }
        else if (pickInfo.CurrentCell != null)
        {
            HexResource res = pickInfo.CurrentCell.Res;
            if(res.GetAmount(res.ResType)>0)
            {
                string[] resNames = {"木材","粮食","铁矿" };
                toggleRootName = $"{resNames[(int) res.ResType]}:{res.GetLevel(res.ResType)}";
            }
            else if (pickInfo.CurrentCell.IsUnderwater)
            {
                toggleRootName = "水";
            }
            else
            {// Sand-0; Grass-1; Mud-2; Stone-3; Snow-4
                string[] terrainNames = {"沙漠", "草原", "沃土", "山区", "雪地"};
                toggleRootName = terrainNames[(int)pickInfo.CurrentCell.TerrainTypeIndex];
            }
        }
        int countCmd = LoadCommandMenu(strCmdSet);
        //gameObject.SetActive(countCmd > 0);
        _toggleText.text = toggleRootName;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 根据指令菜单集加载菜单UI
    /// </summary>
    /// <param name="strCmdSet"></param>
    private int LoadCommandMenu(string strCmdSet)
    {
        if (string.IsNullOrEmpty(strCmdSet))
        {
            return 0;
        }
        string[] strCmds = strCmdSet.Split('|');
        List<CommandManager.CommandInfo> cmds = new List<CommandManager.CommandInfo>();
        for (int i = 0; i < strCmds.Length; ++i)
        {
            string strCmd = strCmds[i];
            if (!string.IsNullOrEmpty(strCmd))
            {
                int iCmd = int.Parse(strCmd);
                if (GameRoomManager.Instance.CommandManager.Commands.ContainsKey((CommandManager.CommandID) iCmd))
                {
                    cmds.Add(GameRoomManager.Instance.CommandManager.Commands[(CommandManager.CommandID) iCmd]);
                }
                else
                {
                    Debug.LogError($"PanelCommands LoadCommandMenu Error - Command Id not found!!! - CmdId:{iCmd}");
                }
            }
        }
        
        // sort commands
        cmds.Sort((a,b)=>b.Order - a.Order);
        
        foreach (var cmd in cmds)
        {
            AddCommand(cmd);    
        }
        
        return strCmds.Length;
    }

    private CommandItem AddCommand(CommandManager.CommandInfo cmd)
    {
        CommandItem ci = Instantiate(_cmdItemTemplate, _container);
        if (ci)
        {
            ci.name = $"{cmd.Order}_{cmd.Name}";
            ci.Init(cmd.CmdId, cmd.Name, cmd.Func, ci.gameObject);
            return ci;
        }
        else
        {
            Debug.LogError("PanelCommands AddCommand Error - Cannot find Command Item Template Object!!!");
        }
        return null;
    }
    
    public void ClearCommands()
    {
        for (int i = 0; i < _container.childCount; ++i)
        {
            Destroy(_container.GetChild(i).gameObject);
        }
    }
    

    public void OnToggleRoot()
    {
        _isExpand = _toggleRoot.isOn;
        _container.gameObject.SetActive(_isExpand);
    }
    
    public static void TickFunc()
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
                if (!ci) continue;
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

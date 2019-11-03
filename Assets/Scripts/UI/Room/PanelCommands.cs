using System.Collections;
using System.Collections.Generic;
using Animation;
using Ionic.Zlib;
using UnityEngine;
using UnityEngine.UI;
using GameUtils;

public class PanelCommands : MonoBehaviour
{
    [SerializeField] private CommandItem _cmdItemTemplate;
    [SerializeField] private Transform _container;
    [SerializeField] private Toggle _toggleRoot;
    [SerializeField] private Text _toggleText;
    
    private bool _isExpand;
    // Start is called before the first frame update
    void Start()
    {
        _isExpand = false;
    }

    // Update is called once per frame
    void Update()
    {
        
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
            strCmdSet = CommandSet.GetValue(2001, "CommandSet");
            toggleRootName = "城市";
        }
        else if (pickInfo.CurrentUnit != null)
        {
            var av = pickInfo.CurrentUnit.GetComponent<ActorVisualizer>();
            if (av)
            {
                strCmdSet = CommandSet.GetValue(av.ActorInfoId, "CommandSet");
                CsvStreamReader csv = CsvDataManager.Instance.GetTable("actor_info");
                toggleRootName = csv.GetValue(av.ActorInfoId, "Name");
            }
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
        for (int i = 0; i < strCmds.Length; ++i)
        {
            string strCmd = strCmds[i];
            AddCommand(strCmd);
        }

        return strCmds.Length;
    }

    private CommandItem AddCommand(string strCmd)
    {
        if (string.IsNullOrEmpty(strCmd))
            return null;
        int iCmd = int.Parse(strCmd);
        CommandItem ci = Instantiate(_cmdItemTemplate, _container);
        if (ci)
        {
            if (GameRoomManager.Instance.CommandManager.Commands.ContainsKey((CommandManager.CommandID)iCmd))
            {
                CommandManager.CommandInfo cmd = GameRoomManager.Instance.CommandManager.Commands[(CommandManager.CommandID)iCmd];
                ci.name = $"{cmd.Order}_{cmd.Name}";
                ci.Init(cmd.CmdId, cmd.Name, cmd.Func);
                return ci;
            }
            else
            {
                Debug.LogError($"PanelCommands AddCommand Error - Command Id not found!!! - CmdId:{iCmd}");                
            }
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
}

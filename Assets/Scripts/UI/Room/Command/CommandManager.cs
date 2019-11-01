using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandManager : MonoBehaviour
{
    private static CommandManager _instance;
    public static CommandManager Instance => _instance;

    private CommandID _runningCommandId;
    public CommandID RunningCommandId => _runningCommandId;
    private ICommand _runningCommand;
    
    private const int COMMAND_ID_NONE = 0;
    public HexUnit CurrentExecuter; // 目前仅支持单选

    private bool _monitingSelection; // 是否允许切换被选中的物体(如果在命令执行中,再次点击,选中的是宾语,而不是主语)
    
    
    public enum CommandID
    {
        BuildCity = 1001, // 建造城市
        AbandonCity = 1002, // 废弃城市
        AbandonBuilding = 1003, // 废弃建筑
        
        CreateFarmer = 2001, // 生产农民
        CreateSettler = 2002, // 生产开拓者
        CreateSoldier1 = 2003, // 生产刀兵
        CreateSoldier2 = 2004, // 生产长枪兵
        CreateSoldier3 = 2005, // 生产弓箭手
        CreateSoldier4 = 2006, // 生产骑兵
        
        Lumberjack = 3001, // 伐木
        Harvest = 3002, // 收割
        Mining = 3003, // 采矿
        BuildRoad = 3004, // 修路
        BuildBridge = 3005, // 搭桥
        DismissTroop = 3010, // 解散
        
        March = 5001, // 行军
        Attack = 5002, // 进攻
        Guard = 5003, // 驻守
        RapidMarch = 5004, // 急行军
        Charge = 5005, // 冲锋
        
    }
    
    public class CommandInfo
    {
        public string Name;
        public string Icon;
        public int Order;
        public ICommand Func;
    }
    public Dictionary<CommandID, CommandInfo> Commands;

    private void Awake()
    {
        _instance = this;
        _monitingSelection = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        LoadCommands();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void InvokeCmd(CommandID cmdId)
    {
        SetRunningCommand(cmdId);
    }

    public void StopCurrentCommand()
    {
        SetRunningCommand(COMMAND_ID_NONE);
    }
    
    private bool IsValidCommand(CommandID cmdId)
    {
        if (cmdId == COMMAND_ID_NONE)
            return false;
        if (!Commands.ContainsKey(cmdId))
            return false;
        return true;
    }

    void MonitingSelectionChange(bool moniting)
    {
        _monitingSelection = moniting;
    }
    
    void SetRunningCommand(CommandID cmdId)
    {
        //Only one command can exist
        if (_runningCommand != null)
            _runningCommand.Stop();
        _runningCommand = null;
        _runningCommandId = cmdId;
        
        if (IsValidCommand(cmdId))
        {
            // Disable command bar refereshing
            MonitingSelectionChange(false);
            _runningCommand = Commands[cmdId].Func;
        }
        else
        {
            // this command is over
            MonitingSelectionChange(true);
            
        }
    }

    #region  全部命令

    private void LoadCommands()
    {
        // create and attach child object
        GameObject obj = new GameObject("Commands");
        obj.transform.parent = transform;
        obj.transform.localPosition = Vector3.zero;
        Commands = new Dictionary<CommandID, CommandInfo>();
        
        // create all the commands
        CommandInfo ci = null; 
        ci = new CommandInfo(){Name = "建造城市", Func = obj.AddComponent<CmdBuildCity>(),};
        Commands.Add(CommandID.BuildCity, ci);
        ci = new CommandInfo(){Name = "废弃城市", Func = obj.AddComponent<CmdAbandonCity>(),};
        Commands.Add(CommandID.AbandonCity, ci);
        ci = new CommandInfo(){Name = "废弃", Func = obj.AddComponent<CmdAbandonBuilding>(),};
        Commands.Add(CommandID.AbandonBuilding, ci);
        
        ci = new CommandInfo(){Name = "生产农民", Func = obj.AddComponent<CmdCreateFarmer>(),};
        Commands.Add(CommandID.CreateFarmer, ci);
        ci = new CommandInfo(){Name = "生产开拓者", Func = obj.AddComponent<CmdCreateSettler>(),};
        Commands.Add(CommandID.CreateSettler, ci);
        ci = new CommandInfo(){Name = "生产刀兵", Func = obj.AddComponent<CmdCreateSoldier1>(),};
        Commands.Add(CommandID.CreateSoldier1, ci);
        ci = new CommandInfo(){Name = "生产长枪兵", Func = obj.AddComponent<CmdCreateSoldier2>(),};
        Commands.Add(CommandID.CreateSoldier2, ci);
        ci = new CommandInfo(){Name = "生产弓箭手", Func = obj.AddComponent<CmdCreateSoldier3>(),};
        Commands.Add(CommandID.CreateSoldier3, ci);
        ci = new CommandInfo(){Name = "生产骑兵", Func = obj.AddComponent<CmdCreateSoldier4>(),};
        Commands.Add(CommandID.CreateSoldier4, ci);
        
        ci = new CommandInfo(){Name = "伐木", Func = obj.AddComponent<CmdLumberjack>(),};
        Commands.Add(CommandID.Lumberjack, ci);
        ci = new CommandInfo(){Name = "收割", Func = obj.AddComponent<CmdHarvest>(),};
        Commands.Add(CommandID.Harvest, ci);
        ci = new CommandInfo(){Name = "采矿", Func = obj.AddComponent<CmdMining>(),};
        Commands.Add(CommandID.Mining, ci);
        ci = new CommandInfo(){Name = "修路", Func = obj.AddComponent<CmdBuildRoad>(),};
        Commands.Add(CommandID.BuildRoad, ci);
        ci = new CommandInfo(){Name = "搭桥", Func = obj.AddComponent<CmdBuildBridge>(),};
        Commands.Add(CommandID.BuildBridge, ci);
        ci = new CommandInfo(){Name = "解散", Func = obj.AddComponent<CmdDismissTroop>(),};
        Commands.Add(CommandID.DismissTroop, ci);
        
        ci = new CommandInfo(){Name = "行军", Func = obj.AddComponent<CmdMarch>(),};
        Commands.Add(CommandID.March, ci);
        ci = new CommandInfo(){Name = "进攻", Func = obj.AddComponent<CmdAttack>(),};
        Commands.Add(CommandID.Attack, ci);
        ci = new CommandInfo(){Name = "驻守", Func = obj.AddComponent<CmdGuard>(),};
        Commands.Add(CommandID.Guard, ci);
        ci = new CommandInfo(){Name = "急行军", Func = obj.AddComponent<CmdRapidMarch>(),};
        Commands.Add(CommandID.RapidMarch, ci);
        ci = new CommandInfo(){Name = "冲锋", Func = obj.AddComponent<CmdCharge>(),};
        Commands.Add(CommandID.Charge, ci);
    }

    #endregion
}

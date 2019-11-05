using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameUtils;

public class CommandManager : MonoBehaviour
{
    private static CommandManager _instance;
    public static CommandManager Instance => _instance;

    private CommandID _runningCommandId;
    public CommandID RunningCommandId => _runningCommandId;
    private ICommand _runningCommand;
    
    private const int COMMAND_ID_NONE = 0;
    public PickInfo CurrentExecuter; // 目前仅支持单选

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
        Halt = 5006, 停止
        
    }
    
    public class CommandInfo
    {
        public CommandID CmdId;
        public string Name;
        public string Icon;
        public int Order;
        public ICommand Func;
    }
    public Dictionary<CommandID, CommandInfo> Commands;

    private void Awake()
    {
        if (_instance)
        {
            Debug.LogError("CommandManager is singlon, cannot be initialized more than once!");
        }
        _instance = this;
        _monitingSelection = true;
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    // 新增的内容,指令在执行的过程中也可以有函数来处理了 
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

    public bool IsCommandRunning()
    {
        if (_runningCommandId != COMMAND_ID_NONE)
        {
            if (CommandTargetSelected != null) // 这说明该条指令,需要等待目标单位被选中
            {
                return true;
            }
        }
        return false;
    }

    void MonitingSelectionChange(bool moniting)
    {
        _monitingSelection = moniting;
    }
    
    void SetRunningCommand(CommandID cmdId)
    {
        //Only one command can exist
        // 这些代码是参考<二战风云2>的代码而来的,写的不是很好.只能在下个指令开始的时候,结束上一个指令,没有地方提前Stop上一个指令
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
    
    #region 目标被选中

    public event Action<PickInfo> CommandTargetSelected;

    public void OnCommandTargetSelected(PickInfo piTarget)
    {
        CommandTargetSelected?.Invoke(piTarget);
    }
    
    #endregion

    #region  全部命令

    public void LoadCommands()
    {
        // create and attach child object
        GameObject obj = new GameObject("Commands");
        obj.transform.parent = transform;
        obj.transform.localPosition = Vector3.zero;
        Commands = new Dictionary<CommandID, CommandInfo>();
        
        // create all the commands
        CommandInfo ci = null; 
        CsvStreamReader csv = CsvDataManager.Instance.GetTable("command_id");

        CommandID cmdId = CommandID.BuildCity;
        string cmdName = csv.GetValue((int)cmdId, "Name");
        int order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdBuildCity>(),};
        Commands.Add(cmdId, ci);
        
        cmdId = CommandID.AbandonCity;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdAbandonCity>(),};
        Commands.Add(cmdId, ci);
        
        cmdId = CommandID.AbandonBuilding;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdAbandonBuilding>(),};
        Commands.Add(cmdId, ci);
        
        
        cmdId = CommandID.CreateFarmer;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdCreateFarmer>(),};
        Commands.Add(cmdId, ci);
        
        cmdId = CommandID.CreateSettler;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdCreateSettler>(),};
        Commands.Add(cmdId, ci);
        
        cmdId = CommandID.CreateSoldier1;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdCreateSoldier1>(),};
        Commands.Add(cmdId, ci);
        
        cmdId = CommandID.CreateSoldier2;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdCreateSoldier2>(),};
        Commands.Add(cmdId, ci);
        
        cmdId = CommandID.CreateSoldier3;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdCreateSoldier3>(),};
        Commands.Add(cmdId, ci);
        
        cmdId = CommandID.CreateSoldier4;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdCreateSoldier4>(),};
        Commands.Add(cmdId, ci);
        
        
        cmdId = CommandID.Lumberjack;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdLumberjack>(),};
        Commands.Add(cmdId, ci);
        
        cmdId = CommandID.Harvest;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdHarvest>(),};
        Commands.Add(cmdId, ci);
        
        cmdId = CommandID.Mining;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdMining>(),};
        Commands.Add(cmdId, ci);
        
        cmdId = CommandID.BuildRoad;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name =cmdName, Order = order, Func = obj.AddComponent<CmdBuildRoad>(),};
        Commands.Add(cmdId, ci);
        
        cmdId = CommandID.BuildBridge;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdBuildBridge>(),};
        Commands.Add(cmdId, ci);
        
        cmdId = CommandID.DismissTroop;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdDismissTroop>(),};
        Commands.Add(cmdId, ci);
        

        cmdId = CommandID.March;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdMarch>(),};
        Commands.Add(cmdId, ci);
        
        cmdId = CommandID.Attack;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdAttack>(),};
        Commands.Add(cmdId, ci);
        
        cmdId = CommandID.Guard;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdGuard>(),};
        Commands.Add(cmdId, ci);
        
        cmdId = CommandID.RapidMarch;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdRapidMarch>(),};
        Commands.Add(cmdId, ci);
        
        cmdId = CommandID.Charge;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdCharge>(),};
        Commands.Add(cmdId, ci);
        
        cmdId = CommandID.Halt;
        cmdName = csv.GetValue((int)cmdId, "Name");
        order = csv.GetValueInt((int)cmdId, "Order");
        ci = new CommandInfo(){CmdId = cmdId, Name = cmdName, Order = order, Func = obj.AddComponent<CmdHalt>(),};
        Commands.Add(cmdId, ci);
        
    }

    #endregion
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelCommands : MonoBehaviour
{
    [SerializeField] private CommandItem _cmdItemTemplate;
    [SerializeField] private Transform _container;
    [SerializeField] private Toggle _toggleRoot;
    
    public enum CommandType
    {
        CMD_NONE = 0,
        CMD_CREATE_ACTOR = 1,
        CMD_DESTROY_ACTOR = 2,
        CMD_FIND_PATH = 3,
        CMD_BUILD_CITY = 4,
    };

    public const int SELECTOR_TYPE_MAX = 16; 
    public const int SELECTOR_COMMAND_MAX = 8; 
    public enum SelectorType
    {
        NONE = 0,
        CELL = 1,
        CITY = 2,
        RESOURCE_WOOD = 3,
        RESOURCE_FOOD = 4,
        RESOURCE_IRON = 5,
        RESERVED_1 = 6,
        RESERVED_2 = 7,
        TROOP_FARMER = 8,
        TROOP_SETTLER = 9,
        RESERVED_3 = 10,
        TROOP_SOLDIER_1 = 11,
        TROOP_SOLDIER_2 = 12,
        TROOP_SOLDIER_3 = 13,
        TROOP_SOLDIER_4 = 14,
        RESERVED_4 = 15,
    }

    private List<CommandItem> _commandList = new List<CommandItem>();

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

    public void SetSelector(SelectorType selectorType)
    {
        ClearCommands();

        string[,] labels = new string[SELECTOR_TYPE_MAX,SELECTOR_COMMAND_MAX]{
            /*NONE*/{"","","","","","","","",},
            /*CELL*/{"","","","","","","","",}, 
            /*CITY*/{"生产农民", "生产拓荒者","生产刀兵","生产长枪兵","生产弓箭手","生产骑兵","废弃城市","",}, 
            /*RESOURCE_WOOD*/{"","","","","","","","",}, 
            /*RESOURCE_FOOD*/{"","","","","","","","", }, 
            /*RESOURCE_IRON*/{"","","","","","","","", },
            /*RESERVED_1*/{"","","","","","","","",},
            /*RESERVED_2*/{"","","","","","","","",},
            /*TROOP_FARMER*/{"伐木","收割","采矿","修路","架桥","解散","","",},
            /*TROOP_SETTLER*/{"建造城市","解散","","","","","","",},
            /*RESERVED_3*/{"","","","","","","","",},
            /*TROOP_SOLDIER_1*/{"移动","攻击","驻守","解散","","","","",},
            /*TROOP_SOLDIER_2*/{"移动","攻击","驻守","解散","","","","",},
            /*TROOP_SOLDIER_3*/{"移动","攻击","驻守","解散","","","","",},
            /*TROOP_SOLDIER_4*/{"移动","攻击","驻守","解散","","","","",},
            /*RESERVED_4*/{"","","","","","","","",},
        };
        
        CommandItem.ClickCallBack [,] callBacks = new CommandItem.ClickCallBack[,]
        {
            /*NONE*/{null,null,null,null,null,null,null,null,},
            /*CELL*/{null,null,null,null,null,null,null,null,}, 
            /*CITY*/{"生产农民", "生产拓荒者","生产刀兵","生产长枪兵","生产弓箭手","生产骑兵","废弃城市","",}, 
            /*RESOURCE_WOOD*/{null,null,null,null,null,null,null,null,}, 
            /*RESOURCE_FOOD*/{null,null,null,null,null,null,null,null,}, 
            /*RESOURCE_IRON*/{null,null,null,null,null,null,null,null,},
            /*RESERVED_1*/{null,null,null,null,null,null,null,null,},
            /*RESERVED_2*/{null,null,null,null,null,null,null,null,},
            /*TROOP_FARMER*/{"伐木","收割","采矿","修路","架桥","解散","","",},
            /*TROOP_SETTLER*/{"建造城市","解散","","","","","","",},
            /*RESERVED_3*/{null,null,null,null,null,null,null,null,},
            /*TROOP_SOLDIER_1*/{"移动","攻击","驻守","解散","","","","",},
            /*TROOP_SOLDIER_2*/{"移动","攻击","驻守","解散","","","","",},
            /*TROOP_SOLDIER_3*/{"移动","攻击","驻守","解散","","","","",},
            /*TROOP_SOLDIER_4*/{"移动","攻击","驻守","解散","","","","",},
            /*RESERVED_4*/{null,null,null,null,null,null,null,null,},
        };
        
    }

    public void SetCommand(CommandType type)
    {
        switch (type)
        {
            case CommandType.CMD_NONE:
                break;
            case CommandType.CMD_BUILD_CITY:
                break;
            case CommandType.CMD_CREATE_ACTOR:
                break;
            case CommandType.CMD_FIND_PATH:
                break;
        }
    }

    public void ClearCommands()
    {
        foreach (var cmdItem in _commandList)
        {
            Destroy(cmdItem);
        }

        _commandList.Clear();
    }
    
    public CommandItem AddCommand(string label, CommandItem.ClickCallBack OnClick)
    {
        CommandItem cmdItem = Instantiate(_cmdItemTemplate, _container);
        if (cmdItem)
        {
            cmdItem.Init(label, OnClick);
            _commandList.Add(cmdItem);
        }
        else
        {
            Debug.LogError("PanelCommands AddCommand Error - Cannot find Command Item Template Object!!!");
        }

        return cmdItem;
    }

    public void OnToggleRoot()
    {
        _isExpand = _toggleRoot.isOn;
        _container.gameObject.SetActive(_isExpand);
    }
}

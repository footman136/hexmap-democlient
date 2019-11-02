using System;
using UnityEngine;
using UnityEngine.UI;

public class CommandItem : MonoBehaviour
{
    [SerializeField] private CommandManager.CommandID _cmdId; 
    [SerializeField] private Text _label;
    private ICommand _iCommand;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init(CommandManager.CommandID cmdId, string label, ICommand iCommand)
    {
        _cmdId = cmdId;
        _label.text = label;
        _iCommand = iCommand;
        if(!_iCommand.CanRun())
            gameObject.SetActive(false);
    }

    public void Fini()
    {
        _iCommand?.Stop();
    }
    
    public void OnClick()
    {
        CommandManager.Instance.InvokeCmd(_cmdId);
        _iCommand?.Run();
    }
}

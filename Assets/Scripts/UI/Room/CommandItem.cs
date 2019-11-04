using System;
using UnityEngine;
using UnityEngine.UI;

public class CommandItem : MonoBehaviour
{
    [SerializeField] private CommandManager.CommandID _cmdId; 
    [SerializeField] private Text _label;
    [SerializeField] private GameObject _goSelect;
    private ICommand _iCommand;
    
    // Start is called before the first frame update
    void Start()
    {
        _goSelect.SetActive(false);   
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init(CommandManager.CommandID cmdId, string label, ICommand iCommand, GameObject goCmd)
    {
        _cmdId = cmdId;
        _label.text = label;
        _iCommand = iCommand;
        _iCommand.Cmd = goCmd;

        int canRun = _iCommand.CanRun();
        switch (canRun)
        {
            case 0:
                var button = GetComponent<Button>();
                if (button)
                { // Disable本按钮
                    button.enabled = false;
                    button.interactable = false;
                }
                else
                { // 如果不是按钮,无法Disable,则隐藏起来
                    gameObject.SetActive(false);
                }

                break;
            case -1: // 隐藏本按钮
                gameObject.SetActive(false);
                break;
        }
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

    public void Select(bool bSelect)
    {
        if(_goSelect)
            _goSelect.SetActive(bSelect);
    }
}

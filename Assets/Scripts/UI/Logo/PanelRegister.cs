using System.Collections;
using System.Collections.Generic;
using Main;
using UnityEngine;
using UnityEngine.UI;

public class PanelRegister : MonoBehaviour
{
    [SerializeField] private InputField _lbAccount;

    [SerializeField] private InputField _lbPassword1;
    
    [SerializeField] private InputField _lbPassword2;
    private PanelLogin _panelLogin;
    
#region 系统函数
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
#endregion
    
#region 操作
    public void Show(bool show)
    {
        gameObject.SetActive(show);
    }
    
    public void SetLoginPanel(PanelLogin panelLogin)
    {
        _panelLogin = panelLogin;
    }

    public void SetData(string account, string password)
    {
        _lbAccount.text = account;
        _lbPassword1.text = password;
        _lbPassword2.text = password;
    }
#endregion
    
#region 消息处理
    public void OnClickLogin()
    {
        if(_panelLogin!=null)
        {
            string strAccount = _lbAccount.text;
            string strPassword1 = _lbPassword1.text;
            Show(false);
            _panelLogin.Show(true);
            _panelLogin.SetData(strAccount, strPassword1);
        }
    }
    public void OnClickRegister()
    {
        string strAccount = _lbAccount.text;
        string strPassword1 = _lbPassword1.text;
        string strPassword2 = _lbPassword2.text;
        if (strAccount == "")
        {
            UIManager.Instance.SystemTips("账号不能为空", PanelSystemTips.MessageType.Error);
            return;
        }
        if (strPassword1.Length < 6)
        {
            UIManager.Instance.SystemTips("密码至少为6个字符", PanelSystemTips.MessageType.Error);
            return;
        }
        if (strPassword1 != strPassword2)
        {
            UIManager.Instance.SystemTips("两次输入的密码不相同，请再次输入！", PanelSystemTips.MessageType.Important);
            return;
        }
        if(_panelLogin!=null)
        {
            Show(false);
            _panelLogin.SetData(strAccount, strPassword1);
        }
        // 修改状态
        ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.PLAYFAB_REGISTER);
        // 注册PlayFab
        ClientManager.Instance.PlayFab.Register(strAccount, strPassword1);
    }
#endregion
}

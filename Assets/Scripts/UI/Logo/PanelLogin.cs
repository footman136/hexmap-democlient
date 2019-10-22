using System.Collections;
using System.Collections.Generic;
using Main;
using UnityEngine;
using UnityEngine.UI;

public class PanelLogin : MonoBehaviour
{
    [SerializeField] private InputField _lbAccount;

    [SerializeField] private InputField _lbPassword;

    private GameObject _panelRegister;
    
#region 系统函数
    // Start is called before the first frame update
    void Start()
    {
        //Windows10保存在下面类似的地方：
        //计算机\HKEY_CURRENT_USER\Software\Unity\UnityEditor\Wistone\Dino Park
        //计算机\HKEY_USERS\S-1-5-21-4028632277-793711192-2424791590-1001\Software\Unity\UnityEditor\Wistone\Dino Park
        _lbAccount.text = PlayerPrefs.GetString(GameSettings.KEY_ACCOUNT);
        _lbPassword.text = PlayerPrefs.GetString(GameSettings.KEY_PASSWORD);
        Show(true);
    }

    private void OnDestroy()
    {
        if(_panelRegister!=null)
            UIManager.Instance.DestroyPanel(ref _panelRegister);
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

    public void ShowRegister(bool show)
    {
        if (_panelRegister == null)
            return;
        var panelRegister = _panelRegister.GetComponent<PanelRegister>();
        if (panelRegister != null)
        {
            panelRegister.Show(show);
        }
    }

    public void SetData(string account, string password)
    {
        _lbAccount.text = account;
        _lbPassword.text = password;
    }

    public void SaveData()
    {
        //???寫不進去，不知道爲什麽
        PlayerPrefs.SetString(GameSettings.KEY_ACCOUNT, _lbAccount.text);
        PlayerPrefs.SetString(GameSettings.KEY_PASSWORD, _lbPassword.text);
    }
#endregion
    
#region 消息处理
    public void OnClickLogin()
    {
        string strAccount = _lbAccount.text;
        string strPassword = _lbPassword.text;
        if (strAccount == "")
        {
            UIManager.Instance.SystemTips("账号不能为空", PanelSystemTips.MessageType.Error);
            return;
        }

        if (strPassword.Length < 6)
        {
            UIManager.Instance.SystemTips("密码至少为6个字符", PanelSystemTips.MessageType.Error);
            return;
        }

        SaveData();
        Show(false);
        // 修改状态
        ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.PLAYFAB_LOGIN);
        // 登录PlayFab
        ClientManager.Instance.PlayFab.Login(strAccount, strPassword);
    }

    public void OnClickRegister()
    {
        if (_panelRegister == null)
        {
            _panelRegister = UIManager.Instance.CreatePanel(UIManager.Instance.Root, "", "UI/Logo/PanelRegister");
        }

        if (_panelRegister!=null)
        {
            var panelRegister = _panelRegister.GetComponent<PanelRegister>();
            if (panelRegister != null)
            {
                SaveData();
                Show(false);
                string strAccount = _lbAccount.text;
                string strPassword = _lbPassword.text;
                panelRegister.SetData(strAccount, strPassword);
                panelRegister.SetLoginPanel(GetComponent<PanelLogin>());
                panelRegister.Show(true);
            }
        }
    }
#endregion
}

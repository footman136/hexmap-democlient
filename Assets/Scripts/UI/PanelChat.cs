using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class PanelChat : MonoBehaviour
{
    public ClientScript _client;
    public InputField  _inputField; 
    public Text _txtOutput;

    private const int MAX_CHAR_COUNT = 1000;

    private struct NotifyMessage
    {
        public PanelSystemTips.MessageType _type;
        public string _message;
    };
    private List<NotifyMessage> _listNotify; 
        
    // Start is called before the first frame update
    void Start()
    {
        if (_client != null)
        {
            _client.Received += OnReceiveMsg;
            _client.Completed += OnComplete;
        }

        _listNotify = new List<NotifyMessage>();

        StartCoroutine(ProcessNotifyMessage());
    }

    private void OnDestroy()
    {
        if (_client != null)
        {
            _client.Received -= OnReceiveMsg;
            _client.Completed += OnComplete;
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void OnClickSend(GameObject go)
    {
        if (_client == null)
        {
            Debug.Log("ClientScript is not found!!!");
            return;
        }

        if (string.IsNullOrEmpty(_inputField.text))
            return;
        
//        _client.SendMsg(MsgDefine.NORMAL_MESSAGE(_inputField.text));
        _inputField.text = "";
    }

    void OnReceiveMsg(byte[] data)
    {
        string oldText = _txtOutput.text;

        if (_txtOutput.text.Length >= MAX_CHAR_COUNT)
        {
            oldText = oldText.Substring(oldText.Length - MAX_CHAR_COUNT);
        }

        oldText += "\n" + System.Text.Encoding.Default.GetString(data);
        _txtOutput.text = oldText;
    }

    void OnComplete(SocketAction action, string msg)
    {
        switch (action)
        {
            case SocketAction.Connect:
            {
                NotifyMessage nm = new NotifyMessage
                {
                    _type = PanelSystemTips.MessageType.Success,
                    _message = "服务器连接成功！",
                };
                _listNotify.Add(nm);
                // 发送第一条消息，登录该用户
//                _client.SendMsg(MsgDefine.PLAYER_ENTER("footman", 123456));
                Debug.Log(msg);
            }
                break;
            case SocketAction.Send:
                Debug.Log(msg);
                break;
            case SocketAction.Receive:
                Debug.Log(msg);
                break;
            case SocketAction.Close:
            {
                NotifyMessage nm = new NotifyMessage
                {
                    _type = PanelSystemTips.MessageType.Error,
                    _message = "服务器断开连接！",
                };
                _listNotify.Add(nm);
                Debug.Log(msg);
            }
                break;
            case SocketAction.Error:
                Debug.LogError(msg);
                break;
        }
    }
    
    IEnumerator ProcessNotifyMessage()
    {
        while (true)
        {
            if (_listNotify.Count > 0)
            {
                var nm = _listNotify[0];
                UIManager.Instance.SystemTips(nm._message, nm._type);
                _listNotify.RemoveAt(0);
                yield return new WaitForSeconds(3f);
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
        }
    }
}

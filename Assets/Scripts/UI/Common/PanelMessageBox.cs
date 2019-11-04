using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelMessageBox : MonoBehaviour
{
    [SerializeField] private Text _lbTitle;
    [SerializeField] private Text _lbContent;
    [SerializeField] private Button _btn1;
    [SerializeField] private Button _btn2;
    [SerializeField] private Button _btn3;
    [SerializeField] private GameObject _goBg;

    public enum BUTTON
    {
        YES = 1,
        NO = 2,
        OK = 4,
    }

    public delegate void MessageBoxCallBack(int buttonIndex);

    private MessageBoxCallBack _messageBoxCallBack;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClickButton(int buttonIndex)
    {
        gameObject.SetActive(false);
        _messageBoxCallBack?.Invoke(buttonIndex);
    }
    
    /// <summary>
    /// 对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="content">对话框文字内容</param>
    /// <param name="buttonPattern">按钮的种类,常用方法:BUTTON.YES|BUTTON.NO,或者BUTTON.OK</param>
    /// <param name="callBack">对话框按钮被按下后的回调函数,参数是按钮的ID</param>
    /// <param name="showBackground">是否显示背景板</param>
    public void Init(string title, string content, int buttonPattern, MessageBoxCallBack callBack, bool showBackground)
    {
        _lbTitle.text = title;
        _lbContent.text = content;
        
        _btn1.gameObject.SetActive(false);            
        _btn2.gameObject.SetActive(false);            
        _btn3.gameObject.SetActive(false);
        _goBg.SetActive(false);
        if ((buttonPattern & (int) BUTTON.YES) == (int)BUTTON.YES)
        {
            _btn1.gameObject.SetActive(true);            
        }
        if ((buttonPattern & (int) BUTTON.NO) == (int)BUTTON.NO)
        {
            _btn2.gameObject.SetActive(true);            
        }
        if ((buttonPattern & (int) BUTTON.OK) == (int)BUTTON.OK)
        {
            _btn3.gameObject.SetActive(true);            
        }

        if (showBackground)
        {
            _goBg.SetActive(true);
        }
        _messageBoxCallBack = callBack;
    }

}

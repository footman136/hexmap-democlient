using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Transform _root;
    public Transform Root => _root;
    
    private static UIManager _inst;
    public static UIManager Instance => _inst;

    void Awake()
    {
        _inst = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    #region 杂项    
    public bool IsPointerOverGameObject(Vector2 screenPosition)
    {
        //实例化点击事件
        PointerEventData eventDataCurrentPosition = new PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        //将点击位置的屏幕坐标赋值给点击事件
        eventDataCurrentPosition.position = new Vector2(screenPosition.x, screenPosition.y);
 
        List<RaycastResult> results = new List<RaycastResult>();
        //向点击处发射射线
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
 
        return results.Count > 0;
    }
//    ————————————————
//    版权声明：本文为CSDN博主「PassionY」的原创文章，遵循 CC 4.0 BY-SA 版权协议，转载请附上原文出处链接及本声明。
//    原文链接：https://blog.csdn.net/yupu56/article/details/54561553    
    
    #endregion
    
    #region Loading界面
    private GameObject _goLoading;
    public void BeginLoading()
    {
        if (_goLoading == null)
        {
            string uiPrefab = "UI/Common/PanelLoading";
            _goLoading = CreatePanel(_root, "", uiPrefab);
            if (!_goLoading)
            {
                Debug.LogError($"UIManager BeginLoading Error - UI not found:{uiPrefab}");
            }
        }
        else
        {
            ShowPanel(_goLoading, true);
        }
    }

    public void EndLoading(bool destroy = false)
    {
        if (_goLoading != null)
        {
            if (destroy)
            {
                DestroyPanel(ref _goLoading);
            }
            else
            {
                ShowPanel(_goLoading, false);
            }
        }
    }
    #endregion
    
    #region 网络连接界面
    private GameObject _panelConnecting; 
    public void BeginConnecting()
    {
        if (_panelConnecting == null)
        {
            _panelConnecting = CreatePanel(_root, "", "UI/Common/PanelConnecting");
        }
        else
        {
            ShowPanel(_panelConnecting, false);
        }
    }

    public void EndConnecting(bool destroy = false)
    {
        if (_panelConnecting != null)
        {
            if (destroy)
            {
                DestroyPanel(ref _panelConnecting);
            }
            else
            {
                ShowPanel(_panelConnecting, false);
            }
        }
    }
    #endregion
    
    #region SystemTips

    public List<SystemTipsParam> SystemTipsList
    {
        get { return _systemTipsList; }
        set { _systemTipsList = value; }
    }

    public struct SystemTipsParam
    {
        public PanelSystemTips.MessageType _type;
        public string _msg;
        public PanelSystemTips _tips;
    }
    private List<SystemTipsParam> _systemTipsList;
    private bool _systemTipsPlaying = false;
    public void SystemTips(string msg, PanelSystemTips.MessageType msgType)
    {
        if (_systemTipsList == null)
        {
            _systemTipsList = new List<SystemTipsParam>();
        }

        PanelSystemTips systemTips = null;
        { // pool里空了，创建一个新的
            var go = Resources.Load("UI/Common/PanelSystemTips");
            if (go!=null)
            {
                var go2 = Instantiate(go, transform) as GameObject;
                if (go2 != null)
                {
                    systemTips = go2.GetComponent<PanelSystemTips>();
                }
            }
            else
            {
                Debug.LogError("UI/PanelSystemTips not found!");
            }
        }
        
        if (systemTips != null)
        {
            if (_systemTipsPlaying)
            { // 当前动画正在播放，新增的动画就保存起来
                SystemTipsParam stp = new SystemTipsParam()
                {
                    _type = msgType,
                    _msg = msg,
                    _tips = systemTips,
                };
                // 添加到播放链表
                _systemTipsList.Add(stp);
                systemTips.gameObject.SetActive(false);
            }
            else
            { // 否则直接播放
                systemTips.Show(msg, msgType, OnSystemTipsComplete);
                _systemTipsPlaying = true;
            }
        }
    }

    void OnSystemTipsComplete()
    {
        _systemTipsPlaying = false;
        if (_systemTipsList.Count > 0)
        {
            _systemTipsList[0]._tips.gameObject.SetActive(true);
            _systemTipsList[0]._tips.Show(_systemTipsList[0]._msg, _systemTipsList[0]._type, OnSystemTipsComplete);
            _systemTipsList.RemoveAt(0);
        }
    }
    #endregion

    #region CreatePanel
    /// <summary>
    /// 创建一个UI
    /// </summary>
    /// <param name="anchor">锚点，放在哪个节点下</param>
    /// <param name="packageName">资源包名</param>
    /// <param name="prefabName">预制件名</param>
    /// <returns>创建出来的UI</returns>
    public GameObject CreatePanel(Transform anchor, string packageName, string prefabName)
    {
        var go = Resources.Load(prefabName);
        if (go != null)
        {
            var go2 = Instantiate(go, anchor) as GameObject;
            return go2;
        }

        Debug.LogError("UIManager - CreatePanel() Failed - <" + packageName + "> <" + prefabName + ">");
        return null;
    }

    public void ShowPanel(GameObject go, bool bShow)
    {
        go.SetActive(bShow);
    }

    public void DestroyPanel(ref GameObject go)
    {
        if (go != null)
        {
            Destroy(go);
            go = null;
        }
    }
    
    #endregion

    #region MessageBox

    private GameObject _goMessagebox;
    /// <summary>
    /// 对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="content">对话框内容</param>
    /// <param name="buttonPattern">按钮的种类,常用方法:BUTTON.YES|BUTTON.NO,或者BUTTON.OK</param>
    /// <param name="callBack">对话框按钮被按下后的回调函数,参数是按钮的ID</param>
    /// <param name="showBackground">是否显示背景板</param>
    public void MessageBox(string title, string content, int buttonPattern,  
        PanelMessageBox.MessageBoxCallBack callBack, bool showBackground = true)
    {
        if (_goMessagebox == null)
        {
            string uiPrefab = "UI/Common/PanelMessageBox";
            _goMessagebox = CreatePanel(_root, "", uiPrefab);
            var mb = _goMessagebox.GetComponent<PanelMessageBox>();
            if (mb)
            {
                mb.Init(title, content, buttonPattern, callBack, showBackground);
            }
            else
            {
                Debug.LogError($"UIManager MessageBox Error - UI not found:{uiPrefab}");
            }
        }
        else
        {
            ShowPanel(_goMessagebox, true);
        }
    }

    #endregion
}

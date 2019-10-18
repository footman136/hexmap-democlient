using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using Main;
using Protobuf.Lobby;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//头部引入

public class PanelLobbyMain : MonoBehaviour
{
    public static PanelLobbyMain Instance;
    [SerializeField] private GameObject _ScrollView;
    [SerializeField] private VerticalLayoutGroup _ScrollViewContent;
    [SerializeField] private RoomInfoItem _roomInfoItemTemplate;
    [SerializeField] private GameObject _btnJoinRoom;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("PanelLobbyMain must be singleton.");
        }
        Instance = this;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        _ScrollView.SetActive(false);
        _btnJoinRoom.SetActive(false);
        _roomInfoItemTemplate.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #region 事件处理
    public void OnClickReturn()
    {
        ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.START);
        SceneManager.LoadScene("logo");
    }

    public void OnClickCreateRoom()
    {
        _ScrollView.SetActive(false);
        _btnJoinRoom.SetActive(false);
    }

    public void OnClickRoomList()
    {
        _btnJoinRoom.SetActive(false);
        AskRoomList data = new AskRoomList();
        ClientManager.Instance.LobbyManager.SendMsg(LOBBY.AskRoomList, data.ToByteArray());
    }
    
    public void OnClickBackground()
    {
        UIManager.Instance.SystemTips("Background clicked!!!", PanelSystemTips.MessageType.Important);
        Debug.Log("Background clicked!!!");
    }
    
    #endregion
    
    #region 房间列表

    public void ClearRoomList()
    {
        for (int i = 0; i < _ScrollViewContent.transform.childCount; i++) 
        {  
            Destroy (_ScrollViewContent.transform.GetChild (i).gameObject);  
        }  
    }

    public void AddRoomInfo(string name, long roomId, int playerCount, int maxPlayerCount, long createTime)
    {
        RoomInfoItem item = Instantiate(_roomInfoItemTemplate, _ScrollViewContent.transform);
        if (item != null)
        {
            item.gameObject.SetActive(true);
            string count = $"{playerCount}/{maxPlayerCount}";
            item.SetData(name, roomId.ToString(), count, createTime.ToString());
        }
        _ScrollView.SetActive(true);
    }

    public void UnSelectAll()
    {
        for (int i = 0; i < _ScrollViewContent.transform.childCount; i++)
        {
            RoomInfoItem item = _ScrollViewContent.transform.GetChild(i).GetComponent<RoomInfoItem>();
            if (item != null)
            {
                item.Select(false);
            }
        }
    }

    public void ItemSelected(RoomInfoItem item)
    {
        _btnJoinRoom.SetActive(true);
    }
    #endregion
    
}

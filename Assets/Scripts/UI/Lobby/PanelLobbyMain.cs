using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using Main;
using Protobuf.Lobby;
using Protobuf.Room;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RoomInfo = Protobuf.Lobby.RoomInfo;

//头部引入

public class PanelLobbyMain : MonoBehaviour
{
    public static PanelLobbyMain Instance;
    [SerializeField] private GameObject _ScrollView;
    [SerializeField] private VerticalLayoutGroup _ScrollViewContent;
    [SerializeField] private RoomInfoItem _roomInfoItemTemplate;
    [SerializeField] private GameObject _btnJoinRoom;
    [SerializeField] private GameObject _btnDeleteRoom;

    private RoomInfoItem _selectedItem;

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
        _btnDeleteRoom.SetActive(false);
        _roomInfoItemTemplate.gameObject.SetActive(false);
        _selectedItem = null;
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
        AskRoomList output = new AskRoomList();
        ClientManager.Instance.LobbyManager.SendMsg(LOBBY.AskRoomList, output.ToByteArray());
    }

    public void OnClickJoinRoom()
    {
        if (_selectedItem != null)
        {
            long roomId = _selectedItem.RoomId;
            int maxPlayerCount = _selectedItem.MaxPlayerCount;
            AskJoinRoom output = new AskJoinRoom()
            {
                RoomId = roomId,
                MaxPlayerCount = maxPlayerCount,
            };
            ClientManager.Instance.LobbyManager.SendMsg(LOBBY.AskJoinRoom, output.ToByteArray());
        }
    }

    public void OnClickDeleteRoom()
    {
        if (_selectedItem != null)
        {
            DestroyRoom output = new DestroyRoom()
            {
                RoomId = _selectedItem.RoomId,
            };
            ClientManager.Instance.LobbyManager.SendMsg(LOBBY.DestroyRoom,output.ToByteArray());
        }
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

    public void AddRoomInfo(RoomInfo roomInfo)
    {
        RoomInfoItem item = Instantiate(_roomInfoItemTemplate, _ScrollViewContent.transform);
        if (item != null)
        {
            item.gameObject.SetActive(true);
            bool isCreatedByMe = roomInfo.Creator == ClientManager.Instance.Player.TokenId;
            item.SetData(roomInfo.RoomName, roomInfo.RoomId.ToString(), roomInfo.CreateTime, roomInfo.CurPlayerCount, roomInfo.MaxPlayerCount, isCreatedByMe, roomInfo.IsRunning);
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
        _selectedItem = item;
        if (!_selectedItem.IsRunning && _selectedItem.IsCreateByMe)
        {
            _btnDeleteRoom.SetActive(true);
        }
        else
        {
            _btnDeleteRoom.SetActive(false);
        }
    }
    #endregion
    
}

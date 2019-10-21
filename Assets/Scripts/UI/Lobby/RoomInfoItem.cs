using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomInfoItem : MonoBehaviour
{
    [SerializeField] private Text _name;

    [SerializeField] private Text _roomId;

    [SerializeField] private Text _count;

    [SerializeField] private GameObject _selected;
    
    [SerializeField] private Toggle _togRunning;
    [SerializeField] private Toggle _togCreatedByMe;

    public string RoomName => _name.text;
    public long RoomId => long.Parse(_roomId.text);
    public bool IsRunning => _togRunning.isOn;
    public bool IsCreateByMe => _togCreatedByMe.isOn;
    public int CurPlayerCount
    {
        get
        {
            string[] countStrs = _count.text.Split('/');
            return int.Parse(countStrs[0]);
        }
    }
    
    public int MaxPlayerCount
    {
        get
        {
            string[] countStrs = _count.text.Split('/');
            return int.Parse(countStrs[1]);
        }
    }

    
    // Start is called before the first frame update
    void Start()
    {
        _selected.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetData(string name, string roomId, long createTime, int curPlayerCount, int maxPlayerCount, bool isCreatedByMe, bool isRunning)
    {
        _name.text = name;
        _roomId.text = roomId;
        _count.text = $"{curPlayerCount}/{maxPlayerCount}";
        _togRunning.isOn = isRunning;
        _togCreatedByMe.isOn = isCreatedByMe;
    }

    public void Select(bool sel)
    {
        _selected.SetActive(sel);
    }
    
    public void OnClickItem()
    {
        PanelLobbyMain.Instance.UnSelectAll();
        Select(true);
        PanelLobbyMain.Instance.ItemSelected(this);
    }

}

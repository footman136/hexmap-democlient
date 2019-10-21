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

    }

    public void GetData(out string name, out long roomId, out int curCount, out int maxCount)
    {
        name = _name.text;
        roomId = long.Parse(_roomId.text);
        string[] countStrs = _count.text.Split('/');
        curCount = int.Parse(countStrs[0]);
        maxCount = int.Parse(countStrs[1]);
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

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

    public void SetData(string name, string roomId, string count, string createTime)
    {
        _name.text = name;
        _roomId.text = roomId;
        _count.text = count;
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

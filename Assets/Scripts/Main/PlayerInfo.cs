using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    // 当前本玩家的tokenId
    [SerializeField] private long _tokenId;
    public long TokenId
    {
        get { return _tokenId; }
        set { _tokenId = value; }
    }

    // 当前本玩家的账号名（显示用）
    [SerializeField] private string _account;
    public string Account
    {
        get { return _account; }
        set { _account = value; }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

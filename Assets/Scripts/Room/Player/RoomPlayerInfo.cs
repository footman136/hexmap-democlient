using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoomPlayerInfo : MonoBehaviour
{
    [SerializeField] private string _account;
    [SerializeField] private long _tokenId;
    [SerializeField] private int _wood;
    [SerializeField] private int _food;
    [SerializeField] private int _iron;
    [SerializeField] private int _actionPoint;
    [SerializeField] private int _actionPointMax;

    public string Account => _account;
    public long TokenId => _tokenId;

    public int Wood => _wood;
    public int Food => _food;
    public int Iron => _iron;
    public int ActionPoint => _actionPoint;
    public int ActionPointMax => _actionPointMax;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init(string account, long tokenId)
    {
        _account = account;
        _tokenId = tokenId;
    }

    public void SetRes(int wood, int food, int iron)
    {
        _wood = wood;
        _food = food;
        _iron = iron;
    }

    public void SetActionPoint(int actionPoint, int actionPointMax)
    {
        _actionPoint = actionPoint;
        _actionPointMax = actionPointMax;
    }
}

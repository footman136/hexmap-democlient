using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomPlayerInfo : MonoBehaviour
{
    [SerializeField] private int _wood;
    [SerializeField] private int _food;
    [SerializeField] private int _iron;

    public int Wood => _wood;
    public int Food => _food;
    public int Iron => _iron;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Set(int wood, int food, int iron)
    {
        _wood = wood;
        _food = food;
        _iron = iron;
    }
}

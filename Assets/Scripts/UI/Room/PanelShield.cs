using System.Collections;
using System.Collections.Generic;
using Animation;
using UnityEngine;

public class PanelShield : MonoBehaviour
{
    [SerializeField] private Vector3 _pos;
    [SerializeField] private Vector3 _posScreen;

    private ActorVisualizer _actor;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _pos = _actor.CurrentPosition;
        _pos.y += 16;
        _posScreen = Camera.main.WorldToScreenPoint(_pos);
        transform.position = _posScreen;
    }
    public void Init(ActorVisualizer av)
    {
        _actor = av;
    }
}

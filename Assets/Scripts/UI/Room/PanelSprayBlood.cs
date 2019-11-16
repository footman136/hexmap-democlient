using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Animation;
using UnityEngine;
using UnityEngine.UI;

public class PanelSprayBlood : MonoBehaviour
{
    [SerializeField] private Text _blood;
    [SerializeField] private Vector3 _pos;
    [SerializeField] private Vector3 _posScreen;

    private ActorVisualizer _actor;
    
    private const float _SPEED = 1.0f;
    private const float _FLY_TIME = 3.0f; // 第一阶段: 向上飞行的时间
    private const float _FADE_TIME = 2.0f; // 第二阶段: 一边继续向上飞行, 一边开始淡出, 到最后消失
    private const float _FLY_HEIGHT = 100f; // 飞行高度

    private float _startTime;
    private Vector3 _posEnd;
    private Vector3 _posSaved;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        _startTime += Time.deltaTime;
        
        _pos = _actor.CurrentPosition;
        _pos.y += 16;
        _posScreen = Camera.main.WorldToScreenPoint(_pos);
        transform.position = _posScreen;

        // 移动动画
        if (Mathf.Abs(transform.localPosition.y - _posEnd.y) > 0.01f)
        {
            Vector3 posNow = Vector3.Lerp(transform.localPosition, _posEnd, Time.deltaTime * _SPEED);
            transform.localPosition = posNow;
        }
        
        if (_startTime >= _FLY_TIME)
        {
            float alpha = _blood.color.a * (1 - (_startTime - _FLY_TIME) / _FADE_TIME);
            _blood.color = new Color(_blood.color.r, _blood.color.g, _blood.color.b, alpha);
        }
        if (_startTime >= _FLY_TIME + _FADE_TIME)
        { // 把自己送回内存池
            gameObject.Recycle();
            _startTime = 0;
        }
    }

    public void Play(ActorVisualizer av, int blood)
    {
        _actor = av;
        _blood.text = blood.ToString();
        _blood.color = new Color(_blood.color.r, _blood.color.g, _blood.color.b, 1f);
        _startTime = 0;
        
        _pos = _actor.CurrentPosition;
        _pos.y += 16;
        _posScreen = Camera.main.WorldToScreenPoint(_pos);
        transform.position = _posScreen;
        
        _posSaved = transform.localPosition;
        _posEnd = _posSaved + new Vector3(0,_FLY_HEIGHT,0);
    }

}

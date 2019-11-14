using System.Collections;
using System.Collections.Generic;
using Animation;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.UI;

public class PanelSliderBlood : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private Vector3 _pos;
    [SerializeField] private Vector3 _posScreen;

    private ActorVisualizer _actor;

    private const float _MORE_DELAY_TIME = 3f; // 在战斗结束后再延迟多少秒 
    private float _startTime;
    private float _durationTime;
    
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        _startTime += Time.deltaTime;
        SetValue(_actor.Hp);
        if (_startTime > _durationTime)
        {
            gameObject.Recycle();
        }
    }

    private void LateUpdate()
    {
        _pos = _actor.CurrentPosition;
        _pos.y += 16;
        _posScreen = Camera.main.WorldToScreenPoint(_pos);
        transform.position = _posScreen;
    }

    public void Init(ActorVisualizer av)
    {
        _actor = av;
        _slider.minValue = 0;
        _slider.maxValue = av.HpMax;
        _startTime = 0;
        _durationTime = av.AttackDuration + _MORE_DELAY_TIME;
        SetValue(av.Hp);
    }

    public void SetValue(float value)
    {
        _slider.value = value;
    }
}

using System.Collections;
using System.Collections.Generic;
using Animation;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.UI;

public class PanelSliderHarvest : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private Vector3 _pos;
    [SerializeField] private Vector3 _posScreen;

    private ActorVisualizer _actor;

    private float _startTime;
    private float _totalTime;
    
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        _startTime += Time.deltaTime;
        if (_startTime >= _totalTime)
        {
            _startTime = _totalTime;
        }
        
        SetValue(_startTime);
    }

    private void LateUpdate()
    {
        _pos = _actor.CurrentPosition;
        _pos.y += 16;
        _posScreen = HexGameUI.CurrentCamera.WorldToScreenPoint(_pos);
        transform.position = _posScreen;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="av"></param>
    /// <param name="durationTime">剩余的时间</param>
    /// <param name="totalTime">总时间</param>
    public void Init(ActorVisualizer av, float durationTime, float totalTime)
    {
        _actor = av;
        _slider.minValue = 0;
        _slider.maxValue = totalTime;
        _startTime = totalTime - durationTime;
        _totalTime = totalTime;
        SetValue(0);
    }

    public void SetValue(float value)
    {
        _slider.value = value;
    }
}

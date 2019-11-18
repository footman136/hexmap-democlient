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
    private float _durationTime;
    
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        _startTime += Time.deltaTime;
        if (_startTime >= _durationTime)
        {
            _startTime = _durationTime;
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

    public void Init(ActorVisualizer av, float durationTime)
    {
        _actor = av;
        _slider.minValue = 0;
        _slider.maxValue = durationTime;
        _startTime = 0;
        _durationTime = durationTime;
        SetValue(0);
    }

    public void SetValue(float value)
    {
        _slider.value = value;
    }
}

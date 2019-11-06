using System.Collections;
using System.Collections.Generic;
using Animation;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.UI;

public class PanelSliderHarvest : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    private ActorVisualizer _actor;

    private float _durationTime;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    private float timeNow;
    void Update()
    {
        timeNow += Time.deltaTime;
        if (timeNow >= _durationTime)
        {
            timeNow = _durationTime;
        }
        
        SetValue(timeNow);
    }

    void LateUpdate()
    {
        Vector3 pos = _actor.CurrentPosition;
        pos.y += 10;
        Vector3 posNew = Camera.main.WorldToScreenPoint(pos);
        transform.position = posNew;
    }

    public void Init(ActorVisualizer av, float durationTime)
    {
        _slider.minValue = 0;
        _slider.maxValue = durationTime;
        _actor = av;
        _durationTime = durationTime;
        timeNow = 0;
        SetValue(0);
    }

    public void SetValue(float value)
    {
        _slider.value = value;
    }
}

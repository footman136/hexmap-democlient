﻿using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using UnityEngine.UI;
using System;

/// <summary>
/// 系统信息，出现在屏幕上方，出现一段时间后自动消失，新消息会顶掉旧消息
/// </summary>
public class PanelSystemTips : MonoBehaviour
{
    [SerializeField] private Text _lbMsg;
    [SerializeField] private CanvasGroup _group;

    private const float _FLY_HEIGHT = 200f; // 飞行高度
    private const float _SPEED = 1.0f; // 移动速度
    private const float _BG_BORDER = 22f; // 背景比文字高度多多少
    
    public enum MessageType
    {
        None = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Success = 4,
        Important = 5,
    }

    private Vector3 _posSaved;
    
    public bool IsPlaying { get; set; }

    public Action _Completed;

    void Awake()
    {
        _posSaved = transform.localPosition;
        if (_group == null)
            _group = gameObject.GetComponent<CanvasGroup>();
        if (_group == null)
        {
            Debug.LogWarning("PanelSystemTips Awake() Canvas Group not found!!! 这个SystemTips不知道是从哪里初始化来的...");
        }
        
    }
    // Start is called before the first frame update
    void Start()
    {
    }

    /// <summary>
    /// 实在没有一个好用的动画插件，使用Animation也不行（不能重播第二次），使用DoTween也不行（同样不能播放第二次，播完第一次以后旧消失不见了），只能自己写
    /// 将来谁会什么好方法，就可以重写【动画】这部分内容。Sep.4.2019. Liu Gang.
    /// </summary>
    private Vector3 _posStart;
    private Vector3 _posEnd;
    private float _alphaStart;
    private float _alphaEnd;
    private IEnumerator PlayAnimation()
    {
        // 移动动画
        _posStart = _posSaved + new Vector3(0,0,0);
        _posEnd = _posSaved + new Vector3(0,_FLY_HEIGHT,0);
        transform.localPosition = _posStart;
        // 淡入动画
        _alphaStart = 0f;
        _alphaEnd = 1f;
        if(_group != null)
            _group.alpha = _alphaStart;
        yield return new WaitForSeconds(3f);
        // 淡出动画
        _alphaStart = 1f;
        _alphaEnd = 0f;
        if(_group != null)
            _group.alpha = _alphaStart;
        IsPlaying = false;
        _Completed?.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        // 移动动画
        if (Mathf.Abs(transform.localPosition.y - _posEnd.y) > 0.01f)
        {
            Vector3 posNow = Vector3.Lerp(transform.localPosition, _posEnd, Time.deltaTime * _SPEED);
            transform.localPosition = posNow;
        }
        // 淡入淡出动画
        if (_group == null)
            return;
        if (Mathf.Abs(_group.alpha - _alphaEnd) > 0.01f)
        {
            float alphaNow = Mathf.Lerp(_group.alpha, _alphaEnd, Time.deltaTime * _SPEED);
            _group.alpha = alphaNow;
        }
        var back = GetComponent<Image>();
        var txtHeight = _lbMsg.rectTransform.rect.height; // 背景高度比文字高度多22
        float y = txtHeight + _BG_BORDER;
        back.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, y);
    }

    public void Show(string msg, MessageType msgType, Action Completed)
    {
        _Completed = Completed;
        string strColorBeginFormat = "<color={0}>{1}{2}";
        string strColorEnd = "</color>";
        string msgWithColor = msg;
        switch (msgType)
        {
            case MessageType.Info: // 白色
                msgWithColor = string.Format(strColorBeginFormat, "white", msg, strColorEnd);
                break;
            case MessageType.Warning: // 黄色
                msgWithColor = string.Format(strColorBeginFormat, "yellow", msg, strColorEnd);
                break;
            case MessageType.Error: // 红色
                msgWithColor = string.Format(strColorBeginFormat, "red", msg, strColorEnd);
                break;
            case MessageType.Success: // 草绿色
                msgWithColor = string.Format(strColorBeginFormat, "#7FFF00", msg, strColorEnd);
                break;
            case MessageType.Important: // 天蓝色
                msgWithColor = string.Format(strColorBeginFormat, "#00BFFF", msg, strColorEnd);
                break;
        }

        _lbMsg.text = msgWithColor;

        var back = GetComponent<Image>();
        var txtHeight = _lbMsg.rectTransform.rect.height;
        float y = txtHeight + _BG_BORDER;
        back.rectTransform.sizeDelta = new Vector2(back.rectTransform.sizeDelta.x, y);
        
        StopAllCoroutines(); // 新消息顶掉旧消息
        StartCoroutine(PlayAnimation());
        IsPlaying = true;
    }
    
}

using System.Collections;
using System.Collections.Generic;
using Animation;
using UnityEngine;
using static FSMStateActor;

public class ActorAnimatorEvents : MonoBehaviour
{
    private ActorVisualizer _actorVisualizer;
    // Start is called before the first frame update
    void Start()
    {
        _actorVisualizer = GetComponentInParent<ActorVisualizer>();
        if (!_actorVisualizer)
        {
            Debug.LogError("ActorAnimatorEvents Start Error - ActorVisualizer not found!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnAttack()
    {
        Debug.Log("OnAttack ...");
    }

    public void OnHit()
    {
        Debug.Log("OnHit ...");
    }

    /// <summary>
    /// 如果本玩家正处于[战斗]状态, 当攻击动作播放结束, 则停止攻击(进入[空闲]状态), 然后通知服务器进行攻击计算(通知的内容放在状态机里面执行)
    /// </summary>
    public void OnAttackPlayFinished()
    {
        // 典型的从客户端找到服务器端的单位数据
        var ab = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(_actorVisualizer.ActorId);
        if (ab != null)
        {
            if (ab.StateMachine.CurrentAiState == StateEnum.FIGHT || ab.StateMachine.CurrentAiState == StateEnum.DELAYFIGHT)
            {
                ab.StateMachine.TriggerTransition(StateEnum.IDLE);
                Debug.Log("攻击动画播放完毕");
            }
            else
            {
                Debug.Log($"忽略: 攻击动画播放完毕 - {ab.StateMachine.CurrentAiState}");
            }
        }
    }
    
    /// <summary>
    /// 当死亡动作播放结束
    /// </summary>
    public void OnDiePlayFinished()
    {
        Debug.Log("OnDiePlayFinished ...");
        _actorVisualizer.SetAiState(StateEnum.VANISH);
        _actorVisualizer.ShowSliderBlood(false);
    }
}

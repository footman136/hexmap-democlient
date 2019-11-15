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

    public void OnDiePlayFinished()
    {
        Debug.Log("OnDiePlayFinished ...");
        _actorVisualizer.SetAiState(StateEnum.VANISH);
        _actorVisualizer.ShowSliderBlood(false);
    }
}

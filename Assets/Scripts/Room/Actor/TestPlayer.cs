using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AnimalState
{
    public string stateName = "New State";
    public string animationBool = string.Empty;
}

public class TestPlayer : MonoBehaviour
{
    [Header("Animation States"), Space(5)]
    [SerializeField]
    private AnimalState[] dinoStates;

    private Animator animator;
    private const int stateCount = 7; 

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        PlayAnimation(1);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void PlayAnimation(int inState)
    {
        foreach (var state in dinoStates)
        {
            animator.SetBool(state.animationBool, false);
        }
        var stateNames = new string[stateCount] {"Idle", "Walk", "run", "Attack", "Attack", "Die", "DieEnd"};
        var aniStateName = stateNames[inState]; 
        animator.SetBool(dinoStates[inState].animationBool, true);
        Debug.Log($"Animation playering : {aniStateName}");
    }

}

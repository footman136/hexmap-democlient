using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Actor Stats", menuName = "Hexmap/NewActorStats", order = 1)]
public class     ActorStats : ScriptableObject
{
    [SerializeField, Tooltip("角色的种类，名称")]
    public string species = "NA";

    [SerializeField, Tooltip("How many seconds this animal can run for before it gets tired.")]
    public float stamina = 10f;

    [SerializeField, Tooltip("How much this damage this animal does to another animal.")]
    public float power = 10f;

    [SerializeField, Tooltip("How much health this animal has.")]
    public float toughness = 5f;

    [SerializeField, Tooltip("Chance of this animal attacking another animal."), Range(0f, 100f)]
    public float agression = 0f;

    [SerializeField, Tooltip("How quickly the animal does damage to another animal (every 'attackSpeed' seconds will cause 'power' amount of damage).")]
    public float attackSpeed = 0.5f;

    [SerializeField, Tooltip("How far this animal can sense a predator.")]
    public float awareness = 30f;

    [SerializeField, Tooltip("How far this animal can sense it's prey.")]
    public float scent = 30f;

    [SerializeField, Tooltip("恐龙的大小，走到多近距离后就停下")]
    public float contingencyDistance = 1.0f;
        
    [SerializeField, Tooltip("思考间隔，间隔越小，反应越灵敏（单位:秒）")]
    public float thinkingFrequency = 1.0f;
    
    [SerializeField, Tooltip("移动速度")]
    public float moveSpeed = 3.0f;
    
    [SerializeField, Tooltip("奔跑速度")]
    public float runSpeed = 6.0f;

    [SerializeField, Tooltip("转身速度")]
    public float turnSpeed = 120.0f;
    
}

//public class     ActorStats : ScriptableObject
//{
//    [SerializeField, Tooltip("恐龙的种类，名称")]
//    public string species = "NA";
//
//    [SerializeField, Tooltip("How dominent this animal is in the food chain, agressive animals will attack less dominant animals.")]
//    public int dominance = 1;
//
//    [SerializeField, Tooltip("How many seconds this animal can run for before it gets tired.")]
//    public float stamina = 10f;
//
//    [SerializeField, Tooltip("How much this damage this animal does to another animal.")]
//    public float power = 10f;
//
//    [SerializeField, Tooltip("How much health this animal has.")]
//    public float toughness = 5f;
//
//    [SerializeField, Tooltip("Chance of this animal attacking another animal."), Range(0f, 100f)]
//    public float agression = 0f;
//
//    [SerializeField, Tooltip("How quickly the animal does damage to another animal (every 'attackSpeed' seconds will cause 'power' amount of damage).")]
//    public float attackSpeed = 0.5f;
//
//    [SerializeField, Tooltip("If true, this animal will attack other animals of the same specices.")]
//    public bool territorial = false;
//
//    [SerializeField, Tooltip("Stealthy animals can't be detected by other animals.")]
//    public bool stealthy = false;
//
//    [SerializeField, Tooltip("How far this animal can sense a predator.")]
//    public float awareness = 30f;
//
//    [SerializeField, Tooltip("How far this animal can sense it's prey.")]
//    public float scent = 30f;
//
//    [SerializeField, Tooltip("This animal will be peaceful towards species in this list.")]
//    public string[] nonAgressiveTowards;
//
//    [SerializeField, Tooltip("恐龙的大小，走到多近距离后就停下")]
//    public float contingencyDistance = 1.0f;
//        
//    [SerializeField, Tooltip("思考间隔，间隔越小，反应越灵敏（单位:秒）")]
//    public float thinkingFrequency = 1.0f;
//    
//    [SerializeField, Tooltip("移动速度")]
//    public float moveSpeed = 3.0f;
//    
//    [SerializeField, Tooltip("奔跑速度")]
//    public float runSpeed = 6.0f;
//
//    [SerializeField, Tooltip("转身速度")]
//    public float turnSpeed = 120.0f;
//    
//    [SerializeField, Tooltip("追击敌人所坚持的时间（单位：秒）")]
//    public float hardTime = 40.0f;
//    
//    [SerializeField, Tooltip("身上携带的最大食物")]
//    public float foodStorage = 100.0f;
//    
//    [SerializeField, Tooltip("饥饿到什么程度（身上的食物与最大携带量的比例）就要去找吃的了")]
//    public float HungryRate = 0.33f;
//
//    [SerializeField, Tooltip("活着的消耗，每秒消耗多少的食物")]
//    public float liveCost = 1.0f;
//
//    [SerializeField, Tooltip("胃口，每次吃多少食物")]
//    public float appetite = 1.0f;
//
//    [SerializeField, Tooltip("素食者")]
//    public bool vegetarian = true;
//    
//    [SerializeField, Tooltip("食物达到多少比例的情况下，可以下蛋")]
//    public float hatchRate = 0.67f;
//    
//    [SerializeField, Tooltip("距离上次下蛋的间隔时间")]
//    public float hatchDelay = 30f;
//    
//    [SerializeField, Tooltip("下蛋时间，注意不是孵蛋时间")]
//    public float layEggTime = 5f;
//    
//    [SerializeField, Tooltip("长成成人需要的时间")]
//    public float growUpTime = 60f;
//
//    [SerializeField, Tooltip("恐龙寿命")]
//    public int ageMax = 1200;
//
//}

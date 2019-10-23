using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Animation
{
    public class ActorBehaviour : MonoBehaviour
    {
        
        #region 成员
        
        public long ActorId;
        public long OwnerActorId;
        
        [Space(), Header("AI"), Space(5)] 
        [SerializeField]
        private string _species = "NA";
        public string Species => _species;

        [SerializeField, Tooltip("This specific animal stats asset, create a new one from the asset menu under (LowPolyAnimals/NewAnimalStats)")]
        private ActorStats ScriptableActorStats;

        public StateMachineActor StateMachine;
        [SerializeField] private FSMStateActor.StateEnum _currentAiState; // AI的状态
        [SerializeField] private Vector3 _targetPosition;
        [SerializeField] private float _distance;
        
        [Space(), Header("Debug"), Space(5)]
        [SerializeField, Tooltip("If true, AI changes to this animal will be logged in the console.")]
        private bool _logChanges = false;
        
        private Animator animator;
        
        private static Dictionary<long, ActorBehaviour> _allActors = new Dictionary<long, ActorBehaviour>();
        public static Dictionary<long, ActorBehaviour> AllActors => _allActors;

        #endregion
        
        #region 标准函数

        private void Awake()
        {
            animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
            
        }

        // Start is called before the first frame update
        void Start()
        {
            _allActors.Add(ActorId, this);
            _species = ScriptableActorStats.species;
            StartCoroutine(Running());
        }

        private void OnDestroy()
        {
            StopAllCoroutines();        
        }

        // Update is called once per frame
        private float TIME_DELAY = 0.03f;
        private float timeNow = 0;
        void Update()
        {
            _distance = Vector3.Distance(transform.position, _targetPosition);
            timeNow += Time.deltaTime;
            if (timeNow < TIME_DELAY)
            {
                return;
            }

            timeNow = 0;
        
            _currentAiState = StateMachine.CurrentState;
            StateMachine.Tick();
        }

        void OnEnable()
        {
            
        }

        void OnDisable()
        {
            
        }
        
        public void Log(string msg)
        {
            if(_logChanges)
                Debug.Log(msg);
        }
        
        #endregion
        
        #region 播放动画
        
        private void PlayAnimation([NotNull] AnimationState[] animationState)
        {
            if (animationState == null) throw new ArgumentNullException(nameof(animationState));
            int totalState = animationState.Length;
            int randomValue = Random.Range(0, totalState);
            foreach (var state in animationState)
            {
                animator.SetBool(state.animationBool, false);
            }
            animator.SetBool(animationState[randomValue].animationBool, true);
        }
        
        #endregion
        
        #region AI - 第一层
        IEnumerator Running()
        {
            yield return new WaitForSeconds(ScriptableActorStats.thinkingFrequency);
            while (true)
            {
                AI_Running();
            
                yield return new WaitForSeconds(ScriptableActorStats.thinkingFrequency);
            }
        }
        #endregion
        
        #region AI - 第二层

        private bool bFirst = true;
        private float _lastTime = 0f;
        private float _deltaTime;

        private void AI_Running()
        {
            // 这里的_deltaTime是真实的每次本函数调用的时间间隔（而不是Time.deltaTime）。
            //_deltaTime = Time.deltaTime;
            var nowTime = Time.time;
            _deltaTime = nowTime - _lastTime;
            _lastTime = nowTime;

            if (bFirst)
            {
                //newBorn();
                _deltaTime = 0; // 第一次不记录时间延迟
                bFirst = false;
            }
        }

        #endregion
    }

}

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Animation
{
    public class ActorVisualizer : MonoBehaviour
    {
        #region 成员

        public long ActorId;
        public long OwnerActorId;
        
        [Header("Animation States"), Space(5)]
        [SerializeField]
        private IdleState[] idleStates;
        [SerializeField]
        private MovementState[] movementStates;
        [SerializeField]
        private MovementState[] runningStates;
        [SerializeField]
        private AnimationState[] attackingStates;
        [SerializeField]
        private AnimationState[] deathStates;

        [Space(), Header("AI"), Space(5)] 
        [SerializeField]
        private string _species = "NA";
        public string Species => _species;

        [SerializeField, Tooltip("This specific animal stats asset, create a new one from the asset menu under (LowPolyAnimals/NewAnimalStats)")]
        ActorStats ScriptableActorStats;
        [SerializeField] private Vector3 _targetPosition;
        [SerializeField] private float _distance;

        [Space(), Header("Debug"), Space(5)]
        [SerializeField, Tooltip("If true, AI changes to this animal will be logged in the console.")]
        public bool _logChanges = false;
        
        private Animator animator;
        
        private static Dictionary<long, ActorVisualizer> _allActors = new Dictionary<long, ActorVisualizer>();
        public static Dictionary<long, ActorVisualizer> AllActors => _allActors;

        #endregion
        
        #region 标准函数

        void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            animator.applyRootMotion = false;
        }
        // Start is called before the first frame update
        void Start()
        {
            _allActors.Add(ActorId, this);
        }

        void OnEnable()
        {
            
        }

        void OnDisable()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            _distance = Vector3.Distance(transform.position, _targetPosition);
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
        
        private IEnumerator Vanishing()
        {
            while (true)
            {
                Vector3 newpos = transform.position;
                newpos.y -= 0.1f;
                transform.position = newpos;
                if (newpos.y <= -3f)
                {
                    DestroyActor();
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }
    
        public void DestroyActor()
        {
            // 客户端貌似不能发送WorldCommand
            Destroy(gameObject);
        }
        
        #endregion
    }

}

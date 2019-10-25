using System;
using System.Collections;
using System.Collections.Generic;
using GameUtils;
using JetBrains.Annotations;
using Protobuf.Room;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Animation
{
    /// <summary>
    /// 本类是自我管理的，所有有_allActors，不需要ActorManager的存在
    /// </summary>
    public class ActorVisualizer : MonoBehaviour
    {
        #region 成员

        [Header("Basic Attributes"), Space(5)]
        public long RoomId;
        public long OwnerId;
        public long ActorId;
        public int PosX;
        public int PosZ;
        public float Orientation;
        public float Speed;
        public string Species = "N/A";
        
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
        [SerializeField, Tooltip("This specific animal stats asset, create a new one from the asset menu under (LowPolyAnimals/NewAnimalStats)")]
        public Vector3 TargetPosition;
        public Vector3 CurrentPosition;
        public FSMStateActor.StateEnum CurrentAiState; // AI的当前状态
        private HexUnit _hexUnit;
        
        [SerializeField] private float _distance;
        private ActorStats ScriptableActorStats;
        private Coroutine lookAtCoroutine;
        

        [Space(), Header("Debug"), Space(5)]
        [SerializeField, Tooltip("If true, AI changes to this animal will be logged in the console.")]
        public bool _logChanges = false;
        
        private Animator animator;
        
        private static Dictionary<long, ActorVisualizer> _allActors = new Dictionary<long, ActorVisualizer>();
        public static Dictionary<long, ActorVisualizer> AllActors => _allActors;

        #endregion
        
        #region 初始化

        void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            animator.applyRootMotion = false;
        }
        // Start is called before the first frame update
        void Start()
        {
            _allActors.Add(ActorId, this);
            _hexUnit = GetComponent<HexUnit>();
            CurrentPosition = _hexUnit.Location.Position;
            TargetPosition = _hexUnit.Location.Position;
        }

        void OnEnable()
        {
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.TroopAiStateReply, OnAiStateChanged);
        }

        void OnDisable()
        {
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.TroopAiStateReply, OnAiStateChanged);
        }

        // Update is called once per frame
        void Update()
        {
            _distance = Vector3.Distance(CurrentPosition, TargetPosition);
            CurrentPosition = _hexUnit.transform.localPosition;
        }

        public void Log(string msg)
        {
            if(_logChanges)
                Debug.Log(msg);
        }
        
        #endregion
        
        #region 消息处理
        private void OnAiStateChanged(byte[] bytes)
        {
            TroopAiStateReply input = TroopAiStateReply.Parser.ParseFrom(bytes);
            if (!input.Ret)
                return;
            if (input.ActorId != ActorId)
                return; // 不是自己，略过
            HexCell targetCell = GameRoomManager.Instance.HexmapHelper.GetCell(input.PosToX, input.PosToZ);
            Vector3 newPosition = targetCell.Position;
            FSMStateActor.StateEnum newAiState = (FSMStateActor.StateEnum)input.State;
            if (newAiState == CurrentAiState && newPosition == TargetPosition)
            {
                Debug.LogWarning("DinoVisualizer - duplicate AI state : " + newAiState);
                return;
            }
            
            if (lookAtCoroutine != null && newAiState != CurrentAiState)
            { // 状态不同的时候才需要停止携程，防止同一个状态下，动画发生抖动（不停地进行【转向】/【停止转向】）
                StopCoroutine(lookAtCoroutine);
            }
            CurrentAiState = newAiState;
            TargetPosition = newPosition;

            AnimationState[] aniState = null;
            switch (CurrentAiState)
            {
                case FSMStateActor.StateEnum.IDLE:
                    TargetPosition = CurrentPosition;
                    GameRoomManager.Instance.HexmapHelper.Stop(input.ActorId);
                    aniState = idleStates;
                    break;
                case FSMStateActor.StateEnum.WALK:
                    GameRoomManager.Instance.HexmapHelper.DoMove(input.ActorId, input.PosToX, input.PosToZ, input.Speed);
                    Debug.Log($"MSG: TroopAiState - {CurrentAiState} - From<{PosX},{PosZ}> - To<{input.PosToX},{input.PosToZ}>");
                    aniState = movementStates;
                    break;
                case FSMStateActor.StateEnum.FIGHT:
                    aniState = attackingStates;
                    break;
                case FSMStateActor.StateEnum.DIE:
                    aniState = deathStates;
                    break;
                case FSMStateActor.StateEnum.NONE:
                    aniState = idleStates;
                    break;
            }
            PlayAnimation(aniState);

            if (CurrentAiState == FSMStateActor.StateEnum.VANISH)
            {
                StartCoroutine(Vanishing());
            }
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

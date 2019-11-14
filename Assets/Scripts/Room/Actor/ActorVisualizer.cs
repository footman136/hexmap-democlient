using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using GameUtils;
using JetBrains.Annotations;
using Protobuf.Room;
using UnityEngine;
using Random = UnityEngine.Random;
using AI;
using static FSMStateActor;
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
        public string Species = "N/A";
        public int CellIndex; // 根据PosX，PosZ有时候会获取到错误的cell（当PosX,PosZ有一个为负数的时候），所以保存Index是不会出错的
        public int ActorInfoId;

        [Header("Data Attributes"), Space(5)] 
        public string Name;
        public int Hp;
        public int HpMax;
        public float AttackPower;
        public float DefencePower;
        public float Speed;
        public float FieldOfVision;
        public float ShootingRange;
        
        public float AttackDuration; // 攻击持续时间
        public float AttackInterval; // 攻击间隔
        public int AmmoBase; // 弹药基数

        
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
        private AnimationState[] harvestStates;
        [SerializeField]
        private AnimationState[] deathStates;

        [Space(), Header("AI"), Space(5)] 
        [SerializeField, Tooltip("This specific animal stats asset, create a new one from the asset menu under (LowPolyAnimals/NewAnimalStats)")]
        public Vector3 TargetPosition;
        public Vector3 CurrentPosition;
        public StateEnum CurrentAiState; // AI的当前状态
        public HexUnit HexUnit;
        
        [SerializeField] private float _distance;
        private ActorStats ScriptableActorStats;
        private Coroutine lookAtCoroutine;

        [Space(), Header("UI显示"), Space(5)] 
        [SerializeField] private PanelSliderHarvest _sliderHarvest;

        [Space(), Header("Debug"), Space(5)]
        [SerializeField, Tooltip("If true, AI changes to this animal will be logged in the console.")]
        public bool _logChanges = false;
        
        private Animator animator;

        private Transform _inner;
        
        private static Dictionary<long, ActorVisualizer> _allActors = new Dictionary<long, ActorVisualizer>();
        public static Dictionary<long, ActorVisualizer> AllActors => _allActors;

        #endregion
        
        #region 初始化

        void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            animator.applyRootMotion = false;
            _inner = UIManager.Instance.Root.Find("Inner");
        }
        // Start is called before the first frame update
        void Start()
        {
            _allActors.Add(ActorId, this);
            HexUnit = GetComponent<HexUnit>();
            CurrentPosition = HexUnit.Location.Position;
            TargetPosition = HexUnit.Location.Position;
            Orientation = HexUnit.Orientation;
            PlayAnimation(idleStates);
        }

        void OnDestroy()
        {
            _allActors.Remove(ActorId);
            HexUnit = null;
        }

        void OnEnable()
        {
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.TroopAiStateReply, OnAiStateChanged);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.HarvestStartReply, OnHarvestStartReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.HarvestStopReply, OnHarvestStopReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.FightStartReply, OnFightStartReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.FightStopReply, OnFightStopReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.SprayBloodReply, OnSprayBloodReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.UpdateActorInfoReply, OnUpdateActorInfoReply);
        }

        void OnDisable()
        {
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.TroopAiStateReply, OnAiStateChanged);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.HarvestStartReply, OnHarvestStartReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.HarvestStopReply, OnHarvestStopReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.FightStartReply, OnFightStartReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.FightStopReply, OnFightStopReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.SprayBloodReply, OnSprayBloodReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.UpdateActorInfoReply, OnUpdateActorInfoReply);
        }

        // Update is called once per frame
        void Update()
        {
            _distance = Vector3.Distance(CurrentPosition, TargetPosition);
            CurrentPosition = HexUnit.transform.localPosition;
            //CurrentPosition = transform.localPosition;
            PosX = HexUnit.Location.coordinates.X;
            PosZ = HexUnit.Location.coordinates.Z;
            CellIndex = HexUnit.Location.Index;
        }

        public void Log(string msg)
        {
            if(_logChanges)
                Debug.Log(msg);
        }
        
        #endregion
        
        #region 外部接口
        
        public bool IsEnemyInRange(ActorVisualizer avEnemy)
        {
            if (avEnemy == null) return false;
            List<HexCell> cellsInRange = GameRoomManager.Instance.HexmapHelper.GetCellsInRange(HexUnit.Location, (int)ShootingRange);
            if (cellsInRange.Contains(avEnemy.HexUnit.Location))
            {
                return true;
            }

            return false;
        }

        public ActorVisualizer FindEnemyInRange()
        {
            List<HexCell> cellsInRange = GameRoomManager.Instance.HexmapHelper.GetCellsInRange(HexUnit.Location, (int)ShootingRange);
            foreach (HexCell cell in cellsInRange)
            {
                if (cell.Unit != null)
                {
                    var av = cell.Unit.GetComponent<ActorVisualizer>();
                    return av;
                }
            }

            return null;
        }

        #endregion
        #region 播放动画

        private void StopAllAnimations()
        {
            animator.SetBool(idleStates[0].animationBool, false);
            animator.SetBool(movementStates[0].animationBool, false);
            animator.SetBool(attackingStates[0].animationBool, false);
            animator.SetBool(harvestStates[0].animationBool, false);
            animator.SetBool(deathStates[0].animationBool, false);
            animator.SetBool(runningStates[0].animationBool, false);
        }
        
        private void PlayAnimation([NotNull] AnimationState[] animationState)
        {
            if (animationState == null) return;
            StopAllAnimations();
            int totalState = 1;//animationState.Length;
            int randomValue = Random.Range(0, totalState);
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
                    GameRoomManager.Instance.HexmapHelper.DestroyUnit(ActorId);
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
        
        #region 状态改变
        
        private void OnAiStateChanged(byte[] bytes)
        {
            TroopAiStateReply input = TroopAiStateReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过
            if (!input.Ret)
                return;

            HexCell targetCell = GameRoomManager.Instance.HexmapHelper.GetCell(input.CellIndexTo);
            Vector3 newPosition = targetCell.Position;
            ActorVisualizer avTarget = null;
            if (AllActors.ContainsKey(input.TargetId))
            { //如果目标是单位,优先用单位的坐标作为目标点
                avTarget = AllActors[input.TargetId];
                newPosition = avTarget.CurrentPosition;
            }
            
            StateEnum newAiState = (StateEnum)input.State;
            
            if (lookAtCoroutine != null && newAiState != CurrentAiState)
            { // 状态不同的时候才需要停止携程，防止同一个状态下，动画发生抖动（不停地进行【转向】/【停止转向】）
                StopCoroutine(lookAtCoroutine);
            }
            CurrentAiState = newAiState;
            TargetPosition = newPosition;

            AnimationState[] aniState = null;
            switch (CurrentAiState)
            {
                case StateEnum.IDLE:
                    TargetPosition = CurrentPosition;
                    GameRoomManager.Instance.HexmapHelper.Stop(input.ActorId);
                    aniState = idleStates;
                    break;
                case StateEnum.VANISH:
                    break;
                case StateEnum.DIE:
                    aniState = deathStates;
                    break;
                case StateEnum.WALK:
                case StateEnum.WALKFIGHT:
                case StateEnum.WALKGUARD:
                    GameRoomManager.Instance.HexmapHelper.DoMove(input.ActorId, input.CellIndexFrom, input.CellIndexTo);
                    Debug.Log($"MSG: TroopAiState - {CurrentAiState} - From<{input.PosXFrom},{input.PosZFrom}> - To<{input.PosXTo},{input.PosZTo}>");
                    //aniState = movementStates;
                    aniState = runningStates;
                    break;
                case StateEnum.FIGHT:
                    GameRoomManager.Instance.HexmapHelper.Stop(input.ActorId);
                    GameRoomManager.Instance.HexmapHelper.LookAt(input.ActorId, TargetPosition);
                    aniState = attackingStates;
                    break;
                case StateEnum.GUARD:
                    GameRoomManager.Instance.HexmapHelper.Stop(input.ActorId);
                    aniState = idleStates;
                    break;
                case StateEnum.HARVEST:
                    aniState = harvestStates;
                    break;
                case StateEnum.NONE:
                    aniState = idleStates;
                    break;
            }
            PlayAnimation(aniState);

            if (CurrentAiState == StateEnum.VANISH)
            {
                StartCoroutine(Vanishing());
            }
        }
        
        #endregion
        
        #region 采集

        private void OnHarvestStartReply(byte[] bytes)
        {
            HarvestStartReply input = HarvestStartReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过
            if (!input.Ret)
                return;

            // 显示进度条
            _sliderHarvest = GameRoomManager.Instance.FightManager.SliderHarvest.Spawn(_inner, Vector3.zero);
            _sliderHarvest.Init(this, input.DurationTime);
        }
        
        private void OnHarvestStopReply(byte[] bytes)
        {
            HarvestStopReply input = HarvestStopReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过
            if (!input.Ret)
                return;

            HexResource.RESOURCE_TYPE resType = (HexResource.RESOURCE_TYPE) input.ResType;
            HexCell currentCell = HexUnit.Location;
            HexResource res = currentCell.Res;
            int level = res.GetLevel(resType);
            res.SetAmount(resType, input.ResRemain);
            int levelNew = res.GetLevel(resType);
            if (level != levelNew)
            {
                res.Refresh(currentCell);
            }

            // 隐藏进度条
            if (_sliderHarvest)
            {
                _sliderHarvest.Recycle();
                _sliderHarvest = null;
            }

            string [] resTypes = {"木材","粮食","铁矿"};
            string msg = $"获取了 {input.ResHarvest} 的 {resTypes[input.ResType]} 资源";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Success);
            GameRoomManager.Instance.Log("MSG: HarvestStop OK - " + msg + $" - 剩余资源{input.ResRemain}");
        }
        
        #endregion
        
        #region 战斗

        private void OnFightStartReply(byte[] bytes)
        {
            FightStartReply input = FightStartReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过
            if (!input.Ret)
                return;
            
            // 显示自己的血条
            ShowSliderBlood();
            
            // 显示对方的血条
            var avTarget = GameRoomManager.Instance.GetActorVisualizer(input.TargetId);
            if (!avTarget) return;
            avTarget.ShowSliderBlood();
            
            GameRoomManager.Instance.Log("ActorVisualizer OnFightStart ...");
        }

        private void ShowSliderBlood()
        {
            var sliderBlood = GameRoomManager.Instance.FightManager.SliderBlood.Spawn(_inner, Vector3.zero);
            if (!sliderBlood) return;
            sliderBlood.Init(this);
        }

        private void OnFightStopReply(byte[] bytes)
        {
            FightStopReply input = FightStopReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过
            if (!input.Ret)
            {
                GameRoomManager.Instance.Log($"ActorVisualizer OnFightStopReply Error - {input.ErrMsg}");
                return;
            }

            GameRoomManager.Instance.Log("ActorVisualizer OnFightStop ...");
        }

        private void OnSprayBloodReply(byte[] bytes)
        {
            SprayBloodReply input = SprayBloodReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过

            // 每次都创建新的
            var spray = GameRoomManager.Instance.FightManager.SprayBlood.Spawn(_inner, Vector3.zero);
            if (spray == null) return;
            spray.Play(this, input.Damage);

            GameRoomManager.Instance.Log($"Spraying Blood ...");
        }
        
        #endregion
        
        #region 刷新属性

        private void OnUpdateActorInfoReply(byte[] bytes)
        {
            UpdateActorInfoReply input = UpdateActorInfoReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过

            // 客户端
            Hp = input.Hp;
        }
        
        #endregion

    }

}

﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using GameUtils;
using JetBrains.Annotations;
using Protobuf.Room;
using UnityEngine;
using Random = UnityEngine.Random;
using AI;
using UnityEngine.UIElements;
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
        public int AmmoBaseMax; // 最大弹药基数
        
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
        [SerializeField]
        private AnimationState[] vanishStates;

        [Space(), Header("AI"), Space(5)] 
        [SerializeField, Tooltip("This specific animal stats asset, create a new one from the asset menu under (LowPolyAnimals/NewAnimalStats)")]
        public Vector3 TargetPosition;
        public Vector3 CurrentPosition;
        public StateEnum CurrentAiState; // AI的当前状态
        public long TargetActorId; // 目标单位的id, 在监视面板上可以看到
        public int TargetCellIndex;
        public HexUnit HexUnit;
        
        [SerializeField] private float _distance;
        private ActorStats ScriptableActorStats;

        [Space(), Header("UI显示"), Space(5)] 
        [SerializeField] private PanelSliderHarvest _sliderHarvest;
        [SerializeField] private PanelSliderBlood _sliderBlood;
        [SerializeField] private PanelShield _shield;
        
        // 这个 Line Renderer 太丑了, 以后用三国的搞 
        //[SerializeField] private LineRenderer _lineRenderer;

        private List<HexCell> _listPath;
        public List<HexCell> ListPath => _listPath;

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
            if(UIManager.Instance)
                _inner = UIManager.Instance.Root.Find("Inner");
            PrepareAllAnimations();

            // 寻路的路线,但是现在太丑了, 所以先去掉, 以后再说
//            GameObject go = Resources.Load("UI/Room/Arrow") as GameObject;
//            if (go)
//            {
//                GameObject go2 = Instantiate(go, transform); 
//                _lineRenderer = go2.GetComponent<LineRenderer>();
//            }
//
//            if (!_lineRenderer)
//            {
//                Debug.LogError("Arrow not found!");
//            }
//            else
//            {
//                _lineRenderer.gameObject.SetActive(false);
//            }
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
            if (_sliderHarvest != null)
            {
                _sliderHarvest.Recycle();
                _sliderHarvest = null;
            }

            if (_sliderBlood != null)
            {
                _sliderBlood.Recycle();
                _sliderBlood = null;
            }

            if (_shield != null)
            {
                _shield.Recycle();
                _shield = null;
            }
                
        }

        void OnEnable()
        {
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.ActorAiStateReply, OnActorAiStateReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.HarvestStartReply, OnHarvestStartReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.HarvestStopReply, OnHarvestStopReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.FightStartReply, OnFightStartReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.FightStopReply, OnFightStopReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.SprayBloodReply, OnSprayBloodReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.UpdateActorInfoReply, OnUpdateActorInfoReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.ActorPlayAniReply, OnActorPlayAniReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.AmmoSupplyReply, OnAmmoSupplyReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.TryCommandReply, OnTryCommandReply);
        }

        void OnDisable()
        {
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.ActorAiStateReply, OnActorAiStateReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.HarvestStartReply, OnHarvestStartReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.HarvestStopReply, OnHarvestStopReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.FightStartReply, OnFightStartReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.FightStopReply, OnFightStopReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.SprayBloodReply, OnSprayBloodReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.UpdateActorInfoReply, OnUpdateActorInfoReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.ActorPlayAniReply, OnActorPlayAniReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.AmmoSupplyReply, OnAmmoSupplyReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.TryCommandReply, OnTryCommandReply);
        }

        // Update is called once per frame
        void Update()
        {
            CurrentPosition = HexUnit.transform.localPosition; // 不是Cell的坐标, 不见得是格子中心,所以要这样取值
            Vector2 curPos2 = new Vector2(CurrentPosition.x, CurrentPosition.z);
            Vector2 targetPos2 = new Vector2(TargetPosition.x, TargetPosition.z);
            _distance = Vector2.Distance(curPos2, targetPos2);//
            
            CellIndex = HexUnit.Location.Index;
            PosX = HexUnit.Location.coordinates.X;
            PosZ = HexUnit.Location.coordinates.Z;

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

        public bool IsDead => CurrentAiState == StateEnum.DIE || CurrentAiState == StateEnum.VANISH;

        #endregion
        
        #region 播放动画

        private void PrepareAllAnimations()
        {
            const int stateCount = 7; 
            var stateNames = new string[stateCount] {"Idle", "Walk", "run", "Attack", "Harvest", "Die", "DieEnd"};
            var animationBools = new string[stateCount] {"isIdling", "isWalking", "isRunning", "isAttacking", "isHarvesting", "isDying", "isVanishing"};
            idleStates = new IdleState[1];
            idleStates[0] = new IdleState()
            {
                stateName = stateNames[0],
                animationBool = animationBools[0],
            };
            movementStates = new MovementState[1];
            movementStates[0] = new MovementState()
            {
                stateName = stateNames[1],
                animationBool = animationBools[1],
            };
            runningStates = new MovementState[1];
            runningStates[0] = new MovementState()
            {
                stateName = stateNames[2],
                animationBool = animationBools[2],
            };
            attackingStates = new AnimationState[1];
            attackingStates[0] = new AnimationState()
            {
                stateName = stateNames[3],
                animationBool = animationBools[3],
            };
            harvestStates = new AnimationState[1];
            harvestStates[0] = new AnimationState()
            {
                stateName = stateNames[4],
                animationBool = animationBools[4],
            };
            deathStates = new AnimationState[1];
            deathStates[0] = new AnimationState()
            {
                stateName = stateNames[5],
                animationBool = animationBools[5],
            };
            vanishStates = new AnimationState[1];
            vanishStates[0] = new AnimationState()
            {
                stateName = stateNames[6],
                animationBool = animationBools[6],
            };
        }
        private void StopAllAnimations()
        {
            animator.SetBool(idleStates[0].animationBool, false);
            animator.SetBool(movementStates[0].animationBool, false);
            animator.SetBool(runningStates[0].animationBool, false);
            animator.SetBool(attackingStates[0].animationBool, false);
            animator.SetBool(harvestStates[0].animationBool, false);
            animator.SetBool(deathStates[0].animationBool, false);
            animator.SetBool(vanishStates[0].animationBool, false);
        }
        
        private void PlayAnimation(AnimationState[] animationState)
        {
            if (animationState != null)
            {
                StopAllAnimations();
                int totalState = 1; //animationState.Length;
                int randomValue = Random.Range(0, totalState);
                animator.SetBool(animationState[randomValue].animationBool, true);
            }

            // 这是纯客户端行为,所以不要在StateMachine里执行,StateMachine还是服务器的逻辑(只不过现在在客户端里借用)
            if (CurrentAiState == StateEnum.VANISH)
            {
                StartCoroutine(Vanishing());
            }
        }

        /// <summary>
        /// 仅播放动画,但是不修改AI状态机CurrentAiState
        /// </summary>
        /// <param name="aiState"></param>
        public void PlayAnimation(StateEnum aiState)
        {
            switch (aiState)
            {
                case StateEnum.IDLE:
                case StateEnum.DELAYFIGHT:
                case StateEnum.GUARD:
                    PlayAnimation(idleStates);
                    break;
                case StateEnum.WALK:
                case StateEnum.WALKFIGHT:
                    PlayAnimation(runningStates);
                    break;
                case StateEnum.FIGHT:
                    PlayAnimation(attackingStates);
                    break;
                case StateEnum.HARVEST:
                    PlayAnimation(harvestStates);
                    break;
                case StateEnum.DIE:
                    PlayAnimation(deathStates);
                    break;
                case StateEnum.VANISH:
                    PlayAnimation(null); // vanishStates这个状态的动画不知道为什么不正确,这里只能继续沿用Die的最后一帧了
                    break;
            }
        }

        /// <summary>
        /// 修改AI状态机
        /// </summary>
        /// <param name="aiState"></param>
        public void SetAiState(StateEnum aiState)
        {
            CurrentAiState = aiState;
            PlayAnimation(aiState);
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
        
        #endregion
        
        #region 盾牌效果(驻守警戒)
        
        public void ShowShield(bool show = true)
        {
            if (show)
            {
                if (!_shield)
                {
                    _shield = GameRoomManager.Instance.FightManager.Shield.Spawn(_inner, Vector3.zero);
                    _shield.Init(this);
                }
            }
            else
            {
                if (_shield)
                {
                    _shield.Recycle();
                    _shield = null;
                }
            }
        }
        
        #endregion
        
        #region 状态改变
        
        private void OnActorAiStateReply(byte[] bytes)
        {
            ActorAiStateReply input = ActorAiStateReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过
            if (!input.Ret)
                return;

            HexCell fromCell = GameRoomManager.Instance.HexmapHelper.GetCell(input.AiCellIndexFrom);

            Vector3 newPosition = Vector3.zero;
            HexCell targetCell = null;
            ActorVisualizer avTarget = null;
            if (AllActors.ContainsKey(input.AiTargetId))
            { //如果目标是单位,优先用单位的[所在格子的]坐标作为目标点, 注意, 也不是目标点的实际坐标
                avTarget = AllActors[input.AiTargetId];
                targetCell = avTarget.HexUnit.Location;
            }
            else
            {
                targetCell = GameRoomManager.Instance.HexmapHelper.GetCell(input.AiCellIndexTo);
            }
            newPosition = targetCell.Position;

            StateEnum newAiState = (StateEnum)input.AiState;

            // 注: 在客户端: input.DurationTime是没有用的, 时间完全由 服务器端/AI端 控制

            switch (CurrentAiState)
            {
                case StateEnum.GUARD:
                    ShowShield(false);
                    break;
                case StateEnum.HARVEST:
                    ShowSliderHarvest(false);
                    break;
                case StateEnum.FIGHT:
                    break;
            }

            CurrentAiState = newAiState;
            TargetPosition = newPosition;
            TargetActorId = input.AiTargetId;
            TargetCellIndex = input.AiCellIndexTo;

            if (avTarget == null || avTarget.IsDead)
            {
                TargetActorId = 0;
            }

            AnimationState[] aniState = null;
            switch (CurrentAiState)
            {
                case StateEnum.IDLE:
                    TargetPosition = CurrentPosition;
                    GameRoomManager.Instance.HexmapHelper.Stop(input.ActorId);
                    aniState = idleStates;
                    break;
                case StateEnum.VANISH:
                    //aniState = vanishStates; // vanishStates这个状态的动画不知道为什么不正确,这里只能继续沿用Die的最后一帧了
                    break;
                case StateEnum.DIE:
                    GameRoomManager.Instance.HexmapHelper.Stop(input.ActorId);
                    aniState = deathStates;
                    break;
                case StateEnum.WALK:
                case StateEnum.WALKFIGHT:
                    _listPath = GameRoomManager.Instance.HexmapHelper.DoMove(input.ActorId, input.AiCellIndexFrom, input.AiCellIndexTo);
                    Debug.Log($"MSG: ActorAiState - {CurrentAiState} - From<{fromCell.coordinates.X},{fromCell.coordinates.X}> - To<{targetCell.coordinates.X},{targetCell.coordinates.Z}>");
                    //aniState = movementStates;
                    aniState = runningStates;
                    break;
                case StateEnum.FIGHT:
                    GameRoomManager.Instance.HexmapHelper.Stop(input.ActorId);
                    GameRoomManager.Instance.HexmapHelper.LookAt(input.ActorId, TargetPosition);
                    ShowSliderBlood();
                    aniState = attackingStates;
                    break;
                case StateEnum.DELAYFIGHT:
                    ShowSliderBlood();
                    aniState = idleStates; // 先不播放战斗动画,过了AttackInternal的时间以后再通过ActorPlayAni消息来播
                    break;
                case StateEnum.GUARD:
                    ShowShield();
                    GameRoomManager.Instance.HexmapHelper.Stop(input.ActorId);
                    aniState = idleStates;
                    break;
                case StateEnum.HARVEST:
                    ShowSliderHarvest(true, input.AiDurationTime, input.AiTotalTime);
                    aniState = harvestStates;
                    break;
                case StateEnum.NONE:
                    aniState = idleStates;
                    break;
            }
            PlayAnimation(aniState);
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

            string [] resTypes = {"木材","粮食","铁矿"};
            string msg = $"获取了 {input.ResHarvest} 的 {resTypes[input.ResType]} 资源";
            UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Success);
            GameRoomManager.Instance.Log("MSG: HarvestStop OK - " + msg + $" - 剩余资源{input.ResRemain}");
            
            // 必要的时候, 刷新地面上的资源数字
            PanelRoomMain.Instance.UpdateResInCell(currentCell.Index);
        }

        private void ShowSliderHarvest(bool show, float durationTime = 0, float totalTime = 0)
        {
            if (show)
            {
                // 显示进度条
                _sliderHarvest = GameRoomManager.Instance.FightManager.SliderHarvest.Spawn(_inner, Vector3.zero);
                _sliderHarvest.Init(this, durationTime, totalTime);
            }
            else
            {
                // 隐藏进度条
                if (_sliderHarvest)
                {
                    _sliderHarvest.Recycle();
                    _sliderHarvest = null;
                }
            }
        }
        
        #endregion
        
        #region 战斗

        private void OnFightStartReply(byte[] bytes)
        {
            FightStartReply input = FightStartReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过
            if (!input.Ret)
            {
                PanelSprayMessage spray = GameRoomManager.Instance.FightManager.SprayMessage.Spawn(_inner, Vector3.zero);
                if (spray == null) return;
                spray.Play(this, input.ErrMsg, PanelSystemTips.MessageType.Error);
                GameRoomManager.Instance.Log($"ActorVisualizer OnFightStartReply Error - {input.ErrMsg}");
                return;
            }

            // 显示自己的血条
            ShowSliderBlood();
            GameRoomManager.Instance.Log("ActorVisualizer OnFightStartReply OK ...");
            
            // 显示对方的血条
            var avTarget = GameRoomManager.Instance.GetActorVisualizer(input.TargetId);
            if (!avTarget) return;
            if (avTarget.IsDead) return;
            avTarget.ShowSliderBlood();
        }

        public void ShowSliderBlood(bool show = true)
        {
            if (show)
            {
                if (!_sliderBlood || !_sliderBlood.gameObject.activeSelf)
                {
                    _sliderBlood = GameRoomManager.Instance.FightManager.SliderBlood.Spawn(_inner, Vector3.zero);
                }

                if (!_sliderBlood) return;
                _sliderBlood.Init(this);
            }
            else
            {
                if(_sliderBlood)
                    _sliderBlood.Recycle();
            }
        }

        private void OnFightStopReply(byte[] bytes)
        {
            FightStopReply input = FightStopReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过
            if (!input.Ret)
            {   GameRoomManager.Instance.Log($"ActorVisualizer OnFightStopReply Error - {input.ErrMsg}");
                return;
            }

            if (input.FightAgain)
            {
                // 显示自己的血条
                ShowSliderBlood();
            
                // 显示对方的血条
                var avTarget = GameRoomManager.Instance.GetActorVisualizer(input.TargetId);
                if (!avTarget) return;
                avTarget.ShowSliderBlood();
            }

            // 如果弹药没了, 提示"弹药已经用完", 提示玩家下次无法攻击了
            if (AmmoBase <= 0)
            {
                string msg = "弹药已经用完!";
                PanelSprayMessage spray = GameRoomManager.Instance.FightManager.SprayMessage.Spawn(_inner, Vector3.zero);
                if (spray == null) return;
                spray.Play(this, msg, PanelSystemTips.MessageType.Error);
            }

            GameRoomManager.Instance.Log("ActorVisualizer OnFightStopReply OK ...");
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

            GameRoomManager.Instance.Log($"Spraying Blood ... {ActorId}");
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
            AmmoBase = input.AmmoBase;
        }

        private void OnActorPlayAniReply(byte[] bytes)
        {
            ActorPlayAniReply input = ActorPlayAniReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过
            
            PlayAnimation((StateEnum)input.AiState);            
        }
        
        private void OnAmmoSupplyReply(byte[] bytes)
        {
            AmmoSupplyReply input = AmmoSupplyReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过

            AmmoBase = input.AmmoBase;
            string msg = $"恢复弹药:{AmmoBase}/{AmmoBaseMax}";
            
            // 每次都创建新的
            var spray = GameRoomManager.Instance.FightManager.SprayMessage.Spawn(_inner, Vector3.zero);
            if (spray == null) return;
            spray.Play(this, msg, PanelSystemTips.MessageType.Success);
        }

        #endregion
        
        #region 显示信息
        
        private void OnTryCommandReply(byte[] bytes)
        {
            TryCommandReply input = TryCommandReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过
            if (!input.Ret)
            {
                // 每次都创建新的
                PanelSprayMessage spray = GameRoomManager.Instance.FightManager.SprayMessage.Spawn(_inner, Vector3.zero);
                if (spray == null) return;
                spray.Play(this, input.ErrMsg, PanelSystemTips.MessageType.Error);
            }

            // 该指令可以执行,虽然是马后炮
        }

        #endregion
        
        #region 显示路径
//        public void ShowPath()
//        {
//            if (_listPath == null)
//                return;
//            if (_listPath.Count == 0)
//            {
//                _lineRenderer.positionCount = 0;
//                _lineRenderer.gameObject.SetActive(false);
//            }
//            else
//            {
//                _lineRenderer.positionCount = _listPath.Count;//设置线段数
//                for(int i=0; i<_listPath.Count; ++i)
//                {
//                    var cell = _listPath[i];
//                    _lineRenderer.SetPosition(i, cell.Position);                    
//                }
//                _lineRenderer.gameObject.SetActive(true);
//            }
//        }

        public void UpdatePath()
        {
            if (_listPath == null)
            {
                GameRoomManager.Instance.HexmapHelper.ShowPath(null);
                return;
            }

            // 找到本单位当前格子是否在路径上(注意, 有可能确实不在, 比如抄近路的时候, 有可能完全绕过一个格子)
            int find = -1;
            for (int i = 0; i < _listPath.Count; ++i)
            {
                var cell = _listPath[i];
                if (CellIndex == cell.Index)
                {
                    find = i;
                    break;
                }
            }

            // 把这个格子以前的路径都干掉, 然后重画路径, 这样画出来的路径永远是从当前位置到目标点的
            if (find >= 0 && find < _listPath.Count)
            {
                for (int i = 0; i < find; ++i)
                {
                    _listPath.RemoveAt(0);
                }
                GameRoomManager.Instance.HexmapHelper.ShowPath(this);
            }
        }
        #endregion
        
    }

}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Animation;
using GameUtils;
using Google.Protobuf;
using JetBrains.Annotations;
using Protobuf.Lobby;
using Protobuf.Room;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using Random = UnityEngine.Random;
using static FSMStateActor;
namespace AI
{
    /// <summary>
    /// 本类需要ActorManager来进行管理，与ActorManager配套使用，与ActorVisualizer无关，这是为了看以后是否方便移植到服务器去
    /// </summary>
    public class ActorBehaviour
    {
        
        #region 成员
        
        public long RoomId;
        public long OwnerId;
        public long ActorId;
        public int PosX; // 格子坐标
        public int PosZ;
        public float Orientation;
        public string Species = "N/A";
        public int CellIndex; // 根据PosX，PosZ有时候会获取到错误的cell（当PosX,PosZ有一个为负数的时候），所以保存Index是不会出错的
        public int ActorInfoId;
        
        // 这些数据一开始从表格读取
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

        //This specific animal stats asset, create a new one from the asset menu under (LowPolyAnimals/NewAnimalStats)
        private ActorStats ScriptableActorStats;

        public StateMachineActor StateMachine;
        public Vector3 CurrentPosition; // 3D精确坐标，等同于transform.localPosition

        public HexUnit HexUnit;
        private float _distance;
        public float Distance => _distance;
        
        private const float AI_TIME_DELAY = 1f;  // AI思考的间隔时间(秒)
        public bool IsCounterAttack; // 我是否处于反击状态
        
        // AI - 代理权, True-本机拥有本单元的AI控制权 (接收并执行ActorAiStateReply函数)
        public bool HasAiRights;
        // 高级AI状态, 这个主要用于记录, 存盘的时候, 本单位最后的AI状态, 如果中途发生了改变, 当回到IDLE状态的时候, 
        // 本单位应该恢复成这个状态, 例如: 如果是GUARD状态, 中途如果发现了敌人, 自己的状态就变了(变成FIGHT),
        // 但是如果打完敌人(状态变为IDLE), 这个状态应该仍然回到GUARD状态.
        // 但是有的时候又不用回到原来的状态, 例如: WALK, 到达目的地以后, 就可以不用再动了
        public StateEnum HighAiState;
        public int HighAiTargetCell;
        public long HighAiTargetId;
        public float HighAiDurationTime;
        public float HighAiTotalTime;

        //If true, AI changes to this animal will be logged in the console.
        private bool _logChanges = false;
        
        #endregion
        
        #region 初始化

        public void Init(HexUnit hu)
        {
            StateMachine = new StateMachineActor(this);
            HexUnit = hu;
            CurrentPosition = HexUnit.transform.localPosition;
            HasAiRights = false;
            AddListener();
            
        }
        public void Fini()
        {
            RemoveListener();
        }

        private void AddListener()
        {
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.ActorAiStateHighReply, OnActorAiStateHighReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.UpdateActorInfoReply, OnUpdateActorInfoReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.FightStartReply, OnFightStartReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.FightStopReply, OnFightStopReply);
            MsgDispatcher.RegisterMsg((int)ROOM_REPLY.AmmoSupplyReply, OnAmmoSupplyReply);

        }

        private void RemoveListener()
        {
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.ActorAiStateHighReply, OnActorAiStateHighReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.UpdateActorInfoReply, OnUpdateActorInfoReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.FightStartReply, OnFightStartReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.FightStopReply, OnFightStopReply);
            MsgDispatcher.UnRegisterMsg((int)ROOM_REPLY.AmmoSupplyReply, OnAmmoSupplyReply);
        }


        // Update is called once per frame
        private float timeNow = 0;
        public void Tick()
        {
            CurrentPosition = HexUnit.transform.localPosition; // 不是Cell的坐标
            Vector2 curPos2 = new Vector2(CurrentPosition.x, CurrentPosition.y);
            Vector2 targetPos2 = new Vector2(StateMachine.TargetPosition.x, StateMachine.TargetPosition.y);
            //_distance = Vector3.Distance(CurrentPosition, StateMachine.TargetPosition);
            _distance = Vector2.Distance(curPos2, targetPos2);// 
            int posXOld = PosX;
            int posZOld = PosZ;
            var currentCell = HexUnit.Grid.GetCell(CurrentPosition);
            if (currentCell != null)
            {
                if (currentCell.Index == 0)
                {
                    Debug.LogError($"ActorBehaviour Fuck!!! - actor position is lost!!! - Position:{CurrentPosition} - HexUnit:{HexUnit.Location.Position} - local:{HexUnit.transform.localPosition}");
                }

                PosX = currentCell.coordinates.X;
                PosZ = currentCell.coordinates.Z;
                CellIndex = currentCell.Index;
            }

            Orientation = HexUnit.Orientation;
            if (posXOld != PosX || posZOld != PosZ)
            { // 发送最新坐标给服务器
                UpdateActorPos();
            }
            
            StateMachine.Tick();
            
            timeNow += Time.deltaTime;
            if (timeNow < AI_TIME_DELAY)
            {
                return;
            }

            timeNow = 0;
        
            // AI的执行频率要低一些
            AI_Running();
        }

        public void Log(string msg)
        {
            if(_logChanges)
                Debug.Log(msg);
        }
        
        #endregion
        
        #region 外部接口
        public bool IsEnemyInRange(ActorBehaviour abEnemy)
        {
            if (abEnemy == null) return false;
            List<HexCell> cellsInRange = GameRoomManager.Instance.HexmapHelper.GetCellsInRange(HexUnit.Location, (int)ShootingRange);
            if (cellsInRange.Contains(abEnemy.HexUnit.Location))
            {
                return true;
            }

            return false;
        }

        public ActorBehaviour FindEnemyInRange()
        {
            List<HexCell> cellsInRange = GameRoomManager.Instance.HexmapHelper.GetCellsInRange(HexUnit.Location, (int)ShootingRange);
            foreach (HexCell cell in cellsInRange)
            {
                if (cell.Unit != null)
                {
                    var av = cell.Unit.GetComponent<ActorVisualizer>();
                    if (av != null && av.OwnerId != OwnerId && av.CurrentAiState != StateEnum.DIE && av.CurrentAiState != StateEnum.VANISH)
                    { // 绕这么大一圈子,将来移植到服务器的话,需要考虑应该如何做
                        var ab = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(av.ActorId);
                        return ab;
                    }
                }
            }

            return null;
        }
        
        public bool IsDead => StateMachine.CurrentAiState == StateEnum.DIE || StateMachine.CurrentAiState == StateEnum.VANISH;

        public bool IsFighting => HighAiState == StateEnum.FIGHT ||
                                  HighAiState == StateEnum.DELAYFIGHT;

        #endregion
        
        #region AI - 代理权

        /// <summary>
        /// AI - 代理权: 如果我拥有了它的控制权, 则状态机要在这里运行, ActorVisualizer的同名函数会继续运行(接受本函数发送的消息)
        /// </summary>
        /// <param name="bytes"></param>
        private void OnActorAiStateHighReply(byte[] bytes)
        {
            ActorAiStateHighReply input = ActorAiStateHighReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过
            if (!input.Ret)
                return;
            // 如果本地AI可以控制本单位, 或者是当前玩家自己, 则继续, 否则这里返回
            if (!HasAiRights && input.OwnerId != GameRoomManager.Instance.CurrentPlayer.TokenId)
                return;

            HighAiState = (StateEnum) input.HighAiState;// 记录高级AI状态
            HighAiTargetCell = input.HighAiCellIndexTo;
            HighAiTargetId = 0;
            var abTarget = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(input.HighAiTargetId);
            if(abTarget != null && !abTarget.IsDead)
            {
                HighAiTargetId = input.HighAiTargetId;
            }
            HighAiDurationTime = input.HighAiDurationTime;
            HighAiTotalTime = input.HighAiTotalTime;

            StateMachine.TriggerTransition((StateEnum) input.HighAiState, input.HighAiCellIndexTo, input.HighAiTargetId,
                input.HighAiDurationTime, input.HighAiTotalTime);
        }

        #endregion

        #region 消息
        private void UpdateActorPos()
        {
            UpdateActorPos output = new UpdateActorPos()
            {
                RoomId = RoomId,
                OwnerId = OwnerId,
                ActorId = ActorId,
                PosX = PosX,
                PosZ = PosZ,
                CellIndex = CellIndex,
                Orientation = Orientation,
            };
            GameRoomManager.Instance.SendMsg(ROOM.UpdateActorPos, output.ToByteArray());
        }

        private void OnUpdateActorInfoReply(byte[] bytes)
        {
            UpdateActorInfoReply input = UpdateActorInfoReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过
            
            // AI
            Hp = input.Hp;
            AmmoBase = input.AmmoBase;
        }
        
        private void OnFightStartReply(byte[] bytes)
        {
            FightStartReply input = FightStartReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过
            if (!input.Ret)
            {
                GameRoomManager.Instance.Log($"ActorBehaviour OnFightStartReply Error - {input.ErrMsg}");
                StateMachine.TriggerTransition(StateEnum.IDLE);
                return;
            }

            GameRoomManager.Instance.Log("ActorBehaviour OnFightStartReply OK ...");
        }
        
        private void OnFightStopReply(byte[] bytes)
        {
            FightStopReply input = FightStopReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过
            if (!input.Ret)
            {
                GameRoomManager.Instance.Log($"ActorBehaviour OnFightStopReply Error - {input.ErrMsg}");
                return;
            }

            // 每次攻击都要消耗行动点和弹药基数, 所以需要每一轮攻击都判断行动点和弹药基数是否足够
            // 如果在战斗中才进行下次进攻的判定, 否则战斗结束
            if (input.FightAgain && AmmoBase > 0 && CmdAttack.IsActionPointGranted())
            { // 弹药基数足够, 可以再打一轮, 要用 [延迟攻击] 的状态, 时间也要把 [攻击持续时间] & [攻击间隔] 算在一起
                StateMachine.TriggerTransition(StateEnum.DELAYFIGHT, 0, input.TargetId,
                    AttackDuration + AttackInterval, AttackDuration + AttackInterval);
                long roomId = input.RoomId;
                long ownerId = input.OwnerId;
                long actorId = input.ActorId;
                int commandId = (int)CommandManager.CommandID.Attack;
                int actionPointCost = CommandManager.Instance.Commands[CommandManager.CommandID.Attack].ActionPointCost;
                CmdAttack.TryCommand(roomId, ownerId, actorId, commandId, actionPointCost);
            }
            else if (!input.IsEnemyDead && !IsCounterAttack)
            {
                // 我方战斗结束, 如果这时候敌人没死, (我不是处于反击状态), 敌人反击一次
                var abTarget = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(input.TargetId);
                if (abTarget != null)
                {
                    abTarget.IsCounterAttack = true; // 这是反击, 不是主动攻击, 记录在自己身上, Stop的时候用

                    // 反击的时候, 不需要行动点的允许, 直接就可以打
                    abTarget.IsCounterAttack = true; // 这是反击, 不是主动攻击, 记录在自己身上, Stop的时候用
                    abTarget.StateMachine.TriggerTransition(StateEnum.FIGHT, 0, input.ActorId,
                        abTarget.AttackDuration);
                    GameRoomManager.Instance.Log("ActorBehaviour OnFightStopReply - 敌人反击");
                }
            }

            GameRoomManager.Instance.Log($"ActorBehaviour OnFightStop - Ammo:{AmmoBase}/{AmmoBaseMax}");
        }

        private void OnAmmoSupplyReply(byte[] bytes)
        {
            AmmoSupplyReply input = AmmoSupplyReply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过

            AmmoBase = input.AmmoBase;
            
        }
        
        #endregion
        
        private bool bFirst = true;
        private float _lastTime = 0f;
        private float _deltaTime;
        private const float _REST_TIME = 10f;

        #region AI - 第一层
        
        private void AI_Running()
        {
            if (!GameRoomManager.Instance.IsAiOn)
            {
                return;
            }

            // 如果本地AI可以控制本单位, 或者是当前玩家自己, 则继续, 否则这里返回
            if (!HasAiRights && OwnerId != GameRoomManager.Instance.CurrentPlayer.TokenId)
                return;

            if (StateMachine.CurrentAiState == StateEnum.IDLE)
            {
                float lastedTime = StateMachine.GetLastedTime();
                if (lastedTime > _REST_TIME)
                { // 进入休闲状态超过_REST_TIME秒, 也就是说闲置超过_REST_TIME秒的情况下
                    switch (HighAiState)
                    {
                        case StateEnum.GUARD:
                            StateMachine.TriggerTransition(StateEnum.GUARD);
                            break;
                        case StateEnum.WALKFIGHT:
                            // 如果可以直接攻击, 则直接攻击, 否则走过去再攻击
                            if (AmmoBase <= 0)
                            {
                                ActorAiStateHigh output = new ActorAiStateHigh()
                                {
                                    RoomId = RoomId,
                                    OwnerId = OwnerId,
                                    ActorId = ActorId,
                                    HighAiState = (int)StateEnum.IDLE,
                                };
                                GameRoomManager.Instance.SendMsg(ROOM.ActorAiStateHigh, output.ToByteArray());
                            }
                            var ab = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(HighAiTargetId);
                            if (ab == null || ab.IsDead)
                            {
                                HighAiTargetId = 0;
                                if (CellIndex == HighAiTargetCell)
                                { // 结束这个高级AI
                                    ActorAiStateHigh output = new ActorAiStateHigh()
                                    {
                                        RoomId = RoomId,
                                        OwnerId = OwnerId,
                                        ActorId = ActorId,
                                        HighAiState = (int)StateEnum.IDLE,
                                    };
                                    GameRoomManager.Instance.SendMsg(ROOM.ActorAiStateHigh, output.ToByteArray());
                                }
                            }
                            else
                            {
                                if (!ActorWalkFightState.AttackEnemyInRange(this, HighAiTargetId))
                                {
                                    if (AmmoBase > 0)
                                    {
                                        StateMachine.TriggerTransition(StateEnum.WALKFIGHT, HighAiTargetCell,
                                            HighAiTargetId);
                                    }
                                }
                            }
                            break;
                        case StateEnum.WALK: // 解决拥堵问题
                            if (HighAiTargetCell == CellIndex)
                            { // 走到了, 跳出本逻辑
                                ActorAiStateHigh output = new ActorAiStateHigh()
                                {
                                    RoomId = RoomId,
                                    OwnerId = OwnerId,
                                    ActorId = ActorId,
                                    HighAiState = (int)StateEnum.IDLE,
                                };
                                GameRoomManager.Instance.SendMsg(ROOM.ActorAiStateHigh, output.ToByteArray());
                            }
                            else
                            {
                                StateMachine.TriggerTransition(StateEnum.WALK, HighAiTargetCell);
                            }
                            break;
                    }
                }
            }
            
            

//            float range = 100f;
//            if (StateMachine.CurrentAiState == FSMStateActor.StateEnum.IDLE)
//            {
//                float offsetX = Random.Range(-range, range);
//                float offsetZ = Random.Range(-range, range);
//                Vector3 newTargetPosition = new Vector3(CurrentPosition.x + offsetX, CurrentPosition.y, CurrentPosition.z + offsetZ);
//                HexCell newCell = HexUnit.Grid.GetCell(newTargetPosition);
//                if (newCell != null)
//                {
//                    if (HexUnit.IsValidDestination(newCell))
//                    {
//                        SetTarget(newCell.Position);
//                        StateMachine.TriggerTransition(FSMStateActor.StateEnum.WALK);
//                    }
//                }
//            }
        }
        #endregion

        #region AI - 第二层

        #endregion
    }

}

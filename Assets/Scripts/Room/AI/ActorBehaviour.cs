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
        public int Hp_Max;
        public float AttackPower;
        public float DefencePower;
        public float Speed;
        public float FieldOfVision;
        public float ShootingRange;
        
        public float AttackDuration; // 攻击持续时间
        public float AttackInterval; // 攻击间隔
        public int AmmoBase; // 弹药基数

        //This specific animal stats asset, create a new one from the asset menu under (LowPolyAnimals/NewAnimalStats)
        private ActorStats ScriptableActorStats;

        public StateMachineActor StateMachine;
        public Vector3 CurrentPosition; // 3D精确坐标，等同于transform.localPosition

        public HexUnit HexUnit;
        private float _distance;
        public float Distance => _distance;
        
        private float TIME_DELAY;

        //If true, AI changes to this animal will be logged in the console.
        private bool _logChanges = false;
        
        #endregion
        
        #region 初始化

        public void Init(HexUnit hu)
        {
            StateMachine = new StateMachineActor(this);
            HexUnit = hu;
            CurrentPosition = HexUnit.transform.localPosition;
            
        }
        public void Fini()
        {
        }


        // Update is called once per frame
        private float timeNow = 0;
        public void Tick()
        {
            CurrentPosition = HexUnit.transform.localPosition; // 不是Cell的坐标
            _distance = Vector3.Distance(CurrentPosition, StateMachine.TargetPosition);
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
            if (timeNow < TIME_DELAY)
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
                    if (av != null && av.OwnerId != OwnerId)
                    { // 绕这么大一圈子,将来移植到服务器的话,需要考虑应该如何做
                        var ab = GameRoomManager.Instance.RoomLogic.ActorManager.GetActor(av.ActorId);
                        return ab;
                    }
                }
            }

            return null;
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
            if (!GameRoomManager.Instance.IsAiOn)
            {
                return;
            }

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
    }

}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using GameUtils;
using JetBrains.Annotations;
using Protobuf.Lobby;
using Protobuf.Room;
using UnityEngine;
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
        public long CellIndex; // 根据PosX，PosZ有时候会获取到错误的cell（当PosX,PosZ有一个为负数的时候），所以保存Index是不会出错的

        //This specific animal stats asset, create a new one from the asset menu under (LowPolyAnimals/NewAnimalStats)
        private ActorStats ScriptableActorStats;

        public StateMachineActor StateMachine;
        public Vector3 TargetPosition; // 3D精确坐标，等同于transform.localPosition
        public Vector3 CurrentPosition; // 3D精确坐标，等同于transform.localPosition
        public int TargetPosX;
        public int TargetPosZ;
        public HexUnit HexUnit;
        private float _distance;
        private float TIME_DELAY;

        //If true, AI changes to this animal will be logged in the console.
        private bool _logChanges = false;
        
        #endregion
        
        #region 初始化

        public void Init(long roomId, long ownerId, long actorId, int posX, int posZ, float orientation, string species, HexUnit hu, int cellIndex)
        {
            Species = species;
            TIME_DELAY = 1f;
            RoomId = roomId;
            OwnerId = ownerId;
            ActorId = actorId;
            PosX = posX;
            PosZ = posZ;
            Orientation = orientation;
            Species = species;
            StateMachine = new StateMachineActor(this);
            HexUnit = hu;
            CellIndex = cellIndex;
            
            CurrentPosition = HexUnit.transform.localPosition;
            TargetPosition = HexUnit.transform.localPosition;
        }
        public void Fini()
        {
        }


        // Update is called once per frame
        private float timeNow = 0;
        public void Tick()
        {
            _distance = Vector3.Distance(CurrentPosition, TargetPosition);
            CurrentPosition = HexUnit.transform.localPosition;
            int posXOld = PosX;
            int posZOld = PosZ;
            PosX = HexUnit.Grid.GetCell(CurrentPosition).coordinates.X;
            PosZ = HexUnit.Grid.GetCell(CurrentPosition).coordinates.Z;
//            if (posXOld != PosX || posZOld != PosZ)
//            {
//                Debug.Log($"MOVE : From<{posXOld},{posZOld}> - To<{PosX},{PosZ}>");
//            }
            
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

        public void SetTarget(Vector3 targetPosition)
        {
            TargetPosition = targetPosition;
            HexCell hc = HexUnit.Grid.GetCell(targetPosition);
            TargetPosX = hc.coordinates.X;
            TargetPosZ = hc.coordinates.Z;
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

            float range = 100f;
            if (StateMachine.CurrentAiState == FSMStateActor.StateEnum.IDLE)
            {
                float offsetX = Random.Range(-range, range);
                float offsetZ = Random.Range(-range, range);
                Vector3 newTargetPosition = new Vector3(CurrentPosition.x + offsetX, CurrentPosition.y, CurrentPosition.z + offsetZ);
                HexCell newCell = HexUnit.Grid.GetCell(newTargetPosition);
                if (newCell != null)
                {
                    if (HexUnit.IsValidDestination(newCell))
                    {
                        SetTarget(newCell.Position);
                        StateMachine.TriggerTransition(FSMStateActor.StateEnum.WALK);
                    }
                }
            }
        }

        #endregion
    }

}

using Animation;
using Assets.Gamelogic.FSM;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;
using static FSMStateActor;

namespace AI
{
    public class ActorHarvestState : FsmBaseState<StateMachineActor, StateEnum>
    {
        private readonly ActorBehaviour _actorBehaviour;

        public ActorHarvestState(StateMachineActor owner, ActorBehaviour ab) : base(owner)
        {
            _actorBehaviour = ab;
        }

        public override void Enter()
        {
        }

        public override void Tick()
        {
            if (Owner.TimeIsUp())
            {
                Owner.TriggerTransition(StateEnum.IDLE);
            }
        }

        public override void Exit(bool disabled)
        {
            var av = _actorBehaviour.HexUnit.GetComponent<ActorVisualizer>();
            if (av != null)
            { // 这里因为要获得地图上的资源数据,所以只能找到ActorVisualizer,如果改到服务器端要另外处理
                var currentCell = av.HexUnit.Location;
                var resType = currentCell.Res.ResType;
                int resAmount = currentCell.Res.GetAmount(resType);
                int resHarvest = 0;

                if (Owner.TimeIsUp())
                {
                    resHarvest = resAmount;
                    resAmount = 0;
                }
                else
                {
                    resHarvest = Mathf.RoundToInt(resAmount * Owner.GetLastedTime() / Owner.DurationTime);
                    resAmount = resAmount - resHarvest;
                }

                HarvestStop output = new HarvestStop()
                {
                    RoomId = av.RoomId,
                    OwnerId = av.OwnerId,
                    ActorId = av.ActorId,
                    CellIndex = av.CellIndex,
                    ResType = (int)resType,
                    ResRemain = resAmount,
                    ResHarvest = resHarvest,
                };
                GameRoomManager.Instance.SendMsg(ROOM.HarvestStop, output.ToByteArray());
            }
        }
    }
}
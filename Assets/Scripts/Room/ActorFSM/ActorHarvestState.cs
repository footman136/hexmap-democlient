using Animation;
using Assets.Gamelogic.FSM;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;

namespace AI
{
    public class ActorHarvestState : FsmBaseState<StateMachineActor, FSMStateActor.StateEnum>
    {
        private readonly ActorBehaviour _actorBehaviour;
        private float timeRemain;

        public ActorHarvestState(StateMachineActor owner, ActorBehaviour ab) : base(owner)
        {
            _actorBehaviour = ab;
        }

        public override void Enter()
        {
        }

        public override void Tick()
        {
            timeRemain = Time.time - Owner._startTime;
            if (timeRemain >= _actorBehaviour.DurationTime)
            {
                Owner.TriggerTransition(FSMStateActor.StateEnum.IDLE);
            }
        }

        public override void Exit(bool disabled)
        {
            var av = _actorBehaviour.HexUnit.GetComponent<ActorVisualizer>();
            if (av != null)
            {
                var currentCell = av.HexUnit.Location;
                var resType = currentCell.Res.ResType;
                int resAmount = currentCell.Res.GetAmount(resType);
                int resHarvest = 0;

                if (timeRemain >= _actorBehaviour.DurationTime)
                {
                    resHarvest = resAmount;
                    resAmount = 0;
                }
                else
                {
                    resHarvest = Mathf.RoundToInt(resAmount * timeRemain / _actorBehaviour.DurationTime);
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
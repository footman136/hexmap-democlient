using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AI
{
    /// <summary>
    /// 本类与ActorBehaviour配合使用，专门管理ActorBehaviour
    /// </summary>
    public class ActorManager
    {
        private Dictionary<long, ActorBehaviour> _allActors = new Dictionary<long, ActorBehaviour>();
        public Dictionary<long, ActorBehaviour> AllActors => _allActors;

        public void AddActor(long roomId, long ownerId, long actorId, int posX, int posZ, float orientation, string species, HexUnit hu, int cellIndex)
        {
            ActorBehaviour ab = new ActorBehaviour();
            ab.Init(roomId, ownerId, actorId, posX, posZ, orientation, species, hu, cellIndex);
            
            _allActors.Add(actorId, ab);
        }

        public void RemoveActor(long actorId)
        {
            if (_allActors.ContainsKey(actorId))
            {
                var actor = _allActors[actorId];
                actor.Fini();
                _allActors.Remove(actorId);
            }
        }

        public ActorBehaviour GetPlayer(long actorId)
        {
            if (_allActors.ContainsKey(actorId))
            {
                return _allActors[actorId];
            }

            return null;
        }

        public void Tick()
        {
            foreach (var keyValue in _allActors)
            {
                keyValue.Value.Tick();
            }
        }
    }
}
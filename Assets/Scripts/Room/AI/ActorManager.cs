using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
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

        public void AddActor(ActorBehaviour ab, HexUnit hu)
        {
            ab.Init(hu);
            _allActors.Add(ab.ActorId, ab);
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

        public ActorBehaviour GetActor(long actorId)
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
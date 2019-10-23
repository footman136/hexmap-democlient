using System;

namespace Animation
{
  [Serializable]
  public class MovementState : AnimationState
  {
    public float maxStateTime = 40f;
    public float moveSpeed = 3f;
    public float turnSpeed = 120f;
  }
}
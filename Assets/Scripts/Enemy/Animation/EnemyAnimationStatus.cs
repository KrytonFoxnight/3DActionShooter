using System;
using System.Collections.Generic;
using UnityEngine;

namespace Enemy.Animation
{
    public enum EnemyAnimationStatus
    {
        Attack,
        GetHit,
        Death,
        Idle,
        MoveSpeed,
        MoveSpeedMultiplier,
    }

    public static class EnemyAnimationStatusExtensions
    {
        public static string GetAnimatorName(this EnemyAnimationStatus status)
        {
            return status switch
            {
                EnemyAnimationStatus.Attack => "Attack",
                EnemyAnimationStatus.GetHit => "GetHit",
                EnemyAnimationStatus.Death => "Death",
                EnemyAnimationStatus.Idle => "Idle",
                EnemyAnimationStatus.MoveSpeed => "MoveSpeed",
                EnemyAnimationStatus.MoveSpeedMultiplier => "MoveSpeedMultiplier",
                _ => throw new ArgumentOutOfRangeException("invalid enum value: " + status)
            };
        }

        private static readonly Dictionary<EnemyAnimationStatus, int> AnimatorHashCache = new();

        public static int GetAnimatorHash(this EnemyAnimationStatus status)
        {
            if(AnimatorHashCache.TryGetValue(status, out var hash))
                return hash;

            hash = Animator.StringToHash(status.GetAnimatorName());
            AnimatorHashCache[status] = hash;
            return hash;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainingDummy.Animation
{
    public enum TrainingDummyAnimationStatus
    {
        Attack,
        GetHit,
        Death,
        Idle,
    }

    public static class TrainingDummyAnimationStatusExtensions
    {
        public static string GetAnimatorName(this TrainingDummyAnimationStatus status)
        {
            return status switch
            {
                TrainingDummyAnimationStatus.Attack => "Attack",
                TrainingDummyAnimationStatus.GetHit => "GetHit",
                TrainingDummyAnimationStatus.Death => "Death",
                TrainingDummyAnimationStatus.Idle => "Idle",
                _ => throw new ArgumentOutOfRangeException("invalid enum value: " + status)
            };
        }

        private static readonly Dictionary<TrainingDummyAnimationStatus, int> AnimatorHashCache = new();

        public static int GetAnimatorHash(this TrainingDummyAnimationStatus status)
        {
            if(AnimatorHashCache.TryGetValue(status, out var hash))
                return hash;

            hash = Animator.StringToHash(status.GetAnimatorName());
            AnimatorHashCache[status] = hash;
            return hash;
        }
    }
}

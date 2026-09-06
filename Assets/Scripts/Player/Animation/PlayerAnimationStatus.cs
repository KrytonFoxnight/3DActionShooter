using System;
using System.Collections.Generic;
using UnityEngine;

namespace Player.Animation
{
    public enum PlayerAnimationStatus
    {
        Attack,
        GetHit,
        Death,
        Jump,
        Dash,
        IsGrounded,
        MoveX,
        MoveY,
        DashX,
        DashY,
    }

    public static class PlayerAnimationStatusExtension
    {
        public static string GetAnimatorName(this PlayerAnimationStatus status)
        {
            return status switch
            {
                PlayerAnimationStatus.Attack => "Attack",
                PlayerAnimationStatus.GetHit => "GetHit",
                PlayerAnimationStatus.Death => "Death",
                PlayerAnimationStatus.Jump => "Jump",
                PlayerAnimationStatus.Dash => "Dash",
                PlayerAnimationStatus.IsGrounded => "IsGrounded",
                PlayerAnimationStatus.MoveX => "MoveX",
                PlayerAnimationStatus.MoveY => "MoveY",
                PlayerAnimationStatus.DashX => "DashX",
                PlayerAnimationStatus.DashY => "DashY",
                _ => throw new ArgumentOutOfRangeException("invalid enum value: " + status)
            };
        }

        private static readonly Dictionary<PlayerAnimationStatus, int> AnimatorHashCache = new();

        public static int GetAnimatorHash(this PlayerAnimationStatus status)
        {
            if(AnimatorHashCache.TryGetValue(status, out var hash))
                return hash;

            hash = Animator.StringToHash(status.GetAnimatorName());
            AnimatorHashCache[status] = hash;
            return hash;
        }
    }
}

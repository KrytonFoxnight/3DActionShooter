using Player;
using UnityEngine;

namespace Cameras
{
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter player;

        [SerializeField] private float duration = 0.25f;
        [SerializeField] private float magnitude = 0.18f;
        [SerializeField] private float frequency = 22f;

        private float _remainTime;
        private float _elapsed;
        private float _seedX;
        private float _seedY;

        private void Awake()
        {
            _seedX = Random.value * 100f;
            _seedY = Random.value * 100f;

            if (player) player.Damaged += Play;
        }

        private void OnDestroy()
        {
            if (player) player.Damaged -= Play;
        }

        public void Play()
        {
            if (duration <= 0f || magnitude <= 0f) return;

            _remainTime = duration;
            _elapsed = 0f;
        }

        public void StopImmediate()
        {
            _remainTime = 0f;
            _elapsed = 0f;
        }

        public Vector3 Evaluate(float deltaTime)
        {
            if (_remainTime <= 0f) return Vector3.zero;

            _remainTime -= deltaTime;
            _elapsed += deltaTime;

            if (_remainTime <= 0f) return Vector3.zero;

            var damper = duration > 0f ? _remainTime / duration : 0f;
            damper *= damper;

            var noiseX = (Mathf.PerlinNoise(_seedX, _elapsed * frequency) - 0.5f) * 2f;
            var noiseY = (Mathf.PerlinNoise(_seedY, _elapsed * frequency) - 0.5f) * 2f;

            return new Vector3(noiseX, noiseY, 0f) * (magnitude * damper);
        }
    }
}

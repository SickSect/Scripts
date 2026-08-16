using Core.Player;
using UnityEngine;

namespace Core.Inventory
{
    /// <summary>
    /// Подсветка предмета реальным источником света (Point Light), интенсивность которого
    /// растёт по мере приближения игрока: далеко — не светит, близко — максимум.
    ///
    /// Не трогает материалы и не использует спрайты — только компонент Light.
    /// Хорошо смотрится в тёмных сценах (предмет "сияет").
    ///
    /// Настройка: на предмете дочерний объект с Point Light, на него этот скрипт.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class PickupGlowLight : MonoBehaviour
    {
        [Header("Дистанции")]
        [Tooltip("Дальше этого — не светит.")]
        [SerializeField] private float _maxDistance = 8f;
        [Tooltip("Ближе этого — максимальная яркость.")]
        [SerializeField] private float _minDistance = 1.5f;

        [Header("Яркость")]
        [Tooltip("Максимальная интенсивность света вблизи.")]
        [SerializeField] private float _maxIntensity = 3f;

        [Header("Пульсация (опционально)")]
        [Tooltip("Лёгкое 'дыхание' яркости, когда предмет виден.")]
        [SerializeField] private bool _pulse = true;
        [SerializeField] private float _pulseSpeed = 2f;
        [SerializeField] private float _pulseAmount = 0.2f;

        [Header("Производительность")]
        [SerializeField] private float _updateInterval = 0.05f;

        private Light _light;
        private Transform _player;
        private float _timer;
        private float _baseIntensity; // целевая яркость по расстоянию (до пульсации)

        private void Awake()
        {
            _light = GetComponent<Light>();
            _light.type = LightType.Point;
            _light.intensity = 0f;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _updateInterval;
                RecalcBase();
            }

            // Пульсация поверх базовой яркости (плавно, каждый кадр).
            float intensity = _baseIntensity;
            if (_pulse && _baseIntensity > 0.01f)
            {
                float p = 1f + Mathf.Sin(Time.time * _pulseSpeed) * _pulseAmount;
                intensity *= p;
            }
            _light.intensity = intensity;
        }

        private void RecalcBase()
        {
            var player = FindPlayer();
            if (player == null) { _baseIntensity = 0f; return; }

            float dist = Vector3.Distance(transform.position, player.position);
            float t = Mathf.InverseLerp(_maxDistance, _minDistance, dist); // 1 близко → 0 далеко
            _baseIntensity = t * _maxIntensity;
        }

        private Transform FindPlayer()
        {
            if (_player != null) return _player;
            var pm = Object.FindAnyObjectByType<PlayerMovement>();
            _player = pm != null ? pm.transform : null;
            return _player;
        }
    }
}
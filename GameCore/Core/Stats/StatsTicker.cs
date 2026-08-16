using UnityEngine;

namespace Core.Stats
{
    /// <summary>
    /// Тикает StatsService каждый кадр. Живёт как глобальный объект (создаётся в InitStep).
    /// Использует unscaledDeltaTime? Нет — обычный deltaTime, чтобы на паузе (timeScale=0)
    /// статы замирали.
    /// </summary>
    public class StatsTicker : MonoBehaviour
    {
        private StatsService _service;

        public void Init(StatsService service) => _service = service;

        private void Update()
        {
            if (_service != null)
                _service.Tick(Time.deltaTime);
        }
    }
}

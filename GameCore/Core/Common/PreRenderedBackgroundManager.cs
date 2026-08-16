using UnityEngine;

namespace Core.Common
{
    /// <summary>
    /// Менеджер пререндеренных фонов для уровня.
    /// Переключает видимость рендеров в зависимости от зоны, в которой находится игрок.
    /// 
    /// Настройка:
    /// 1. Создайте на сцене пустой GameObject и добавьте этот скрипт.
    /// 2. Для каждой зоны (колайдера) создайте запись в массиве ZoneRenders.
    /// 3. В поле Zone Collider укажите триггер-коллайдер зоны.
    /// 4. В поле Background Render укажите GameObject с пререндеренным фоном для этой зоны.
    /// 5. Убедитесь, что на игроке есть тег "Player".
    /// 
    /// Все рендеры фона должны быть активны на сцене initially (скрипт сам их скроет при старте, кроме первого).
    /// </summary>
    public class PreRenderedBackgroundManager : MonoBehaviour
    {
        [System.Serializable]
        public class ZoneRenderPair
        {
            [Tooltip("Коллайдер-триггер зоны")]
            public Collider zoneCollider;
            
            [Tooltip("GameObject с пререндеренным фоном для этой зоны")]
            public GameObject backgroundRender;
        }

        [Tooltip("Массив пар зона-рендер")]
        public ZoneRenderPair[] zoneRenders;

        [Tooltip("Тег игрока для отслеживания")]
        public string playerTag = "Player";

        [Tooltip("Фоновый рендер по умолчанию (показывается когда игрок не в одной из зон)")]
        public GameObject defaultBackgroundRender;

        private int _currentZoneIndex = -1;

        private void Start()
        {
            // Скрываем все рендеры при старте
            HideAllRenders();
            
            // Если есть дефолтный рендер, показываем его
            if (defaultBackgroundRender != null)
            {
                defaultBackgroundRender.SetActive(true);
            }
            else if (zoneRenders.Length > 0 && zoneRenders[0].backgroundRender != null)
            {
                // Если нет дефолтного, показываем первый рендер
                zoneRenders[0].backgroundRender.SetActive(true);
                _currentZoneIndex = 0;
            }
        }

        private void HideAllRenders()
        {
            if (defaultBackgroundRender != null)
            {
                defaultBackgroundRender.SetActive(false);
            }

            foreach (var zoneRender in zoneRenders)
            {
                if (zoneRender.backgroundRender != null)
                {
                    zoneRender.backgroundRender.SetActive(false);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag))
            {
                return;
            }

            // Ищем зону, в которую вошел игрок
            for (int i = 0; i < zoneRenders.Length; i++)
            {
                if (zoneRenders[i].zoneCollider == other)
                {
                    SwitchToZone(i);
                    break;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag))
            {
                return;
            }

            // Проверяем, вышли ли мы из текущей активной зоны
            if (_currentZoneIndex >= 0 && _currentZoneIndex < zoneRenders.Length)
            {
                if (zoneRenders[_currentZoneIndex].zoneCollider == other)
                {
                    // Выход из зоны - переключаемся на дефолтный или остаемся без фона
                    HideAllRenders();
                    _currentZoneIndex = -1;
                    
                    // Показываем дефолтный фон если он есть
                    if (defaultBackgroundRender != null)
                    {
                        defaultBackgroundRender.SetActive(true);
                    }
                }
            }
        }

        private void SwitchToZone(int zoneIndex)
        {
            if (zoneIndex < 0 || zoneIndex >= zoneRenders.Length)
            {
                CoreLog.Debug($"[PreRenderedBackgroundManager] Invalid zone index: {zoneIndex}");
                return;
            }

            // Если уже в этой зоне, ничего не делаем
            if (_currentZoneIndex == zoneIndex)
            {
                return;
            }

            // Скрываем все рендеры
            HideAllRenders();

            // Показываем рендер новой зоны
            if (zoneRenders[zoneIndex].backgroundRender != null)
            {
                zoneRenders[zoneIndex].backgroundRender.SetActive(true);
                _currentZoneIndex = zoneIndex;
                CoreLog.Debug($"[PreRenderedBackgroundManager] Switched to zone {zoneIndex}");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Проверка на дубликаты коллайдеров в инспекторе
            if (zoneRenders != null)
            {
                for (int i = 0; i < zoneRenders.Length; i++)
                {
                    for (int j = i + 1; j < zoneRenders.Length; j++)
                    {
                        if (zoneRenders[i].zoneCollider == zoneRenders[j].zoneCollider && 
                            zoneRenders[i].zoneCollider != null)
                        {
                            CoreLog.Debug($"[PreRenderedBackgroundManager] Warning: Duplicate zone collider at indices {i} and {j}");
                        }
                    }
                }
            }
        }
#endif
    }
}

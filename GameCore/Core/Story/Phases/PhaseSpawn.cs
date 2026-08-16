using UnityEngine;

namespace Core.Story.Phases
{
    /// <summary>
    /// Базовый спавн фазы (ScriptableObject): что появится в якоре при старте фазы.
    /// Типизированные наследники: ItemPhaseSpawn (предмет), EventPhaseSpawn (событие).
    ///
    /// Новый тип наполнения = новый наследник. PhaseService и фаза не меняются.
    ///
    /// Контроль повторного спавна: перед спавном PhaseService спрашивает IsConsumed —
    /// «уже получено игроком?». Если да (взял предмет / выдан триггер) — спавн пропускается.
    /// Если нет (спавнилось, но не взяли) — спавнится снова.
    /// </summary>
    public abstract class PhaseSpawn : ScriptableObject
    {
        [Tooltip("В какой якорь сцены спавнить (SpawnAnchor.anchorId).")]
        public string anchorId;

        /// <summary>Заспавнить наполнение в свой якорь. Возвращает созданный объект (или null).</summary>
        public abstract GameObject Spawn(PhaseSpawnContext context);

        /// <summary>
        /// «Уже получено игроком?» — если true, спавн пропускается (не респавнить взятое).
        /// Предмет: проверяет сценовую метку (uniqueId). Событие: проверяет триггер.
        /// По умолчанию false (спавнить всегда).
        /// </summary>
        public virtual bool IsConsumed(PhaseSpawnContext context) => false;
    }
}

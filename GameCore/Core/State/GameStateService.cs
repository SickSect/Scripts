using System.Collections.Generic;
using Core.Save;
using UnityEngine;

namespace Core.State
{
    /// <summary>
    /// Держит текущее состояние игры (снапшот) и умеет:
    ///  - собрать актуальный снапшот из рантайма (Capture),
    ///  - сохранить его в файл (Save).
    ///
    /// Механики регистрируют свои IStateContributor, чтобы попадать в снапшот.
    /// Это ядровая "сущность состояния игры", о которой шла речь.
    /// </summary>
    public class GameStateService
    {
        private readonly JsonSaveProvider _save;
        private readonly List<IStateContributor> _contributors = new();

        /// <summary>Рабочая (мутабельная) копия снапшота на текущую сессию.</summary>
        public GameStateData Runtime { get; private set; }

        public GameStateService(JsonSaveProvider save) => _save = save;

        /// <summary>Загрузить снапшот в сервис при входе на игровую сцену.</summary>
        public void SetState(GameStateData state)
        {
            // Runtime — независимая копия, чтобы правки в игре не били по origin до сохранения.
            Runtime = state.Clone();
        }

        public void RegisterContributor(IStateContributor contributor)
        {
            if (contributor != null && !_contributors.Contains(contributor))
                _contributors.Add(contributor);
        }

        /// <summary>Собрать полный актуальный снапшот текущего состояния игры.</summary>
        public GameStateData Capture()
        {
            foreach (var c in _contributors)
                c.CaptureInto(Runtime);

            Runtime.timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Runtime;
        }

        /// <summary>Собрать снапшот и записать его в слот.</summary>
        public void Save()
        {
            var snapshot = Capture();
            _save.Save(snapshot.currentSaveSlotId, snapshot);
#if UNITY_EDITOR
            Debug.Log($"[GameStateService] Сохранено в слот {snapshot.currentSaveSlotId}: {JsonUtility.ToJson(snapshot)}");
#endif
        }
    }
}

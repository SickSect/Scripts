using Core.Init;
using UnityEngine;

namespace Core.Audio
{
    /// <summary>
    /// Сценовый шаг звука: раздаёт компонентам сцены ссылку на AudioService.
    /// Сам сервис живёт в root и создаётся один раз в GameBootstrap — здесь только привязка.
    ///
    /// Order 25 — ПОСЛЕ камеры (CameraInitStep, 20). Причина в слушателе: AudioListener
    /// висит на камере, которая привязывается к игроку уже после загрузки сцены.
    /// Steam Audio требует, чтобы о такой подмене ему сообщили явно, иначе окклюзия
    /// считается относительно старого или несуществующего слушателя.
    ///
    /// Побочная польза: фоновые петли (SoundEmitter) стартуют, когда слушатель
    /// уже на месте, и не звучат первые кадры «из ниоткуда».
    /// </summary>
    public class AudioInitStep : IInitStep
    {
        public int Order => 25;

        public void Execute(InitContext ctx)
        {
            if (!ctx.Root.TryResolve<AudioService>(out var audio))
            {
                Debug.LogWarning("[AudioInitStep] AudioService не зарегистрирован — звука не будет.");
                return;
            }

            NotifyListenerChanged();

            // Звуки игрока (шаги). Игрок уже заспавнен PlayerInitStep-ом.
            var playerAudio = Object.FindObjectsByType<PlayerAudio>(FindObjectsInactive.Include);
            foreach (var pa in playerAudio) pa.Bind(audio);

            if (playerAudio.Length == 0)
                Debug.LogWarning("[AudioInitStep] PlayerAudio не найден — шагов не будет.");
            else
                Core.Common.CoreLog.Debug($"[AudioInitStep] привязан PlayerAudio ({playerAudio.Length} шт.)");

            // Точечные источники в мире (холодильник, лампа, телевизор).
            var emitters = Object.FindObjectsByType<SoundEmitter>(FindObjectsInactive.Include);
            foreach (var e in emitters) e.Bind(audio, ctx.Root);

            if (emitters.Length > 0)
                Core.Common.CoreLog.Debug($"[AudioInitStep] привязано SoundEmitter: {emitters.Length}");

            // Отладочные проигрыватели (если остались в сцене).
            foreach (var tester in Object.FindObjectsByType<Core.Audio.Debugging.AudioTester>(
                         FindObjectsInactive.Include))
                tester.Bind(audio);
        }

        /// <summary>
        /// Сказать Steam Audio, что слушатель сменился. Без плагина метод пуст —
        /// весь остальной звук от этого не зависит.
        /// </summary>
        private static void NotifyListenerChanged()
        {
#if STEAMAUDIO_ENABLED
            var listener = Object.FindAnyObjectByType<AudioListener>();
            if (listener == null)
            {
                Debug.LogWarning("[AudioInitStep] AudioListener в сцене не найден — окклюзия работать не будет.");
                return;
            }

            SteamAudio.SteamAudioManager.NotifyAudioListenerChanged();
            Core.Common.CoreLog.Debug($"[AudioInitStep] Steam Audio: слушатель '{listener.name}'");
#endif
        }
    }
}

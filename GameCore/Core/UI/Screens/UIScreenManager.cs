using Core.Signals;
using R3;
using UnityEngine;

namespace Core.UI.Screens
{
    /// <summary>
    /// Арбитр блокирующих UI-экранов. Гарантирует: открыт максимум один экран за раз.
    /// Централизованно управляет timeScale и переключением ввода (пауза/континью),
    /// чтобы экраны не конфликтовали.
    ///
    /// Экраны зовут RequestOpen/RequestClose/RequestToggle вместо того, чтобы
    /// самим дёргать timeScale и сигналы.
    ///
    /// timeScale трогается только для экранов с PausesGame = true. Диегетические
    /// экраны (компьютер, телевизор) открываются без остановки мира.
    ///
    /// Живёт в root (регистрируется в GameBootstrap).
    /// </summary>
    public class UIScreenManager
    {
        private readonly Subject<Unit> _pauseSignal;
        private readonly Subject<Unit> _continueSignal;

        private IUIScreen _current;   // единственный открытый экран (или null)
        private bool _timeStopped;    // остановили ли мы время под текущий экран

        public bool AnyOpen => _current != null;

        /// <summary>Текущий открытый экран (или null). Полезно для отладки.</summary>
        public IUIScreen Current => _current;

        public UIScreenManager(Subject<Unit> pauseSignal, Subject<Unit> continueSignal)
        {
            _pauseSignal = pauseSignal;
            _continueSignal = continueSignal;
        }

        /// <summary>Открыть экран. Не откроет, если уже открыт другой.</summary>
        public bool RequestOpen(IUIScreen screen)
        {
            if (screen == null) return false;
            if (_current != null) return false; // уже что-то открыто — не пускаем второй

            _current = screen;
            screen.OpenScreen();

            _timeStopped = screen.PausesGame;
            if (_timeStopped) Time.timeScale = 0f;

            _pauseSignal.OnNext(Unit.Default); // ввод → UI, стоп PlayerLook
            return true;
        }

        /// <summary>Закрыть конкретный экран (если это он открыт).</summary>
        public void RequestClose(IUIScreen screen)
        {
            if (_current != screen) return;

            screen.CloseScreen();
            _current = null;

            if (_timeStopped)
            {
                Time.timeScale = 1f;
                _timeStopped = false;
            }

            _continueSignal.OnNext(Unit.Default); // ввод → Player
        }

        /// <summary>Открыть/закрыть по клавише. Открывает только если ничего не открыто;
        /// закрывает только свой экран.</summary>
        public void RequestToggle(IUIScreen screen)
        {
            if (screen.IsOpen) RequestClose(screen);
            else RequestOpen(screen);
        }

        /// <summary>Закрыть что угодно открытое (например, при выходе со сцены).</summary>
        public void CloseAny()
        {
            if (_current != null) RequestClose(_current);
        }
    }
}

using UnityEngine;

namespace Core.Common
{
    /// <summary>
    /// Единая точка логирования отладки. Выключается одним флагом (или соберётся пусто в билде).
    /// Использование: CoreLog.Debug("[Interact] ...");
    /// </summary>
    public static class CoreLog
    {
        /// <summary>Глобально вкл/выкл отладочные логи ядра.</summary>
        public static bool Enabled = true;

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Debug(string message)
        {
            if (Enabled) UnityEngine.Debug.Log(message);
        }
    }
}

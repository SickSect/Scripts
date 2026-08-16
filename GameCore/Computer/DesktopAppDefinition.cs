using UnityEngine;

namespace Core.Computer
{
    /// <summary>
    /// Приложение рабочего стола: почта, просмотрщик, блокнот, анализатор.
    ///
    /// Описание отделено от префаба, чтобы иконку, заголовок и доступность
    /// можно было менять из данных, не трогая сцену. Доступность привязана к
    /// флагу истории: анализатор появляется на столе не с первого дня.
    /// </summary>
    [CreateAssetMenu(fileName = "APP_", menuName = "Core/Computer/App")]
    public class DesktopAppDefinition : ScriptableObject
    {
        [Header("Идентификация")]
        [Tooltip("Уникальный id. По нему приложение открывается из кода и диалогов.")]
        public string id = "app";

        [Tooltip("Подпись под иконкой и в заголовке окна.")]
        public string title = "Приложение";

        [Header("Вид")]
        public Sprite icon;

        [Tooltip("Префаб окна. Корень должен нести компонент DesktopWindow.")]
        public DesktopWindow windowPrefab;

        [Header("Поведение")]
        [Tooltip("Открывать ли повторный клик как новое окно. Обычно нет: одно окно на приложение.")]
        public bool allowMultipleWindows = false;

        [Tooltip("Где появится окно при первом открытии, в пикселях от центра стола.")]
        public Vector2 spawnOffset = Vector2.zero;
    }
}

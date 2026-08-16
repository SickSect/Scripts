using System;
using System.Collections.Generic;
using Core.Inventory;
using UnityEngine;

namespace Core.Player
{
    /// <summary>
    /// Точка изучения. Реплика + выборы, каждый ссылается на готовые выключенные катсцены.
    /// Выбор может требовать предмет из инвентаря (пусто = не нужен) и не иметь катсцены
    /// (простое действие — открыть ключом, наорать). Открывается по Q или по E (через
    /// ExamineInteractable). Всё здесь, без ScriptableObjectّов.
    /// </summary>
    public class ExaminePoint : MonoBehaviour
    {
        [Serializable]
        public class Choice
        {
            public string Label = "Действие";

            [Tooltip("Это выход — просто закрыть меню (напр. «Оставить в покое»).")]
            public bool IsLeave = false;

            [Header("Требуется предмет (пусто = не нужен)")]
            [Tooltip("Нужен в инвентаре, иначе пункт не показывается.")]
            public ItemDefinition RequiredItem;
            public int RequiredCount = 1;
            [Tooltip("Списать предмет при использовании этого выбора.")]
            public bool ConsumeItem = false;

            [Header("Проверка")]
            [Range(0f, 1f)]
            [Tooltip("Шанс успеха. 1 = всегда успех (проверки нет), 0 = всегда провал.")]
            public float SuccessChance = 1f;

            [Header("Катсцены (пусто = без катсцены, простое действие)")]
            public GameObject SuccessScene;
            public float SuccessDuration = 3f;
            public GameObject FailScene;
            public float FailDuration = 3f;

            [Header("После УСПЕХА поменять мир")]
            public GameObject[] EnableOnSuccess;
            public GameObject[] DisableOnSuccess;

            [Tooltip("После успеха убрать пункт из меню (и не показывать при повторном открытии).")]
            public bool ConsumeOnSuccess = true;
            [Tooltip("После действия сразу вернуться в игру. Выкл = вернуться в меню.")]
            public bool ExitAfterCutscene = true;

            [Header("Скрыть на время ЭТОЙ катсцены")]
            public GameObject[] HideDuringCutscene;
        }

        [Header("Осмотр")]
        [SerializeField] private string _speaker = "";
        [TextArea]
        [SerializeField] private string _examineLine = "Никогда не любил этот цветок.";

        [Tooltip("Открывать по клавише Q (прицел). Выкл = только через E/ExamineInteractable (дверь).")]
        [SerializeField] private bool _respondToQ = true;

        [Tooltip("Сцена «раздумья»: актёр + камера. Выключена. Пусто = меню поверх текущего вида.")]
        [SerializeField] private GameObject _thinkingScene;

        [Header("Скрытие дублируемых объектов (МОДЕЛИ)")]
        [SerializeField] private GameObject[] _hideDuringThinking;
        [SerializeField] private GameObject[] _hideDuringCutscene;

        [Header("Выборы")]
        [SerializeField] private List<Choice> _choices = new();

        public string Speaker => _speaker;
        public string ExamineLine => _examineLine;
        public bool RespondToQ => _respondToQ;
        public GameObject ThinkingScene => _thinkingScene;
        public GameObject[] HideDuringThinking => _hideDuringThinking;
        public GameObject[] HideDuringCutscene => _hideDuringCutscene;
        public IReadOnlyList<Choice> Choices => _choices;

        private readonly HashSet<int> _consumed = new();
        public bool IsConsumed(int i) => _consumed.Contains(i);
        public void MarkConsumed(int i) => _consumed.Add(i);
    }
}
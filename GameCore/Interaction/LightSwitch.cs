using System.Collections.Generic;
using Core.Audio;
using Core.Common;
using Core.Interaction;
using R3;
using UnityEngine;

namespace Core.Interaction
{
    /// <summary>
    /// Выключатель света. Вешается на объект выключателя (нужен коллайдер, чтобы
    /// его брал LookTarget), по Interact переключает привязанные лампы.
    ///
    /// Ламп может быть сколько угодно — одна люстра или пять бра в коридоре.
    /// Дополнительно умеет гасить/зажигать Emission на материалах (плафон,
    /// экран лампы) и играть разные звуки на включение и выключение.
    ///
    /// Состояние — ReactiveProperty: на него может подписаться что угодно
    /// (сюжетные условия, ночная фаза, SoundEmitter гудящей лампы).
    ///
    /// Bind не нужен: AudioService достаётся из InteractionContext.Root,
    /// как в StoryEventInteractable.
    /// </summary>
    public class LightSwitch : MonoBehaviour, IInteractable
    {
        [Header("Лампы")]
        [Tooltip("Все источники света, которыми управляет этот выключатель.")]
        [SerializeField] private List<Light> _lights = new();

        [Tooltip("Начальное состояние. Применяется в Awake — не зависит от того, " +
                 "в каком положении лампы оставлены в редакторе.")]
        [SerializeField] private bool _startsOn = false;

        [Header("Свечение материалов")]
        [Tooltip("Рендереры плафонов: у их материалов включается/выключается Emission. " +
                 "Пусто — ничего не трогаем.")]
        [SerializeField] private List<Renderer> _emissiveRenderers = new();

        [Tooltip("Цвет Emission во включённом состоянии. Яркость (HDR) задаёт силу свечения.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _emissionColor = Color.white;

        [Tooltip("Индекс материала в рендерере, у которого меняем Emission.")]
        [SerializeField] private int _emissiveMaterialIndex = 0;

        [Header("Звук")]
        [Tooltip("Щелчок включения. SoundDefinition со spatialBlend = 1.")]
        [SerializeField] private SoundDefinition _turnOnSound;

        [Tooltip("Щелчок выключения. Отдельный звук — вверх и вниз щёлкает по-разному.")]
        [SerializeField] private SoundDefinition _turnOffSound;

        [Range(0f, 2f)]
        [Tooltip("Множитель поверх громкости ассета — подкрутить конкретный выключатель.")]
        [SerializeField] private float _volumeScale = 1f;

        [Header("Клавиша")]
        [Tooltip("Трансформ клавиши выключателя (дочерний объект корпуса). Пусто — ничего не крутим.")]
        [SerializeField] private Transform _button;

        [Tooltip("Угол наклона клавиши по Z во включённом состоянии, градусы. " +
                 "Выключенное состояние — минус этот угол.")]
        [Range(-45f, 45f)]
        [SerializeField] private float _buttonAngle = 12f;

        [Tooltip("Скорость поворота, градусов в секунду. 0 — мгновенно.")]
        [SerializeField] private float _buttonSpeed = 360f;

        [Header("Подсказка")]
        [SerializeField] private string _promptOn = "Включить свет";
        [SerializeField] private string _promptOff = "Выключить свет";

        [Header("Поведение")]
        [Tooltip("Задержка между щелчком и срабатыванием света, сек. " +
                 "Небольшая задержка звучит живее, чем идеальное совпадение.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _lightDelay = 0f;

        [Tooltip("Заблокировать выключатель. Свет остаётся в текущем состоянии, " +
                 "взаимодействие не срабатывает (выбило пробки, заклинило).")]
        [SerializeField] private bool _locked = false;

        /// <summary>Горит ли свет. Подписывайся, если нужно реагировать на переключение.</summary>
        public ReactiveProperty<bool> IsOn { get; } = new(false);

        public bool Locked
        {
            get => _locked;
            set => _locked = value;
        }

        public string Prompt => IsOn.Value ? _promptOff : _promptOn;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock _propertyBlock;
        private float _pendingAt = -1f;
        private bool _pendingState;

        // Базовый поворот клавиши — то, как она стоит в префабе. Наклон идёт от него,
        // поэтому выключатель можно вешать на стену под любым углом.
        private Quaternion _buttonNeutral;
        private bool _buttonCaptured;

        private void Awake()
        {
            CaptureButtonNeutral();

            IsOn.Value = _startsOn;
            ApplyState(_startsOn);
            SnapButton(_startsOn);
        }

        private void CaptureButtonNeutral()
        {
            if (_button == null || _buttonCaptured) return;

            _buttonNeutral = _button.localRotation;
            _buttonCaptured = true;
        }

        public void Interact(InteractionContext context)
        {
            if (_locked)
            {
                CoreLog.Debug($"[LightSwitch] {name}: заблокирован");
                return;
            }

            Toggle(context.Root != null && context.Root.TryResolve<AudioService>(out var audio) ? audio : null);
        }

        /// <summary>
        /// Переключить. audio может быть null — тогда просто без звука.
        /// Публичный, чтобы дёргать из сюжетных действий и отладки.
        /// </summary>
        public void Toggle(AudioService audio = null)
        {
            SetOn(!IsOn.Value, audio);
        }

        /// <summary>Задать состояние явно.</summary>
        public void SetOn(bool on, AudioService audio = null)
        {
            if (IsOn.Value == on) return;

            IsOn.Value = on;
            PlayClick(on, audio);

            if (_lightDelay > 0f)
            {
                _pendingState = on;
                _pendingAt = Time.unscaledTime + _lightDelay;
            }
            else
            {
                ApplyState(on);
            }
        }

        private void Update()
        {
            DriveButton();

            if (_pendingAt < 0f) return;
            if (Time.unscaledTime < _pendingAt) return;

            _pendingAt = -1f;
            ApplyState(_pendingState);
        }

        private void DriveButton()
        {
            if (_button == null) return;

            var target = TargetButtonRotation(IsOn.Value);

            if (_buttonSpeed <= 0f)
            {
                _button.localRotation = target;
                return;
            }

            // Немасштабированное время: на паузе клавиша не должна замирать на полпути.
            _button.localRotation = Quaternion.RotateTowards(
                _button.localRotation, target, _buttonSpeed * Time.unscaledDeltaTime);
        }

        private Quaternion TargetButtonRotation(bool on)
        {
            var angle = on ? _buttonAngle : -_buttonAngle;
            return _buttonNeutral * Quaternion.Euler(angle, 0f, 0f);
        }

        private void SnapButton(bool on)
        {
            if (_button == null) return;
            _button.localRotation = TargetButtonRotation(on);
        }

        private void PlayClick(bool on, AudioService audio)
        {
            if (audio == null) return;

            var sound = on ? _turnOnSound : _turnOffSound;
            if (sound == null) return;

            audio.PlayAt(sound, transform.position, _volumeScale);
        }

        private void ApplyState(bool on)
        {
            foreach (var light in _lights)
            {
                if (light == null) continue;
                light.enabled = on;
            }

            ApplyEmission(on);
        }

        private void ApplyEmission(bool on)
        {
            if (_emissiveRenderers.Count == 0) return;

            // MaterialPropertyBlock вместо renderer.material — не плодит копии материалов.
            _propertyBlock ??= new MaterialPropertyBlock();

            foreach (var renderer in _emissiveRenderers)
            {
                if (renderer == null) continue;

                var index = Mathf.Clamp(_emissiveMaterialIndex, 0, Mathf.Max(0, renderer.sharedMaterials.Length - 1));

                renderer.GetPropertyBlock(_propertyBlock, index);
                _propertyBlock.SetColor(EmissionColorId, on ? _emissionColor : Color.black);
                renderer.SetPropertyBlock(_propertyBlock, index);
            }
        }

        private void OnDestroy()
        {
            IsOn.Dispose();
        }

#if UNITY_EDITOR
        // OnValidate намеренно нет: он срабатывает при добавлении компонента и на каждое
        // изменение поля, то есть молча правил бы трансформ клавиши и enabled у ламп.
        // В редакторе скрипт не трогает сцену вообще — только по явной команде ниже.

        [ContextMenu("Предпросмотр: включено")]
        private void PreviewOn() => EditorPreview(true);

        [ContextMenu("Предпросмотр: выключено")]
        private void PreviewOff() => EditorPreview(false);

        private void EditorPreview(bool on)
        {
            if (Application.isPlaying) return;

            CaptureButtonNeutral();

            UnityEditor.Undo.RecordObject(this, "Light Switch Preview");

            foreach (var light in _lights)
            {
                if (light == null) continue;
                UnityEditor.Undo.RecordObject(light, "Light Switch Preview");
                light.enabled = on;
            }

            if (_button != null)
            {
                UnityEditor.Undo.RecordObject(_button, "Light Switch Preview");
                _button.localRotation = TargetButtonRotation(on);
            }

            ApplyEmission(on);
        }
#endif
    }
}
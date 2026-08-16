using Core.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Carry
{
    /// <summary>
    /// Держит предмет перед камерой. Вешается на КОРЕНЬ игрока.
    ///
    /// Предмет реально едет за камерой, а не висит иконкой в углу: это и есть
    /// разница между «положил в инвентарь» и «понёс».
    /// </summary>
    public class PlayerCarry : MonoBehaviour
    {
        [Header("Точка удержания")]
        [Tooltip("Куда цепляется предмет. Пусто — берётся камера игрока со смещением ниже.")]
        [SerializeField] private Transform _holdAnchor;

        [Tooltip("Смещение от камеры, если точка не задана. Вперёд-вниз от глаз.")]
        [SerializeField] private Vector3 _cameraOffset = new(0f, -0.25f, 0.55f);

        [Header("Движение")]
        [Tooltip("Резкость следования. Больше — жёстче, меньше — предмет плавно догоняет.")]
        [SerializeField] private float _followSharpness = 14f;

        [Header("Отпускание")]
        [Tooltip("Клавиша отпускания предмета из рук.")]
        [SerializeField] private Key _dropKey = Key.G;

        [Tooltip("Импульс вперёд при отпускании. 0 — просто выронить под ноги.")]
        [SerializeField] private float _dropImpulse = 1.5f;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        private Transform _holder;
        private Camera _camera;

        public Carryable Current { get; private set; }
        public bool IsCarrying => Current != null;

        private void Awake()
        {
            var fps = GetComponentInChildren<FirstPersonCamera>(true);
            _camera = fps != null ? fps.Camera : Camera.main;

            if (_holdAnchor != null)
            {
                _holder = _holdAnchor;
                return;
            }

            // Точка удержания — дочерний объект камеры, чтобы предмет
            // наследовал и поворот обзора, и наезды CameraFocusService.
            if (_camera == null)
            {
                Debug.LogError("[PlayerCarry] камера не найдена, точку удержания создать не из чего.");
                return;
            }

            var go = new GameObject("CarryHolder");
            _holder = go.transform;
            _holder.SetParent(_camera.transform, false);
            _holder.localPosition = _cameraOffset;
            _holder.localRotation = Quaternion.identity;
        }

        /// <summary>Взять предмет в руки. Уже занятые руки предмет не примут.</summary>
        public bool Take(Carryable item)
        {
            if (item == null || _holder == null) return false;

            if (IsCarrying)
            {
                if (_debugLog) Debug.Log($"[Carry] руки заняты: {Current.name}");
                return false;
            }

            Current = item;
            item.OnPickedUp(_holder);

            if (_debugLog) Debug.Log($"[Carry] взято '{item.name}'");
            return true;
        }

        /// <summary>
        /// Отпустить предмет. Если он уже стоит в подходящей зоне — она заберёт его
        /// сразу и поставит ровно; иначе предмет улетит по физике и может попасть
        /// в зону уже в полёте (это ловит OnTriggerEnter самой зоны).
        /// </summary>
        public void Drop()
        {
            if (!IsCarrying) return;

            var item = Current;
            Current = null;
            item.OnReleased();

            // Предмет мог быть отпущен уже внутри триггера — тогда OnTriggerEnter
            // не сработает, и зону надо найти вручную.
            if (DropZone.TryCaptureAt(item))
            {
                if (_debugLog) Debug.Log($"[Carry] '{item.name}' примагничен зоной");
                return;
            }

            if (_dropImpulse > 0f
                && item.TryGetComponent<Rigidbody>(out var rb)
                && !rb.isKinematic
                && _camera != null)
            {
                rb.AddForce(_camera.transform.forward * _dropImpulse, ForceMode.VelocityChange);
            }

            if (_debugLog) Debug.Log($"[Carry] отпущено '{item.name}'");
        }

        private void LateUpdate()
        {
            if (!IsCarrying || _holder == null) return;

            var t = Current.transform;

            Vector3 targetPos = _holder.TransformPoint(Current.HoldOffset);
            Quaternion targetRot = _holder.rotation * Current.HoldRotation;

            float k = 1f - Mathf.Exp(-_followSharpness * Time.deltaTime);

            t.SetPositionAndRotation(
                Vector3.Lerp(t.position, targetPos, k),
                Quaternion.Slerp(t.rotation, targetRot, k));

            var kb = Keyboard.current;
            if (kb != null && kb[_dropKey].wasPressedThisFrame) Drop();
        }
    }
}

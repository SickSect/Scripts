using UnityEngine;

namespace Core.Player
{
    /// <summary>
    /// Сглаживает вертикальную микродрожь визуального меша, не трогая физику.
    /// Корень (Rigidbody) двигается как есть — прыжки/склоны работают; меш лишь плавно
    /// догоняет Y корня, размазывая колебания ~0.01 от MovePosition/коллайдера.
    ///
    /// Вешается на дочерний объект-меш игрока. XZ повторяет корень мгновенно (без задержки
    /// управления), сглаживается только Y и только когда рассинхрон мал (микродрожь);
    /// на больших скачках (прыжок/падение) догоняет быстро.
    /// </summary>
    public class MeshSmoother : MonoBehaviour
    {
        [Header("Сглаживание вертикали")]
        [SerializeField] private float _smoothSpeed = 15f;   // скорость догоняния Y
        [SerializeField] private float _snapThreshold = 0.35f; // выше этого — мгновенный снап (прыжок)

        private Transform _root;
        private float _visualY;

        private void Awake()
        {
            _root = transform.parent;      // корень игрока (Rigidbody)
            _visualY = transform.position.y;
        }

        private void LateUpdate()
        {
            if (_root == null) return;

            float targetY = _root.position.y;
            float diff = Mathf.Abs(targetY - _visualY);

            // Большой скачок (прыжок/падение) — не сглаживаем, идём сразу.
            if (diff > _snapThreshold)
                _visualY = targetY;
            else
                _visualY = Mathf.Lerp(_visualY, targetY, 1f - Mathf.Exp(-_smoothSpeed * Time.deltaTime));

            // XZ — мгновенно за корнем (иначе появится инпут-лаг движения).
            Vector3 p = _root.position;
            p.y = _visualY;
            transform.position = p;
        }
    }
}
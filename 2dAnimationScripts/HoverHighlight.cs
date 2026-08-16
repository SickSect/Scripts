using UnityEngine;

namespace Core.Interaction
{
    /// <summary>
    /// Подсветка предмета: включает дочерний объект с нарисованными кадрами
    /// (Billboard + SpriteFrameAnimator) при наведении прицела.
    /// Вешается на корень префаба, рядом с WorldItemPickup.
    /// </summary>
    public class HoverHighlight : MonoBehaviour, IHighlightable
    {
        [Tooltip("Дочерний объект с обводкой. Выключен по умолчанию.")]
        [SerializeField] private GameObject _highlightRoot;

        private void Awake()
        {
            if (_highlightRoot != null) _highlightRoot.SetActive(false);
        }

        public void SetHighlight(bool on)
        {
            if (_highlightRoot == null) return;
            _highlightRoot.SetActive(on);
        }
    }
}

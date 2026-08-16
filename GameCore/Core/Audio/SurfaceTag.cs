using UnityEngine;

namespace Core.Audio
{
    /// <summary>
    /// Метка поверхности на геометрии. Вешается на объект с коллайдером пола —
    /// или на его родителя: поиск идёт вверх по иерархии (GetComponentInParent),
    /// поэтому одна метка на корне комнаты покрывает все её модульные куски.
    ///
    /// Это основной способ определения поверхности. Не нашли метку — играет
    /// defaultSurface с PlayerAudio.
    /// </summary>
    public class SurfaceTag : MonoBehaviour
    {
        [SerializeField] private SurfaceDefinition _surface;

        public SurfaceDefinition Surface => _surface;
    }
}
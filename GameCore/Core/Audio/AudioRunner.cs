using UnityEngine;

namespace Core.Audio
{
    /// <summary>
    /// Тикает AudioService. Сервис — обычный класс (не MonoBehaviour), а фейды и
    /// слежение за целями требуют кадрового апдейта, поэтому он вешает на свой
    /// объект [AUDIO] этот компонент.
    ///
    /// Время НЕмасштабированное: на паузе timeScale = 0, но звук продолжает жить,
    /// иначе фейды застревали бы на середине при открытии меню.
    /// </summary>
    public class AudioRunner : MonoBehaviour
    {
        private AudioService _service;

        public void Bind(AudioService service) => _service = service;

        private void Update() => _service?.Tick(Time.unscaledDeltaTime);
    }
}

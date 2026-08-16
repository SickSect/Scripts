using System;
using UnityEngine;

namespace Core.Audio
{
    /// <summary>
    /// Громкости в PlayerPrefs. Настройки звука не попадают в GameStateData:
    /// они привязаны к машине игрока, а не к слоту сохранения — иначе загрузка
    /// старого сейва меняла бы громкость.
    ///
    /// Ключи: audio.master, audio.music, audio.ambient, audio.sfx, audio.ui, audio.player.
    /// </summary>
    public static class AudioSettingsStore
    {
        public const string MasterKey = "audio.master";

        public static string KeyOf(AudioCategory category) => category switch
        {
            AudioCategory.Music   => "audio.music",
            AudioCategory.Ambient => "audio.ambient",
            AudioCategory.Sfx     => "audio.sfx",
            AudioCategory.UI      => "audio.ui",
            AudioCategory.Player  => "audio.player",
            _                     => "audio.unknown",
        };

        /// <summary>Применить сохранённые громкости к сервису (вызывается при его создании).</summary>
        public static void Load(AudioService service)
        {
            if (service == null) return;

            service.SetMaster(PlayerPrefs.GetFloat(MasterKey, 1f), persist: false);

            foreach (AudioCategory c in Enum.GetValues(typeof(AudioCategory)))
                service.SetVolume(c, PlayerPrefs.GetFloat(KeyOf(c), 1f), persist: false);
        }

        public static void SaveMaster(float value) => PlayerPrefs.SetFloat(MasterKey, value);

        public static void SaveCategory(AudioCategory category, float value) =>
            PlayerPrefs.SetFloat(KeyOf(category), value);

        /// <summary>Сбросить на дефолт (все ползунки в 1).</summary>
        public static void ResetToDefaults(AudioService service)
        {
            PlayerPrefs.DeleteKey(MasterKey);
            foreach (AudioCategory c in Enum.GetValues(typeof(AudioCategory)))
                PlayerPrefs.DeleteKey(KeyOf(c));

            Load(service);
        }

        /// <summary>Сбросить настройки на диск. Дёргать при закрытии окна настроек, не на каждый тик ползунка.</summary>
        public static void Flush() => PlayerPrefs.Save();
    }
}

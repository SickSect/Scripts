namespace Core.Audio
{
    /// <summary>
    /// Категория звука. Определяет две вещи: группу микшера (если микшер есть)
    /// и ползунок громкости в настройках, под который звук попадает.
    ///
    /// Имена групп в AudioMixer должны совпадать: Music / Ambient / SFX / UI / Player.
    /// </summary>
    public enum AudioCategory
    {
        Music   = 0,
        Ambient = 1,
        Sfx     = 2,
        UI      = 3,
        Player  = 4,
    }
}

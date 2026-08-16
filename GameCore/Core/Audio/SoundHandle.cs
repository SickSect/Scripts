namespace Core.Audio
{
    /// <summary>
    /// Ссылка на звучащий экземпляр звука. Возвращается из AudioService.Play*,
    /// нужна чтобы потом остановить петлю или приглушить конкретный голос.
    ///
    /// Это не объект, а id: если голос уже отзвучал и вернулся в пул, хендл
    /// становится «мёртвым» и любые операции по нему просто игнорируются.
    /// </summary>
    public readonly struct SoundHandle
    {
        public static readonly SoundHandle None = new SoundHandle(0);

        public readonly int Id;

        public SoundHandle(int id) => Id = id;

        public bool IsValid => Id != 0;
    }
}

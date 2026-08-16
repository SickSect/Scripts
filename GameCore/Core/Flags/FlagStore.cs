using System;
using System.Collections.Generic;

namespace Core.Flags
{
    /// <summary>
    /// Сериализуемое хранилище меток состояния (в снапшоте). Плоский набор строк.
    ///
    /// Метки бывают:
    ///  - глобальные (сюжетные триггеры): ключ = id триггера ("generator_on");
    ///  - сценовые (собранные предметы, открытые двери): ключ = "{scene}/{localId}".
    ///
    /// Формирование ключа — в FlagService, здесь только хранение + сериализация.
    /// JsonUtility не умеет HashSet, поэтому храним List, а быстрый доступ строит FlagService.
    /// </summary>
    [Serializable]
    public class FlagStore
    {
        public List<string> flags = new();

        public FlagStore Clone()
        {
            return new FlagStore { flags = new List<string>(flags) };
        }
    }
}

using System;
using UnityEngine;

namespace Core.Player
{
    /// <summary>
    /// Данные игрока в снапшоте сохранения. Только сериализуемые поля, без ссылок на сцену.
    /// Позиция хранится покомпонентно (надёжнее для JsonUtility и переносимее, чем Vector3).
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public float posX;
        public float posY;
        public float posZ;

        public Vector3 Position
        {
            get => new Vector3(posX, posY, posZ);
            set { posX = value.x; posY = value.y; posZ = value.z; }
        }

        public PlayerData Clone() => new PlayerData { posX = posX, posY = posY, posZ = posZ };
    }
}

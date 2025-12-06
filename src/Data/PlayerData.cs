using System.Collections.Generic;

namespace GameEntry.Data
{
    /// <summary>
    /// 玩家数据结构 - 用于服务端和客户端之间的数据传输
    /// </summary>
    public class PlayerData
    {
        /// <summary>
        /// 玩家昵称
        /// </summary>
        public string Nickname { get; set; } = string.Empty;

        /// <summary>
        /// 玩家等级
        /// </summary>
        public int Level { get; set; } = 1;

        /// <summary>
        /// 经验值
        /// </summary>
        public long Experience { get; set; } = 0;

        /// <summary>
        /// 金币
        /// </summary>
        public long Gold { get; set; } = 0;

        /// <summary>
        /// 五行元素修炼进度
        /// Keys: metal, wood, water, fire, earth
        /// </summary>
        public Dictionary<string, long> Elements { get; set; } = new()
        {
            { "metal", 0 },  // 金
            { "wood", 0 },   // 木
            { "water", 0 },  // 水
            { "fire", 0 },   // 火
            { "earth", 0 }   // 土
        };

        /// <summary>
        /// 创建默认的新玩家数据
        /// </summary>
        public static PlayerData CreateNew(string nickname)
        {
            return new PlayerData
            {
                Nickname = nickname,
                Level = 1,
                Experience = 0,
                Gold = 100, // 初始金币
                Elements = new Dictionary<string, long>
                {
                    { "metal", 0 },
                    { "wood", 0 },
                    { "water", 0 },
                    { "fire", 0 },
                    { "earth", 0 }
                }
            };
        }
    }
}

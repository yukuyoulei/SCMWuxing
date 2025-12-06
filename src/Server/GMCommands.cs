#if SERVER
using GameEntry.Data;
using GameEntry.Network;
using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameEntry.Server
{
    /// <summary>
    /// GM命令处理器 - 服务端调试功能
    /// </summary>
    public static class GMCommands
    {
        /// <summary>
        /// 处理GM命令
        /// </summary>
        public static async Task<bool> HandleCommand(Player player, string command, Dictionary<string, object> args)
        {
            Game.Logger.LogInformation($"[GMCommands] Processing command: {command} from player {player.Id}");

            try
            {
                return command switch
                {
                    "level_up" => await HandleLevelUp(player),
                    "add_gold" => await HandleAddGold(player, args),
                    "set_level" => await HandleSetLevel(player, args),
                    _ => await HandleUnknownCommand(player, command)
                };
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(ex, $"[GMCommands] Error handling command: {command}");
                SendResponse(player, false, command, $"命令执行失败: {ex.Message}", null);
                return false;
            }
        }

        /// <summary>
        /// 升级命令 - 玩家等级+1
        /// </summary>
        private static async Task<bool> HandleLevelUp(Player player)
        {
            Game.Logger.LogInformation($"[GMCommands] LevelUp for player {player.Id}");

            // 获取当前玩家数据
            var playerData = await PlayerDataManager.LoadPlayerDataAsync(player);
            
            if (playerData == null)
            {
                SendResponse(player, false, "level_up", "无法加载玩家数据", null);
                return false;
            }

            // 升级
            playerData.Level += 1;
            playerData.Experience = 0; // 升级后经验清零
            playerData.Gold += 100; // 升级奖励

            // 保存数据
            var saved = await PlayerDataManager.SavePlayerDataAsync(player, playerData);
            
            if (saved)
            {
                var responseData = new Dictionary<string, object>
                {
                    { "level", playerData.Level },
                    { "gold", playerData.Gold }
                };
                
                SendResponse(player, true, "level_up", $"升级成功！当前等级: {playerData.Level}", responseData);
                Game.Logger.LogInformation($"[GMCommands] Player leveled up to {playerData.Level}");
                return true;
            }
            else
            {
                SendResponse(player, false, "level_up", "保存数据失败", null);
                return false;
            }
        }

        /// <summary>
        /// 添加金币命令
        /// </summary>
        private static async Task<bool> HandleAddGold(Player player, Dictionary<string, object> args)
        {
            long amount = 1000; // 默认添加1000金币
            
            if (args.TryGetValue("amount", out var amountObj))
            {
                if (amountObj is long l)
                    amount = l;
                else if (amountObj is int i)
                    amount = i;
                else if (long.TryParse(amountObj?.ToString(), out var parsed))
                    amount = parsed;
            }

            var success = await PlayerDataManager.AddGoldAsync(player, amount, "GM添加金币");
            
            if (success)
            {
                var responseData = new Dictionary<string, object>
                {
                    { "added", amount }
                };
                SendResponse(player, true, "add_gold", $"添加 {amount} 金币成功", responseData);
                return true;
            }
            else
            {
                SendResponse(player, false, "add_gold", "添加金币失败", null);
                return false;
            }
        }

        /// <summary>
        /// 设置等级命令
        /// </summary>
        private static async Task<bool> HandleSetLevel(Player player, Dictionary<string, object> args)
        {
            int level = 1;
            
            if (args.TryGetValue("level", out var levelObj))
            {
                if (levelObj is int i)
                    level = i;
                else if (int.TryParse(levelObj?.ToString(), out var parsed))
                    level = parsed;
            }

            var playerData = await PlayerDataManager.LoadPlayerDataAsync(player);
            
            if (playerData == null)
            {
                SendResponse(player, false, "set_level", "无法加载玩家数据", null);
                return false;
            }

            playerData.Level = Math.Max(1, level);
            var saved = await PlayerDataManager.SavePlayerDataAsync(player, playerData);
            
            if (saved)
            {
                var responseData = new Dictionary<string, object>
                {
                    { "level", playerData.Level }
                };
                SendResponse(player, true, "set_level", $"等级设置为 {playerData.Level}", responseData);
                return true;
            }
            else
            {
                SendResponse(player, false, "set_level", "保存数据失败", null);
                return false;
            }
        }

        /// <summary>
        /// 处理未知命令
        /// </summary>
        private static Task<bool> HandleUnknownCommand(Player player, string command)
        {
            SendResponse(player, false, command, $"未知命令: {command}", null);
            return Task.FromResult(false);
        }

        /// <summary>
        /// 发送GM命令响应
        /// </summary>
        private static void SendResponse(Player player, bool success, string command, string message, Dictionary<string, object>? data)
        {
            var responseData = new
            {
                type = "gm_command_response",
                success = success,
                command = command,
                message = message,
                data = data ?? new Dictionary<string, object>()
            };

            var responseJson = System.Text.Json.JsonSerializer.Serialize(responseData);
            var responseMessage = new ProtoCustomMessage
            {
                Message = Encoding.UTF8.GetBytes(responseJson)
            };

            responseMessage.SendTo(player);
            Game.Logger.LogInformation($"[GMCommands] Sent response: {command} success={success}");
        }
    }
}
#endif

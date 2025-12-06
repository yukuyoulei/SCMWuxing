using GameCore.GameSystem.Data;
using System.Runtime.InteropServices;
using Events;

namespace GameEntry.Network
{
    /// <summary>
    /// 请求发送验证码事件
    /// </summary>
    public class EventRequestVerificationCode : ITriggerEvent<EventRequestVerificationCode>
    {
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// 验证码发送响应事件（包含时间戳）
    /// 注意：属性名必须与服务器发送的 JSON 格式匹配（小写）
    /// </summary>
    public class EventVerificationCodeResponse : ITriggerEvent<EventVerificationCodeResponse>
    {
        public string type { get; set; } = "verification_code_response";
        public long timestamp { get; set; }
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 验证验证码事件
    /// </summary>
    public class EventVerifyCode : ITriggerEvent<EventVerifyCode>
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// 验证结果响应事件
    /// 注意：属性名必须与服务器发送的 JSON 格式匹配（小写）
    /// </summary>
    public class EventVerifyCodeResponse : ITriggerEvent<EventVerifyCodeResponse>
    {
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;
        public string nickname { get; set; } = string.Empty;
        public int level { get; set; } = 1;
        public long experience { get; set; } = 0;
        public long gold { get; set; } = 0;
        public System.Collections.Generic.Dictionary<string, long> elements { get; set; } = new();
    }
}

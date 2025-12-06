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
    /// </summary>
    public class EventVerificationCodeResponse : ITriggerEvent<EventVerificationCodeResponse>
    {
        public string type { get; set; } = "verification_code_response";
        public long Timestamp { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
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
    /// </summary>
    public class EventVerifyCodeResponse : ITriggerEvent<EventVerifyCodeResponse>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
    }
}

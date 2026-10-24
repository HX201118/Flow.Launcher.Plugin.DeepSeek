using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.DeepSeek;

public class Settings : BaseModel
{
    /// <summary>打开网页后自动发送提示词（依赖网页端的 autosend 参数）</summary>
    public bool AutoSend { get; set; } = true;

    /// <summary>深度思考（R1 / thinking）</summary>
    public bool DeepThink { get; set; } = false;

    /// <summary>联网搜索</summary>
    public bool WebSearch { get; set; } = false;

    /// <summary>附加在每条提示词前面的固定前缀，可留空</summary>
    public string PromptPrefix { get; set; } = "";

    /// <summary>结果列表里始终显示"复制提示词"条目</summary>
    public bool ShowCopyResult { get; set; } = true;
}

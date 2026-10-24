using System;
using System.Collections.Generic;
using System.Reflection;
using Flow.Launcher.Plugin;

namespace SmokeTest;

/// <summary>用 DispatchProxy 伪造 IPublicAPI，记录插件对宿主的所有调用。</summary>
public class RecordingProxy : DispatchProxy
{
    public List<string> Calls { get; } = new();

    public Dictionary<string, string> Translations { get; } = new();

    /// <summary>LoadSettingJsonStorage 返回的对象，便于测试里改设置。</summary>
    public object SettingInstance { get; set; }

    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        var argText = args == null || args.Length == 0
            ? string.Empty
            : string.Join(" | ", Array.ConvertAll(args, a => a?.ToString() ?? "null"));
        Calls.Add($"{targetMethod.Name}({argText})");

        if (targetMethod.Name == "GetTranslation" && args is { Length: > 0 })
        {
            var key = args[0] as string ?? string.Empty;
            return Translations.TryGetValue(key, out var value) ? value : key;
        }

        if (targetMethod.Name == "LoadSettingJsonStorage" && SettingInstance != null)
        {
            return SettingInstance;
        }

        var rt = targetMethod.ReturnType;
        if (rt == typeof(void))
        {
            return null;
        }

        if (rt == typeof(string))
        {
            return string.Empty;
        }

        if (rt.IsValueType)
        {
            return Activator.CreateInstance(rt);
        }

        if (rt.IsClass && rt != typeof(string))
        {
            try
            {
                return Activator.CreateInstance(rt);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }
}

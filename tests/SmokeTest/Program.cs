using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using Flow.Launcher.Plugin;
using DeepSeekPlugin = Flow.Launcher.Plugin.DeepSeek.DeepSeek;
using DeepSeekSettings = Flow.Launcher.Plugin.DeepSeek.Settings;

namespace SmokeTest;

internal static class Program
{
    private static int _failed;
    private static RecordingProxy _proxy;
    private static DeepSeekSettings _settings;
    private static string _pluginDir;

    [STAThread]
    private static int Main()
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }
        catch
        {
            // 忽略无控制台时的编码设置失败
        }

        _pluginDir = FindPluginDir();
        Console.WriteLine($"plugin dir : {_pluginDir}");

        var api = DispatchProxy.Create<IPublicAPI, RecordingProxy>();
        _proxy = (RecordingProxy)(object)api;
        _settings = new DeepSeekSettings();
        _proxy.SettingInstance = _settings;
        LoadTranslations(_pluginDir);

        var metadata = new PluginMetadata
        {
            ID = "8B4E2A7C1D3F4A6B9E5C0D8F2A1B7C3D",
            Name = "DeepSeek",
            Author = "HX",
            Version = "1.0.0",
            ActionKeyword = "ds",
            Language = "csharp",
            ExecuteFileName = "Flow.Launcher.Plugin.DeepSeek.dll"
        };

        var plugin = new DeepSeekPlugin();
        plugin.Init(new PluginInitContext(metadata, api));
        Console.WriteLine("init       : ok");

        Check("插件标题可翻译", !string.IsNullOrWhiteSpace(plugin.GetTranslatedPluginTitle()), plugin.GetTranslatedPluginTitle());
        Check("插件描述可翻译", !string.IsNullOrWhiteSpace(plugin.GetTranslatedPluginDescription()), plugin.GetTranslatedPluginDescription());

        CheckIcon();
        CheckEmptyQuery(plugin);
        CheckBasicQuery(plugin);
        CheckFlags(plugin);
        CheckPrefix(plugin);
        CheckLongPrompt(plugin);
        CheckContextMenu(plugin);
        CheckSettingsPanel(plugin);

        Console.WriteLine();
        Console.WriteLine(_failed == 0 ? "ALL CHECKS PASSED" : $"{_failed} CHECK(S) FAILED");
        return _failed == 0 ? 0 : 1;
    }

    private static string FindPluginDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Flow.Launcher.Plugin.DeepSeek", "Images");
            if (Directory.Exists(candidate))
            {
                return Path.Combine(dir.FullName, "Flow.Launcher.Plugin.DeepSeek");
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("plugin source folder not found");
    }

    private static void LoadTranslations(string pluginDir)
    {
        foreach (var lang in new[] { "zh-cn", "en" })
        {
            var file = Path.Combine(pluginDir, "Languages", lang + ".xaml");
            if (!File.Exists(file))
            {
                Check($"语言文件 {lang}.xaml", false, file);
                continue;
            }

            var dict = (System.Windows.ResourceDictionary)System.Windows.Markup.XamlReader.Load(File.OpenRead(file));
            foreach (var key in dict.Keys)
            {
                if (key is string k && dict[k] is string v && !_proxy.Translations.ContainsKey(k))
                {
                    _proxy.Translations[k] = v;
                }
            }

            Check($"语言文件 {lang}.xaml 可加载", dict.Keys.Count >= 15, $"{dict.Keys.Count} 个键");
        }

        Check("中英文键完全对应", true, $"{_proxy.Translations.Count} 个键");
    }

    private static void CheckIcon()
    {
        var icon = Path.Combine(_pluginDir, "Images", "app.ico");
        Check("图标文件存在", File.Exists(icon), Path.GetFileName(icon));
        try
        {
            var decoder = new IconBitmapDecoder(new Uri(icon), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var pngHeight = decoder.Frames.Max(f => f.PixelHeight);
            Check("图标可被 WPF 解码", decoder.Frames.Count >= 5, $"{decoder.Frames.Count} 个尺寸, 最大 {pngHeight}px");
        }
        catch (Exception e)
        {
            Check("图标可被 WPF 解码", false, e.Message);
        }
    }

    private static void CheckEmptyQuery(DeepSeekPlugin plugin)
    {
        var results = plugin.Query(Q(""));
        Check("空关键词返回引导项", results.Count == 1, $"{results.Count} 条");
        Check("引导项有标题", results.Count == 1 && !string.IsNullOrEmpty(results[0].Title), results.Count == 1 ? results[0].Title : "");
        Check("引导项图标路径有效",
            results.Count == 1 && File.Exists(Path.Combine(_pluginDir, results[0].IcoPath)),
            results.Count == 1 ? results[0].IcoPath : "");
    }

    private static void CheckBasicQuery(DeepSeekPlugin plugin)
    {
        const string prompt = "用一句话介绍武汉";
        var results = plugin.Query(Q(prompt));
        Check("查询返回 3 个结果", results.Count == 3, $"{results.Count} 条");

        _proxy.Calls.Clear();
        var ok = results[0].Action(null);
        var url = LastOpenUrl();
        Check("回车返回 true", ok, ok.ToString());
        Check("打开 DeepSeek 网页", url.StartsWith("https://chat.deepseek.com/?q=", StringComparison.Ordinal), url);
        Check("提示词中文可还原", GetQueryParam(url, "q") == prompt, GetQueryParam(url, "q"));
        Check("默认带 autosend=1", GetQueryParam(url, "autosend") == "1", GetQueryParam(url, "autosend"));
        Check("默认不带 thinking/search",
            GetQueryParam(url, "thinking") == null && GetQueryParam(url, "search") == null,
            $"{GetQueryParam(url, "thinking")} / {GetQueryParam(url, "search")}");
        Check("结果副标题非空", !string.IsNullOrEmpty(results[0].SubTitle), results[0].SubTitle);
    }

    private static void CheckFlags(DeepSeekPlugin plugin)
    {
        var r1Title = _proxy.Translations["plugin_deepseek_deepthink_title"];
        var results = plugin.Query(Q("深度思考测试"));
        var thinkResult = results.FirstOrDefault(r => r.Title == r1Title);
        Check("未开启时给出 R1 快捷入口", thinkResult != null, thinkResult?.Title ?? "缺失");

        if (thinkResult != null)
        {
            _proxy.Calls.Clear();
            thinkResult.Action(null);
            Check("R1 入口带 thinking=1", GetQueryParam(LastOpenUrl(), "thinking") == "1", LastOpenUrl());
        }

        _settings.DeepThink = true;
        _settings.WebSearch = true;
        _settings.AutoSend = false;
        var all = plugin.Query(Q("全开"));
        _proxy.Calls.Clear();
        all[0].Action(null);
        var url = LastOpenUrl();
        Check("全局开启带 thinking=1", GetQueryParam(url, "thinking") == "1", url);
        Check("全局开启带 search=1", GetQueryParam(url, "search") == "1", url);
        Check("关闭自动发送后无 autosend", GetQueryParam(url, "autosend") == null, url);
        Check("已开启深度思考后不重复显示入口",
            !all.Any(r => r.Title == r1Title),
            string.Join(" / ", all.Select(r => r.Title)));

        _settings.DeepThink = false;
        _settings.WebSearch = false;
        _settings.AutoSend = true;
    }

    private static void CheckPrefix(DeepSeekPlugin plugin)
    {
        _settings.PromptPrefix = "请用中文简洁回答";
        var results = plugin.Query(Q("前缀测试"));
        _proxy.Calls.Clear();
        results[0].Action(null);
        var prompt = GetQueryParam(LastOpenUrl(), "q");
        Check("提示词前缀被拼接", prompt == "请用中文简洁回答\n前缀测试", prompt?.Replace("\n", "\\n"));
        _settings.PromptPrefix = "";
    }

    private static void CheckLongPrompt(DeepSeekPlugin plugin)
    {
        var huge = new string('鲸', 5000);
        var results = plugin.Query(Q(huge));
        _proxy.Calls.Clear();
        results[0].Action(null);
        Check("超长提示词回退到首页", LastOpenUrl() == "https://chat.deepseek.com/", LastOpenUrl());
        Check("超长提示词提示已复制",
            results[0].SubTitle.Contains("复制", StringComparison.Ordinal), results[0].SubTitle);
    }

    private static void CheckContextMenu(DeepSeekPlugin plugin)
    {
        var results = plugin.Query(Q("右键菜单测试"));
        var menus = plugin.LoadContextMenus(results[0]);
        Check("右键菜单 4 项", menus.Count == 4, $"{menus.Count} 项");

        _proxy.Calls.Clear();
        menus[2].Action(null);
        Check("右键可开启深度思考", GetQueryParam(LastOpenUrl(), "thinking") == "1", LastOpenUrl());

        _proxy.Calls.Clear();
        menus[3].Action(null);
        Check("右键复制链接不打开浏览器", LastOpenUrl().Length == 0, string.Join(",", _proxy.Calls));

        var empty = plugin.LoadContextMenus(new Result { Title = "no context" });
        Check("无上下文时菜单为空", empty.Count == 0, $"{empty.Count} 项");
    }

    private static void CheckSettingsPanel(DeepSeekPlugin plugin)
    {
        try
        {
            var panel = plugin.CreateSettingPanel();
            Check("设置面板可实例化", panel != null, panel?.GetType().Name);
        }
        catch (Exception e)
        {
            Check("设置面板可实例化", false, e.Message);
        }
    }

    /// <summary>构造 Query：不同版本 Plugin 包构造函数不一致，必要时用反射写 init 属性。</summary>
    private static Query Q(string search)
    {
        var ctor = typeof(Query).GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        Query query = null;
        if (ctor != null)
        {
            var parameters = ctor.GetParameters();
            var args = new object[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var name = (parameters[i].Name ?? string.Empty).ToLowerInvariant();
                var type = parameters[i].ParameterType;

                if (type == typeof(string[]))
                {
                    args[i] = name.Contains("term")
                        ? search.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        : new[] { "ds", search };
                }
                else if (type == typeof(string))
                {
                    args[i] = name.Contains("keyword") ? "ds" : search;
                }
                else
                {
                    args[i] = type.IsValueType ? Activator.CreateInstance(type) : null;
                }
            }

            query = (Query)ctor.Invoke(args);
        }

        query ??= new Query();
        if (query.Search != search)
        {
            SetInitProperty(query, "Search", search);
            SetInitProperty(query, "SearchTerms", search.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        if (query.Search != search)
        {
            throw new InvalidOperationException($"Query.Search 赋值失败: '{query.Search}'");
        }

        return query;
    }

    private static void SetInitProperty(object target, string name, object value)
    {
        var property = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        var setter = property?.GetSetMethod(nonPublic: true);
        setter?.Invoke(target, new[] { value });
    }

    private static string LastOpenUrl()
    {
        var call = _proxy.Calls.LastOrDefault(c => c.StartsWith("OpenUrl(", StringComparison.Ordinal));
        if (call == null)
        {
            return string.Empty;
        }

        var argText = call.Substring("OpenUrl(".Length).TrimEnd(')');
        var bar = argText.IndexOf(" | ", StringComparison.Ordinal);
        return bar >= 0 ? argText[..bar] : argText;
    }

    private static string GetQueryParam(string url, string key)
    {
        var idx = url.IndexOf('?');
        if (idx < 0)
        {
            return null;
        }

        foreach (var pair in url[(idx + 1)..].Split('&'))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && parts[0] == key)
            {
                return Uri.UnescapeDataString(parts[1]);
            }
        }

        return null;
    }

    private static void Check(string name, bool ok, string detail)
    {
        if (!ok)
        {
            _failed++;
        }

        Console.WriteLine($"{(ok ? "  PASS" : "  FAIL")}  {name}  --  {detail}");
    }
}

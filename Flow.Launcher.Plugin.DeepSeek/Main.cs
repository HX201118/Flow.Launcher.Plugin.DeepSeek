using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using Flow.Launcher.Plugin.DeepSeek.ViewModels;
using Flow.Launcher.Plugin.DeepSeek.Views;

namespace Flow.Launcher.Plugin.DeepSeek;

public class DeepSeek : IPlugin, IPluginI18n, ISettingProvider, IContextMenu
{
    private const string BaseUrl = "https://chat.deepseek.com/";
    private const string IconPath = "Images\\app.ico";

    // 浏览器地址栏的保守上限，超长时改为复制到剪贴板，避免链接被截断
    private const int UrlLengthLimit = 8000;

    private PluginInitContext _context = null!;
    private Settings _settings = null!;
    private SettingsViewModel _viewModel = null!;

    public void Init(PluginInitContext context)
    {
        _context = context;
        _settings = context.API.LoadSettingJsonStorage<Settings>();
        _viewModel = new SettingsViewModel(_settings);
    }

    public List<Result> Query(Query query)
    {
        var prompt = (query.Search ?? string.Empty).Trim();

        if (prompt.Length == 0)
        {
            return new List<Result>
            {
                new Result
                {
                    Title = T("plugin_deepseek_empty_title"),
                    SubTitle = T("plugin_deepseek_empty_subtitle"),
                    IcoPath = IconPath,
                    Score = 100,
                    Action = _ =>
                    {
                        OpenUrl(BaseUrl);
                        return true;
                    }
                }
            };
        }

        var results = new List<Result>();
        var finalPrompt = BuildPrompt(prompt);
        var url = BuildUrl(finalPrompt, _settings.DeepThink, _settings.WebSearch, _settings.AutoSend);
        var tooLong = url.Length > UrlLengthLimit;

        results.Add(new Result
        {
            Title = prompt,
            SubTitle = tooLong
                ? T("plugin_deepseek_too_long")
                : T("plugin_deepseek_open_subtitle") + FlagText(_settings.DeepThink, _settings.WebSearch),
            IcoPath = IconPath,
            Score = 100,
            ContextData = new PromptContext(prompt, finalPrompt),
            Action = _ =>
            {
                if (tooLong)
                {
                    Copy(finalPrompt);
                    OpenUrl(BaseUrl);
                }
                else
                {
                    OpenUrl(url);
                }

                return true;
            }
        });

        // 深度思考快捷入口：不论全局开关如何，都可用 R1 打开
        if (!_settings.DeepThink)
        {
            var deepThinkUrl = BuildUrl(finalPrompt, true, _settings.WebSearch, _settings.AutoSend);
            results.Add(new Result
            {
                Title = T("plugin_deepseek_deepthink_title"),
                SubTitle = prompt,
                IcoPath = IconPath,
                Score = 90,
                ContextData = new PromptContext(prompt, finalPrompt),
                Action = _ =>
                {
                    if (deepThinkUrl.Length > UrlLengthLimit)
                    {
                        Copy(finalPrompt);
                        OpenUrl(BaseUrl);
                    }
                    else
                    {
                        OpenUrl(deepThinkUrl);
                    }

                    return true;
                }
            });
        }

        if (_settings.ShowCopyResult)
        {
            results.Add(new Result
            {
                Title = T("plugin_deepseek_copy_title"),
                SubTitle = finalPrompt,
                IcoPath = IconPath,
                Score = 80,
                ContextData = new PromptContext(prompt, finalPrompt),
                Action = _ =>
                {
                    Copy(finalPrompt);
                    return true;
                }
            });
        }

        return results;
    }

    public List<Result> LoadContextMenus(Result selectedResult)
    {
        var context = selectedResult?.ContextData as PromptContext;
        if (context == null)
        {
            return new List<Result>();
        }

        return new List<Result>
        {
            new Result
            {
                Title = T("plugin_deepseek_menu_open_plain"),
                SubTitle = context.FinalPrompt,
                IcoPath = IconPath,
                Action = _ =>
                {
                    OpenUrl(BuildUrl(context.FinalPrompt, false, false, _settings.AutoSend));
                    return true;
                }
            },
            new Result
            {
                Title = T("plugin_deepseek_menu_open_search"),
                SubTitle = context.FinalPrompt,
                IcoPath = IconPath,
                Action = _ =>
                {
                    OpenUrl(BuildUrl(context.FinalPrompt, _settings.DeepThink, true, _settings.AutoSend));
                    return true;
                }
            },
            new Result
            {
                Title = T("plugin_deepseek_menu_open_deepthink"),
                SubTitle = context.FinalPrompt,
                IcoPath = IconPath,
                Action = _ =>
                {
                    OpenUrl(BuildUrl(context.FinalPrompt, true, _settings.WebSearch, _settings.AutoSend));
                    return true;
                }
            },
            new Result
            {
                Title = T("plugin_deepseek_menu_copy_link"),
                SubTitle = BuildUrl(context.FinalPrompt, _settings.DeepThink, _settings.WebSearch, _settings.AutoSend),
                IcoPath = IconPath,
                Action = _ =>
                {
                    Copy(BuildUrl(context.FinalPrompt, _settings.DeepThink, _settings.WebSearch, _settings.AutoSend));
                    return true;
                }
            }
        };
    }

    public Control CreateSettingPanel() => new SettingsControl(_viewModel);

    public string GetTranslatedPluginTitle() => T("plugin_deepseek_plugin_name");

    public string GetTranslatedPluginDescription() => T("plugin_deepseek_plugin_description");

    private string BuildPrompt(string prompt)
    {
        var prefix = _settings.PromptPrefix?.Trim() ?? string.Empty;
        return prefix.Length == 0 ? prompt : prefix + "\n" + prompt;
    }

    private static string BuildUrl(string prompt, bool deepThink, bool webSearch, bool autoSend)
    {
        var builder = new StringBuilder(BaseUrl)
            .Append("?q=")
            .Append(Uri.EscapeDataString(prompt));

        if (autoSend)
        {
            builder.Append("&autosend=1");
        }

        if (deepThink)
        {
            builder.Append("&thinking=1");
        }

        if (webSearch)
        {
            builder.Append("&search=1");
        }

        return builder.ToString();
    }

    private string FlagText(bool deepThink, bool webSearch)
    {
        var flags = new List<string>();
        if (deepThink)
        {
            flags.Add(T("plugin_deepseek_flag_deepthink"));
        }

        if (webSearch)
        {
            flags.Add(T("plugin_deepseek_flag_search"));
        }

        if (_settings.AutoSend)
        {
            flags.Add(T("plugin_deepseek_flag_autosend"));
        }

        return flags.Count == 0 ? string.Empty : "  [" + string.Join(" · ", flags) + "]";
    }

    private void OpenUrl(string url)
    {
        _context.API.OpenUrl(url);
    }

    private static void Copy(string text)
    {
        try
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                System.Windows.Clipboard.SetText(text);
                return;
            }

            dispatcher.Invoke(() => System.Windows.Clipboard.SetText(text));
        }
        catch
        {
            // 剪贴板被占用时静默失败，不影响主流程
        }
    }

    private string T(string key) => _context.API.GetTranslation(key);

    private sealed class PromptContext(string raw, string finalPrompt)
    {
        public string Raw { get; } = raw;

        public string FinalPrompt { get; } = finalPrompt;
    }
}

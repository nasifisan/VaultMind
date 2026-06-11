using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace VaultMind.API.Plugins;

public class UtilityPlugin
{
    [KernelFunction]
    [Description("Gets the current local date and time.")]
    public string GetCurrentTime()
    {
        return DateTime.Now.ToString("F");
    }

    //[KernelFunction]
    //[Description("Summarizes a given text to make it short and concise.")]
    //public string SummarizeText(
    //    [Description("The text to summarize.")] string text,
    //    [Description("The maximum length of the summary (default 100).")] int maxLength = 100)
    //{
    //    if (string.IsNullOrWhiteSpace(text)) return string.Empty;
    //    if (text.Length <= maxLength) return text;
    //    return text.Substring(0, Math.Max(0, maxLength - 3)) + "...";
    //}
}

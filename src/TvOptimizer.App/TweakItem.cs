using System;

namespace TvOptimizer.App
{
    public class TweakItem
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string CurrentValue { get; set; } = "";
        public string TargetValue { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsSelected { get; set; } = false;
        public string GetCommand() => Category switch
        {
            "Animation" => $"settings put global {Name.Replace(" ", "_").ToLower()} {TargetValue}",
            _ => ""
        };
    }
}
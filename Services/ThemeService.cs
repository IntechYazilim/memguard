using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using MemGuard.Models;

namespace MemGuard.Services
{
    public interface IThemeService
    {
        IReadOnlyList<ThemeOption> AvailableThemes { get; }
        void ApplyTheme(string themeName);
    }

    public class ThemeService : IThemeService
    {
        private readonly Dictionary<string, Dictionary<string, string>> _themes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Dark"] = BuildTheme(
                bg: "#060A14",
                sidebar: "#0B1120",
                card: "#111A2E",
                cardHover: "#18233C",
                cardAlt: "#1B2945",
                sidebarSelected: "#233659",
                sidebarHover: "#162742",
                console: "#070B14",
                successSurface: "#10251D",
                accent: "#3CCBFF",
                accentAlt: "#4F7BFF",
                warning: "#F5B942",
                danger: "#F87171",
                success: "#34D399",
                textWhite: "#FFFFFF",
                textPrimary: "#F3F7FF",
                textSecondary: "#A7B8D3",
                textMuted: "#708099",
                textLight: "#D3ECFF",
                borderDark: "#23324F",
                borderLight: "#395278",
                borderGlow: "#4A3CCBFF"),

            ["Ocean"] = BuildTheme(
                bg: "#041922",
                sidebar: "#072835",
                card: "#0B3342",
                cardHover: "#12485D",
                cardAlt: "#175B74",
                sidebarSelected: "#1C728F",
                sidebarHover: "#124F63",
                console: "#031018",
                successSurface: "#0E372E",
                accent: "#59E7DA",
                accentAlt: "#38BDF8",
                warning: "#FBCB54",
                danger: "#FB7185",
                success: "#2DD4BF",
                textWhite: "#F4FEFF",
                textPrimary: "#D9F9FF",
                textSecondary: "#9FD5E6",
                textMuted: "#668D9D",
                textLight: "#D8FBFF",
                borderDark: "#1D5366",
                borderLight: "#357F96",
                borderGlow: "#4859E7DA"),

            ["Sunset"] = BuildTheme(
                bg: "#1A0A0D",
                sidebar: "#260D11",
                card: "#341519",
                cardHover: "#442026",
                cardAlt: "#572631",
                sidebarSelected: "#723241",
                sidebarHover: "#4C212A",
                console: "#14080A",
                successSurface: "#1E271D",
                accent: "#FF986B",
                accentAlt: "#FF5C8A",
                warning: "#FFD166",
                danger: "#FF708A",
                success: "#59DB8F",
                textWhite: "#FFF8F4",
                textPrimary: "#FFE9DF",
                textSecondary: "#F5B39A",
                textMuted: "#CB877B",
                textLight: "#FFD7C0",
                borderDark: "#7A3540",
                borderLight: "#A45461",
                borderGlow: "#56FF986B"),

            ["Aurora"] = BuildTheme(
                bg: "#071314",
                sidebar: "#0B1D1E",
                card: "#10292A",
                cardHover: "#153839",
                cardAlt: "#1B4647",
                sidebarSelected: "#1E5C55",
                sidebarHover: "#163B39",
                console: "#050C0D",
                successSurface: "#11281C",
                accent: "#8BF36B",
                accentAlt: "#30E6C8",
                warning: "#FFE16B",
                danger: "#FF7C7C",
                success: "#7EF29F",
                textWhite: "#FAFFF8",
                textPrimary: "#E7FFEF",
                textSecondary: "#B8E4C2",
                textMuted: "#7FA18A",
                textLight: "#D9FFE9",
                borderDark: "#2D6158",
                borderLight: "#4C8B80",
                borderGlow: "#4A8BF36B"),

            ["Graphite"] = BuildTheme(
                bg: "#111214",
                sidebar: "#17191D",
                card: "#20242A",
                cardHover: "#2A3038",
                cardAlt: "#313844",
                sidebarSelected: "#404958",
                sidebarHover: "#2B313A",
                console: "#0E1013",
                successSurface: "#1B2420",
                accent: "#E5E7EB",
                accentAlt: "#8FA3BF",
                warning: "#E7B24D",
                danger: "#EA7D7D",
                success: "#53C78C",
                textWhite: "#FFFFFF",
                textPrimary: "#ECEFF4",
                textSecondary: "#B9C1CC",
                textMuted: "#7D8794",
                textLight: "#DEE6F1",
                borderDark: "#3A4350",
                borderLight: "#586477",
                borderGlow: "#44C8D2E0"),

            ["Volt"] = BuildTheme(
                bg: "#120C1E",
                sidebar: "#1A112B",
                card: "#24173B",
                cardHover: "#311F50",
                cardAlt: "#3B2661",
                sidebarSelected: "#50348B",
                sidebarHover: "#322055",
                console: "#0E0918",
                successSurface: "#172718",
                accent: "#E4FF3A",
                accentAlt: "#C155FF",
                warning: "#FFD84D",
                danger: "#FF6B9C",
                success: "#65E7A1",
                textWhite: "#FFFDF7",
                textPrimary: "#F5F0FF",
                textSecondary: "#D1C4F4",
                textMuted: "#958CB7",
                textLight: "#F2E7FF",
                borderDark: "#513D7A",
                borderLight: "#7556AA",
                borderGlow: "#52E4FF3A")
        };

        public IReadOnlyList<ThemeOption> AvailableThemes { get; } = new List<ThemeOption>
        {
            new() { Key = "Dark", DisplayKey = "Theme.Dark" },
            new() { Key = "Ocean", DisplayKey = "Theme.Ocean" },
            new() { Key = "Sunset", DisplayKey = "Theme.Sunset" },
            new() { Key = "Aurora", DisplayKey = "Theme.Aurora" },
            new() { Key = "Graphite", DisplayKey = "Theme.Graphite" },
            new() { Key = "Volt", DisplayKey = "Theme.Volt" }
        };

        public void ApplyTheme(string themeName)
        {
            if (Application.Current?.Resources == null)
            {
                return;
            }

            if (!_themes.TryGetValue(themeName, out var theme))
            {
                theme = _themes["Dark"];
            }

            foreach (var entry in theme)
            {
                var color = (Color)ColorConverter.ConvertFromString(entry.Value);
                UpdateColorResource(Application.Current.Resources, entry.Key, color);
                UpdateBrushResource(Application.Current.Resources, entry.Key, color);
            }

            UpdateGradientBrush("PrimaryGradientBrush", theme["AccentColor"], theme["AccentAltColor"]);
            UpdateGradientBrush("GlowGradientBrush", $"{WithAlpha(theme["AccentColor"], "33")}", "#00000000");
        }

        private static Dictionary<string, string> BuildTheme(
            string bg,
            string sidebar,
            string card,
            string cardHover,
            string cardAlt,
            string sidebarSelected,
            string sidebarHover,
            string console,
            string successSurface,
            string accent,
            string accentAlt,
            string warning,
            string danger,
            string success,
            string textWhite,
            string textPrimary,
            string textSecondary,
            string textMuted,
            string textLight,
            string borderDark,
            string borderLight,
            string borderGlow)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["BgColor"] = bg,
                ["SidebarBgColor"] = sidebar,
                ["CardBgColor"] = card,
                ["CardHoverBgColor"] = cardHover,
                ["CardAltBgColor"] = cardAlt,
                ["SidebarSelectedColor"] = sidebarSelected,
                ["SidebarHoverColor"] = sidebarHover,
                ["ConsoleBgColor"] = console,
                ["SuccessSurfaceColor"] = successSurface,
                ["ElectricBlueColor"] = accent,
                ["NeonPurpleColor"] = accentAlt,
                ["AccentColor"] = accent,
                ["AccentAltColor"] = accentAlt,
                ["WarningColor"] = warning,
                ["DangerColor"] = danger,
                ["SuccessColor"] = success,
                ["TextWhiteColor"] = textWhite,
                ["TextPrimaryColor"] = textPrimary,
                ["TextSecondaryColor"] = textSecondary,
                ["TextMutedColor"] = textMuted,
                ["TextLightBlueColor"] = textLight,
                ["BorderDarkColor"] = borderDark,
                ["BorderLightColor"] = borderLight,
                ["BorderGlowColor"] = borderGlow
            };
        }

        private static string WithAlpha(string hexColor, string alpha)
        {
            var trimmed = hexColor.TrimStart('#');
            return trimmed.Length == 6 ? $"#{alpha}{trimmed}" : hexColor;
        }

        private static void UpdateColorResource(ResourceDictionary dictionary, string colorKey, Color color)
        {
            if (dictionary.Contains(colorKey))
            {
                dictionary[colorKey] = color;
            }

            foreach (var merged in dictionary.MergedDictionaries)
            {
                UpdateColorResource(merged, colorKey, color);
            }
        }

        private static void UpdateBrushResource(ResourceDictionary dictionary, string colorKey, Color color)
        {
            var brushKey = colorKey.Replace("Color", "Brush", StringComparison.Ordinal);
            if (dictionary.Contains(brushKey))
            {
                var replacement = new SolidColorBrush(color);
                replacement.Freeze();
                dictionary[brushKey] = replacement;
            }

            foreach (var merged in dictionary.MergedDictionaries)
            {
                UpdateBrushResource(merged, colorKey, color);
            }
        }

        private static void UpdateGradientBrush(string resourceKey, string startColor, string endColor)
        {
            if (Application.Current?.Resources == null)
            {
                return;
            }

            var start = (Color)ColorConverter.ConvertFromString(startColor);
            var end = (Color)ColorConverter.ConvertFromString(endColor);
            ReplaceGradientResource(Application.Current.Resources, resourceKey, start, end);
        }

        private static void ReplaceGradientResource(ResourceDictionary dictionary, string resourceKey, Color start, Color end)
        {
            if (dictionary.Contains(resourceKey))
            {
                var replacement = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = resourceKey.Equals("GlowGradientBrush", StringComparison.OrdinalIgnoreCase)
                        ? new Point(0, 1)
                        : new Point(1, 1)
                };
                replacement.GradientStops.Add(new GradientStop(start, 0));
                replacement.GradientStops.Add(new GradientStop(end, 1));
                replacement.Freeze();
                dictionary[resourceKey] = replacement;
            }

            foreach (var merged in dictionary.MergedDictionaries)
            {
                ReplaceGradientResource(merged, resourceKey, start, end);
            }
        }
    }
}

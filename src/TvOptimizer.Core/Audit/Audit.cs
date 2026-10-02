// Audit.cs — порт scripts/audit.sh: TIER_1/2/HEURISTIC/PROTECTED/UNIDENTIFIED.
// Чистий C#, детермінований, покритий тестами. Жодного ADB тут — тільки множини.
using System.Text.RegularExpressions;
using TvOptimizer.Core.Config;
using TvOptimizer.Core.Safety;

namespace TvOptimizer.Core.Audit;

public enum Tier { Safe, Review, Heuristic, Protected, Unidentified }

public sealed record AuditResult(
    string PackageName,
    bool GenericMode,
    string ModeName,
    Tier Tier,
    IReadOnlyList<string> SafePresent,
    IReadOnlyList<string> SafeGone,
    IReadOnlyList<string> ReviewPresent,
    IReadOnlyList<string> HeuristicHits,
    IReadOnlyList<string> ProtectedPresent,
    IReadOnlyList<string> ProtectedMissing,
    IReadOnlyList<string> UnidentifiedShown,
    int UnidentifiedTotal,
    string? Label = null,
    string? Description = null,
    int TrackerCount = 0)
{
    public string Details => TrackerCount > 0 
        ? $"Тир: {Tier}, Режим: {ModeName} (Трекери: {TrackerCount})" 
        : $"Тир: {Tier}, Режим: {ModeName}";
    public bool NeedsAction => Tier == Tier.Review || Tier == Tier.Heuristic || Tier == Tier.Protected;
}

public static class AuditEngine
{
    public static readonly IReadOnlyDictionary<string, string> KnownTrackers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "com.yandex.metrica", "AppMetrica" },
        { "com.facebook.analytics", "Facebook Analytics" },
        { "com.appsflyer", "AppsFlyer" },
        { "com.adjust.sdk", "Adjust" },
        { "com.crashlytics", "Crashlytics" }
    };

    private static readonly Regex HeuristicRe = new(
        @"analytics|telemetry|tracker|[^a-z]acr([^a-z]|$)|adservice|recommend|promo|demo|retail|partnercustomizer|printspooler|nearby\\.halfsheet|feedback|federated|personalization",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NoiseRe = new(
        @"^(android|com\\.android|com\\.google\\.android\\.(overlay|ext|module|ondevice|federated|adservices)|mediatek\\.factorymenu)\\b",
        RegexOptions.Compiled);
    private static readonly Regex OverlayNoiseRe = new(
        @"(overlay|rro|resoverlay|auto_generated)\\b", RegexOptions.Compiled);

    /// <param name="installed">Встановлені пакети з `pm list packages`.</param>
    /// <param name="device">Конфіг пристрою або null (generic-режим).</param>
    /// <param name="curatedTier2">Об'єднаний curated TIER_2.</param>
    /// <param name="uadApps">Словник UAD-додатків за id для позначки та опису.</param>
    public static List<AuditResult> Run(
        IReadOnlySet<string> installed,
        DeviceConfig? device,
        IReadOnlySet<string> curatedTier2,
        IReadOnlyDictionary<string, UadAppInfo>? uadApps = null)
    {
        bool generic = device is null;
        IReadOnlySet<string> tier1 = generic
            ? Guard.UniversalSafe
            : new HashSet<string>(device!.SafeRemoveSet, StringComparer.Ordinal);
        IReadOnlySet<string> deviceProtected = generic
            ? Guard.UniversalProtected
            : new HashSet<string>(device!.ProtectedSet, StringComparer.Ordinal);

        // TIER_2 = curated + (device OPTIONAL, якщо не generic)
        var tier2 = new HashSet<string>(curatedTier2, StringComparer.Ordinal);
        if (!generic)
        {
            tier2.UnionWith(device!.OptionalStreamingSet);
            tier2.UnionWith(device!.OptionalOtherSet);
        }

        // Фільтр tier1/tier2 від NEVER_TOUCH/protected (як filter_lists в audit.sh)
        tier1 = tier1.Where(p => Guard.CanRemove(p, deviceProtected,
                generic ? Guard.DefaultStockLauncher : device!.StockLauncher)).ToHashSet(StringComparer.Ordinal);
        var t1snap = new HashSet<string>(tier1, StringComparer.Ordinal);
        tier2 = tier2.Where(p => Guard.CanRemove(p, deviceProtected,
                generic ? Guard.DefaultStockLauncher : device!.StockLauncher) && !t1snap.Contains(p))
            .ToHashSet(StringComparer.Ordinal);

        var known = new HashSet<string>(StringComparer.Ordinal);
        known.UnionWith(tier1);
        known.UnionWith(tier2);
        known.UnionWith(deviceProtected);

        string modeName = generic
            ? "GENERIC (невідомий ТВ, тільки UNIVERSAL_SAFE + curated)"
            : $"DEVICE ({device!.Name})";

        var results = new List<AuditResult>();
        foreach (var package in installed)
        {
            bool isInTier1 = tier1.Contains(package);
            bool isInTier2 = tier2.Contains(package);
            bool isInDeviceProtected = deviceProtected.Contains(package);
            bool isInNeverTouch = Guard.NeverTouch.Contains(package);
            bool isInUniversalProtected = Guard.UniversalProtected.Contains(package);

            bool isProtected = isInDeviceProtected || isInUniversalProtected;
            bool isHeuristic = HeuristicRe.IsMatch(package);

            Tier tier;
            string? label = null;
            string? description = null;

            // If we have UAD info for this package, use it for label/description and potentially for tier
            if (uadApps != null && uadApps.TryGetValue(package, out var uadApp))
            {
                label = uadApp.Label;
                description = uadApp.Description;

                // If the package is not protected by device or universal lists, use UAD removal to determine tier
                if (!isProtected)
                {
                    switch (uadApp.Removal)
                    {
                        case "Recommended": tier = Tier.Safe; break;
                        case "Advanced":    tier = Tier.Review; break;
                        case "Expert":      tier = Tier.Heuristic; break;
                        case "Unsafe":      tier = Tier.Protected; break;
                        default:            tier = Tier.Unidentified; break;
                    }
                }
                else
                {
                    // Package is protected (by device or universal) -> override to Protected regardless of UAD removal
                    tier = Tier.Protected;
                }
            }
            else
            {
                // No UAD info, fall back to original logic
                if (isProtected)
                {
                    tier = Tier.Protected;
                }
                else if (isInTier1)
                {
                    tier = Tier.Safe;
                }
                else if (isInTier2)
                {
                    tier = Tier.Review;
                }
                else if (isHeuristic)
                {
                    tier = Tier.Heuristic;
                }
                else
                {
                    tier = Tier.Unidentified;
                }
            }

            int trackerCount = 0;
            foreach (var tracker in KnownTrackers.Keys)
            {
                if (package.Contains(tracker, StringComparison.OrdinalIgnoreCase))
                {
                    trackerCount++;
                }
            }

            results.Add(new AuditResult(
                PackageName: package,
                GenericMode: generic,
                ModeName: modeName,
                Tier: tier,
                SafePresent: new List<string>(),
                SafeGone: new List<string>(),
                ReviewPresent: new List<string>(),
                HeuristicHits: new List<string>(),
                ProtectedPresent: new List<string>(),
                ProtectedMissing: new List<string>(),
                UnidentifiedShown: new List<string>(),
                UnidentifiedTotal: 0,
                Label: label,
                Description: description,
                TrackerCount: trackerCount
            ));
        }

        return results;
    }
}
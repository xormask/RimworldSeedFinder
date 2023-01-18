using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using HugsLib;
using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Profile;

namespace SeedFinder {

enum FeatureFilter {
    Present,
    NotPresent,
    Either
};

enum Hemisphere {
    Northern,
    Southern,
    Either
};

enum Seasonality {
    Any,
    Normal,
    PermSummer,
    PermWinter,
};

class SeedFinderFilterParameters {
    public string outDirectory;
    public string baseSeed;
    public int maxFound;
    public bool clearFog;
    public bool highlightPOI;
    public float planetCoverage;
    public OverallRainfall rainfall;
    public OverallTemperature temperature;
    public OverallPopulation population;
    public float pollution;
    public int mapSize;
    public List<FactionDef> factions;
    public BiomeDef biome;
    public Hilliness hilliness;
    public FeatureFilter river;
    public List<bool> desiredRivers;
    public FeatureFilter coastal;
    public FeatureFilter caves;
    public Hemisphere hemisphere;
    public int maxTemp;
    public int minTemp;
    public int minGrowingDays;
    public Seasonality seasonality;
    public int minGeysers;
    public int minRichSoilTiles;
    public bool needCivilOutlanderNear;
    public bool needRoughOutlanderNear;
    public bool needCivilTribeNear;
    public bool needRoughTribeNear;
    public bool needEmpireNear;

    public ThingDef firstStone;
    public List<bool> desiredStones;

    public Vector2 stoneScroll;
    public Vector2 windowScroll;

    public SeedFinderFilterParameters()
    {
    }
}

class FilterWindow : Verse.Window
{
    private SeedFinderFilterParameters filterParams;

    public FilterWindow(SeedFinderFilterParameters fp)
    {
        filterParams = fp;
        doCloseX = true;
        closeOnClickedOutside = false;
        closeOnAccept = false;
        resizeable = false;
        draggable = false;
    }
    public override Vector2 InitialSize => new Vector2(750f, UI.screenHeight - 100);

    public override void DoWindowContents(Rect inRect)
    {
        float curY = 0f;
        var buttonSize = new Vector2(150f, 30f);
        var labelSize = 28f;
        var largeButtonSize = new Vector2(150f, 38f);
        var rightOffset = 360f;
        var buttonOffset = 150f;
        var skipSize = 35f;
        var titleSkipSize = 45f;

        var origAnchor = Text.Anchor;
        var origFont = Text.Font;

        Text.Anchor = TextAnchor.MiddleLeft;
        Text.Font = GameFont.Medium;
        

        Rect fullWindowRect = new Rect(0f, 0f, 690f, 950f);//size of the inner settings window
        Rect windowScrollRect = new Rect(0f, 0f, inRect.width - 8f, (inRect.height - largeButtonSize.y)-10);
        Widgets.BeginScrollView(windowScrollRect, ref filterParams.windowScroll, fullWindowRect);

        Rect titleRect = new Rect(0, 0, inRect.width, 40f);
        Widgets.Label(titleRect, "SeedFinder Settings");

        Text.Font = origFont;
        
        curY += titleSkipSize;

        // Seed prefix
        Widgets.Label(new Rect(0, curY, buttonOffset, labelSize), "Screenshot Directory: ");
        filterParams.outDirectory = Widgets.TextField(new Rect(buttonOffset, curY, 300f, buttonSize.y), filterParams.outDirectory);
        if (Widgets.ButtonText(new Rect(buttonOffset + 305f, curY, buttonSize.x - 25, buttonSize.y), "Open Folder")) {
            Directory.CreateDirectory(filterParams.outDirectory);
            Process.Start(@filterParams.outDirectory);
        }

        curY += skipSize;

        Widgets.Label(new Rect(0, curY, buttonOffset, labelSize), "Seed Prefix: ");
        filterParams.baseSeed = Widgets.TextField(new Rect(buttonOffset, curY, 300f, buttonSize.y), filterParams.baseSeed);

        curY += skipSize;

        Widgets.Label(new Rect(0, curY, buttonOffset, labelSize), "Max # Matches: ");
        var numMatchStr = filterParams.maxFound.ToString();
        Widgets.TextFieldNumeric(new Rect(buttonOffset, curY, buttonSize.x, buttonSize.y), ref filterParams.maxFound, ref numMatchStr, 1f, 10000f);

        curY += skipSize;

        Widgets.CheckboxLabeled(new Rect(0, curY, 250, labelSize), "Clear Fog in Screenshots", ref filterParams.clearFog, disabled: false, null, null, placeCheckboxNearText: true);

        Widgets.CheckboxLabeled(new Rect(250, curY, 360, labelSize), "Highlight Map Features (Anima Tree, Geysers, etc)", ref filterParams.highlightPOI, disabled: false, null, null, placeCheckboxNearText: true);

        curY += 60f;

        Text.Font = GameFont.Medium;

        Widgets.Label(new Rect(0, curY, inRect.width, 25f), "Filters");

        Text.Font = origFont;

        curY += titleSkipSize;

        buttonOffset = 135f;
        // Biome
        Widgets.Label(new Rect(0, curY, buttonOffset, labelSize), "Biome: ");

        var curBiome = GenText.CapitalizeAsTitle(filterParams.biome.label);
        if (Widgets.ButtonText(new Rect(buttonOffset, curY, buttonSize.x, buttonSize.y), curBiome, true, true, true)) {
            var options = new List<FloatMenuOption>();
            foreach (var biomeDef in DefDatabase<BiomeDef>.AllDefsListForReading) {
                if (!biomeDef.canBuildBase) continue;

                var label = GenText.CapitalizeAsTitle(biomeDef.label);
                options.Add(new FloatMenuOption(label, () => {
                    filterParams.biome = biomeDef;
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        // Hilliness
        Widgets.Label(new Rect(rightOffset, curY, buttonOffset, labelSize), "Hilliness: ");

        if (Widgets.ButtonText(new Rect(rightOffset + buttonOffset, curY, buttonSize.x, buttonSize.y),
                               GenText.CapitalizeAsTitle(HillinessUtility.GetLabel(filterParams.hilliness)), true, true, true)) {
            var options = new List<FloatMenuOption>();
            var possibleHilliness = new List<Hilliness>() { Hilliness.Flat, Hilliness.SmallHills, Hilliness.LargeHills, Hilliness.Mountainous };
            foreach (var hilliness in possibleHilliness) {
                options.Add(new FloatMenuOption(GenText.CapitalizeAsTitle(HillinessUtility.GetLabel(hilliness)), () => {
                    filterParams.hilliness = hilliness;
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        curY += skipSize;

        Func<FeatureFilter, String> filterToStr = (FeatureFilter f) => {
            if (f == FeatureFilter.Present) {
                return "Yes";
            } else if (f == FeatureFilter.NotPresent) {
                return "No";
            } else {
                return "Don't care";
            }
        };

        var featureFilters = new List<FeatureFilter>() { FeatureFilter.Either, FeatureFilter.Present, FeatureFilter.NotPresent };

        // Coastal
        Widgets.Label(new Rect(0, curY, buttonOffset, labelSize), "Coastal: ");

        if (Widgets.ButtonText(new Rect(buttonOffset, curY, buttonSize.x, buttonSize.y), filterToStr(filterParams.coastal), true, true, true)) {
            var options = new List<FloatMenuOption>();
            foreach (var filter in featureFilters) {
                options.Add(new FloatMenuOption(filterToStr(filter), () => {
                    filterParams.coastal = filter;
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        // Caves
        Widgets.Label(new Rect(rightOffset, curY, buttonOffset, labelSize), "Caves: ");

        if (Widgets.ButtonText(new Rect(rightOffset + buttonOffset, curY, buttonSize.x, buttonSize.y), filterToStr(filterParams.caves), true, true, true)) {
            var options = new List<FloatMenuOption>();
            foreach (var filter in featureFilters) {
                options.Add(new FloatMenuOption(filterToStr(filter), () => {
                    filterParams.caves = filter;
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        curY += skipSize;

        // Stone types
        Widgets.Label(new Rect(0, curY, buttonOffset, labelSize), "Primary Stone: ");
        var curStone = filterParams.firstStone != null ? GenText.CapitalizeAsTitle(filterParams.firstStone.label) : "Any";

        if (Widgets.ButtonText(new Rect(buttonOffset, curY, buttonSize.x, buttonSize.y), curStone, true, true, true)) {
            var options = new List<FloatMenuOption>();
            options.Add(new FloatMenuOption("Any", () => {
                filterParams.firstStone = null;
            }));

            foreach (var stoneDef in SeedFinderController.Instance.allStones) {
                var label = GenText.CapitalizeAsTitle(stoneDef.label);
                options.Add(new FloatMenuOption(label, () => {
                    filterParams.firstStone = stoneDef;
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        // Hemisphere

        Func<Hemisphere, String> hemiToStr = (Hemisphere h) => {
            if (h == Hemisphere.Northern) {
                return "Northern";
            } else if (h == Hemisphere.Southern) {
                return "Southern";
            } else {
                return "Don't care";
            }
        };

        Widgets.Label(new Rect(rightOffset, curY, buttonOffset, labelSize), "Hemisphere: ");

        if (Widgets.ButtonText(new Rect(rightOffset + buttonOffset, curY, buttonSize.x, buttonSize.y), hemiToStr(filterParams.hemisphere), true, true, true)) {
            var options = new List<FloatMenuOption>();
            options.Add(new FloatMenuOption(hemiToStr(Hemisphere.Either), () => {
                filterParams.hemisphere = Hemisphere.Either;
            }));

            options.Add(new FloatMenuOption(hemiToStr(Hemisphere.Northern), () => {
                filterParams.hemisphere = Hemisphere.Northern;
            }));

            options.Add(new FloatMenuOption(hemiToStr(Hemisphere.Southern), () => {
                filterParams.hemisphere = Hemisphere.Southern;
            }));

            Find.WindowStack.Add(new FloatMenu(options));
        }

        curY += skipSize;

        // Rivers
        curY += 10f;

        Widgets.Label(new Rect(0, curY, buttonOffset, labelSize), "River: ");

        if (Widgets.ButtonText(new Rect(buttonOffset, curY, buttonSize.x, buttonSize.y), filterToStr(filterParams.river), true, true, true)) {
            var options = new List<FloatMenuOption>();
            foreach (var filter in featureFilters) {
                options.Add(new FloatMenuOption(filterToStr(filter), () => {
                    filterParams.river = filter;
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        if (filterParams.river == FeatureFilter.Present) {
            var offset = 300f;
            for (int idx = 0; idx < SeedFinderController.Instance.allRivers.Count; idx++) {
                var riverDef = SeedFinderController.Instance.allRivers[idx];
                bool desired = filterParams.desiredRivers[idx];
                var riverLabel = GenText.CapitalizeAsTitle(riverDef.label);

                Widgets.CheckboxLabeled(new Rect(offset, curY + 3.5f, 150, labelSize - 3), riverLabel,
                                        ref desired, disabled: false, null, null, placeCheckboxNearText: true);

                filterParams.desiredRivers[idx] = desired;

                var numChars = riverLabel.Length;
                offset += 50 + 6 * numChars;
            }

        }
        curY += titleSkipSize;

        // Growing days
        Func<int, String> growingDaysToStr = (int growingDays) => {
            if (growingDays < 60) {
                return growingDays.ToString();
            }

            return "Year-round";
        };

        Widgets.Label(new Rect(0, curY, buttonOffset, labelSize), "Min Growing Days: ");
        if (Widgets.ButtonText(new Rect(buttonOffset, curY, buttonSize.x, buttonSize.y), growingDaysToStr(filterParams.minGrowingDays), true, true, true)) {
            var growingIncrements = new List<int>() { 0, 10, 20, 30, 40, 50, 60 };
            var options = new List<FloatMenuOption>();
            foreach (var growingDays in growingIncrements) {
                var label = growingDaysToStr(growingDays);
                options.Add(new FloatMenuOption(label, () => {
                    filterParams.minGrowingDays = growingDays;
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        // Permanent Summer
        Widgets.Label(new Rect(rightOffset, curY, buttonOffset, labelSize), "Seasonality: ");

        Func<Seasonality, String> seasonalityToStr = (Seasonality seasonality) => {
            if (seasonality == Seasonality.Normal) {
                return "Normal";
            } else if (seasonality == Seasonality.PermSummer) {
                return "Permanent Summer";
            } else if (seasonality == Seasonality.PermWinter) {
                return "Permanent Winter";
            } else {
                return "Don't Care";
            }
        };

        if (Widgets.ButtonText(new Rect(rightOffset + buttonOffset, curY, buttonSize.x, buttonSize.y), seasonalityToStr(filterParams.seasonality), true, true, true)) {
            var seasonalities = new List<Seasonality>() { Seasonality.Any, Seasonality.Normal, Seasonality.PermSummer, Seasonality.PermWinter };
            var options = new List<FloatMenuOption>();
            foreach (var seasonality in seasonalities) {
                options.Add(new FloatMenuOption(seasonalityToStr(seasonality), () => {
                    filterParams.seasonality = seasonality;
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        curY += skipSize;

        // Min Temp 
        Widgets.Label(new Rect(0, curY, buttonOffset, labelSize), "Min Allowed Temp: ");
        string minTempStr = filterParams.minTemp.ToString();
        Widgets.TextFieldNumeric(new Rect(buttonOffset, curY, buttonSize.x, buttonSize.y), ref filterParams.minTemp, ref minTempStr, -500f, 500f);

        // Max Temp 
        Widgets.Label(new Rect(rightOffset, curY, buttonOffset, labelSize), "Max Allowed Temp: ");
        string maxTempStr = filterParams.maxTemp.ToString();
        Widgets.TextFieldNumeric(new Rect(rightOffset + buttonOffset, curY, buttonSize.x, buttonSize.y), ref filterParams.maxTemp, ref maxTempStr, -500f, 500f);

        curY += skipSize;

        Widgets.Label(new Rect(0, curY, buttonOffset, labelSize), "Min Geysers: ");
        string geysersTempStr = filterParams.minGeysers.ToString();
        Widgets.TextFieldNumeric(new Rect(buttonOffset, curY, buttonSize.x, buttonSize.y), ref filterParams.minGeysers, ref geysersTempStr, 0, 10);

        Widgets.Label(new Rect(rightOffset, curY, buttonOffset, labelSize), "Min Fertile Soil Tiles: ");
        string soilTempStr = filterParams.minRichSoilTiles.ToString();
        Widgets.TextFieldNumeric(new Rect(rightOffset + buttonOffset, curY, buttonSize.x, buttonSize.y), ref filterParams.minRichSoilTiles, ref soilTempStr, 0, int.MaxValue);

        curY += skipSize;
        curY += 10f;

        Widgets.Label(new Rect(0, curY, 350, labelSize), "Required Stone Types on Map (order doesn't matter)");
        curY += 25f;

        float totalStoneWidth = 0f;
        for (int idx = 0; idx < SeedFinderController.Instance.allStones.Count; idx++) {
            var stoneDef = SeedFinderController.Instance.allStones[idx];
            var stoneLabel = GenText.CapitalizeAsTitle(stoneDef.label);

            totalStoneWidth += 50 + 7 * stoneLabel.Length;
        }

        var stoneOffset = 30f;
        // if statement disables scrollView so it doesn't eat scrollwheel inputs when not needed
        // (Tynan's version of scrollView doesn't include the override that disables the scrollwheel input)
        if (totalStoneWidth > fullWindowRect.width) {
            Rect fullStoneRect = new Rect(30f, curY, totalStoneWidth, labelSize);
            Rect scrollRect = new Rect(30f, curY, fullWindowRect.width - 40f, labelSize + 16f);
            Widgets.BeginScrollView(scrollRect, ref filterParams.stoneScroll, fullStoneRect);
        }

        for (int idx = 0; idx < SeedFinderController.Instance.allStones.Count; idx++) {
            var stoneDef = SeedFinderController.Instance.allStones[idx];
            bool desired = filterParams.desiredStones[idx];
            var stoneLabel = GenText.CapitalizeAsTitle(stoneDef.label);

            Widgets.CheckboxLabeled(new Rect(stoneOffset, curY + 3.5f, 150, labelSize - 3), stoneLabel,
                                    ref desired, disabled: false, null, null, placeCheckboxNearText: true);

            filterParams.desiredStones[idx] = desired;

            var numChars = stoneLabel.Length;
            stoneOffset += 50 + 7 * numChars;
        }

        if (totalStoneWidth > fullWindowRect.width)
        {
            Widgets.EndScrollView();
        }

        curY += skipSize;
        curY += 10f;

        // Faction filters
        Widgets.Label(new Rect(0, curY, 350, labelSize), "Require Nearby Settlements: (within drop pod range)");
        curY += 25f;

        Widgets.CheckboxLabeled(new Rect(30, curY, 150, labelSize - 3), "Civil Outlander", ref filterParams.needCivilOutlanderNear, disabled: false, null, null, placeCheckboxNearText: true);
        Widgets.CheckboxLabeled(new Rect(180, curY, 150, labelSize - 3), "Rough Outlander", ref filterParams.needRoughOutlanderNear, disabled: false, null, null, placeCheckboxNearText: true);

        Widgets.CheckboxLabeled(new Rect(330, curY, 120, labelSize - 3), "Gentle Tribe", ref filterParams.needCivilTribeNear, disabled: false, null, null, placeCheckboxNearText: true);
        Widgets.CheckboxLabeled(new Rect(450, curY, 120, labelSize - 3), "Fierce Tribe", ref filterParams.needRoughTribeNear, disabled: false, null, null, placeCheckboxNearText: true);

        Widgets.CheckboxLabeled(new Rect(590, curY, 120, labelSize - 3), "Empire", ref filterParams.needEmpireNear, disabled: false, null, null, placeCheckboxNearText: true);

        curY += 60f;

        Text.Font = GameFont.Medium;

        Widgets.Label(new Rect(0, curY, inRect.width, 25f), "World Gen Parameters");
        Text.Font = GameFont.Tiny;

        Widgets.Label(new Rect(225, curY + 5f, inRect.width, 15f), "(must match parameters you will use)");

        Text.Font = origFont;

        curY += titleSkipSize;

        buttonOffset = 150f;
        // Planet Size
        Widgets.Label(new Rect(0, curY, buttonOffset, labelSize), "Global Coverage: ");
        Func<float, string> covToStr = (float cov) => {
            if (cov == 1f) {
                return "100%";
            } else if (cov == 0.5f) {
                return "50%";
            } else {
                return "30%";
            }
        };

        if (Widgets.ButtonText(new Rect(buttonOffset, curY, buttonSize.x, buttonSize.y), covToStr(filterParams.planetCoverage), true, true, true)) {
            var possibleCoverage = new List<float>() { 0.3f, 0.5f, 1.0f };
            var options = new List<FloatMenuOption>();

            foreach (var coverage in possibleCoverage) {
                options.Add(new FloatMenuOption(covToStr(coverage), () => {
                    filterParams.planetCoverage = coverage;
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }


        curY += skipSize;

        var sliderSize = new Vector2(200f, 28f);

        // Rainfall
        Widgets.Label(new Rect(0, curY, buttonOffset, labelSize), "Overall Rainfall: ");
        filterParams.rainfall = (OverallRainfall)Mathf.RoundToInt(Widgets.HorizontalSlider(new Rect(buttonOffset, curY, sliderSize.x, sliderSize.y), (float)filterParams.rainfall, 0f, OverallRainfallUtility.EnumValuesCount - 1, middleAlignment: true, "PlanetRainfall_Normal".Translate(), "PlanetRainfall_Low".Translate(), "PlanetRainfall_High".Translate(), 1f));

        curY += skipSize;

        // Temperature
        Widgets.Label(new Rect(0, curY, buttonOffset, labelSize), "Overall Temperature: ");
        filterParams.temperature = (OverallTemperature)Mathf.RoundToInt(Widgets.HorizontalSlider(new Rect(buttonOffset, curY, sliderSize.x, sliderSize.y), (float)filterParams.temperature, 0f, OverallTemperatureUtility.EnumValuesCount - 1, middleAlignment: true, "PlanetTemperature_Normal".Translate(), "PlanetTemperature_Low".Translate(), "PlanetTemperature_High".Translate(), 1f));

        curY += skipSize;

        // Population
        Widgets.Label(new Rect(0, curY, buttonOffset, labelSize), "Population: ");
        filterParams.population = (OverallPopulation)Mathf.RoundToInt(Widgets.HorizontalSlider(new Rect(buttonOffset, curY, sliderSize.x, sliderSize.y), (float)filterParams.population, 0f, OverallPopulationUtility.EnumValuesCount - 1, middleAlignment: true, "PlanetPopulation_Normal".Translate(), "PlanetPopulation_Low".Translate(), "PlanetPopulation_High".Translate(), 1f));

        curY += skipSize;

        // Pollution 
        Widgets.Label(new Rect(0, curY, buttonOffset, labelSize), "Pollution: ");
        filterParams.pollution = Widgets.HorizontalSlider(new Rect(buttonOffset, curY, sliderSize.x, sliderSize.y), filterParams.pollution, 0f, 1f, middleAlignment: true,  filterParams.pollution.ToStringPercent(), null, null, 0.05f);

        curY += skipSize;

        // Map size
        Widgets.Label(new Rect(0, curY, buttonOffset, labelSize), "Map Size: ");
        Func<int, string> mapSizeToStr = (int size) => {
            return string.Concat(size.ToString(), " x ", size.ToString());
        };

        if (Widgets.ButtonText(new Rect(buttonOffset, curY, buttonSize.x, buttonSize.y), mapSizeToStr(filterParams.mapSize))) {
            var mapSizes = new List<int>() { 200, 225, 250, 275, 300, 325 };

            if (Prefs.TestMapSizes) {
                mapSizes.Add(350);
                mapSizes.Add(400);
            }

            var options = new List<FloatMenuOption>();

            foreach (var size in mapSizes) {
                options.Add(new FloatMenuOption(mapSizeToStr(size), () => {
                    filterParams.mapSize = size;
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        curY += skipSize;


        Widgets.EndScrollView();

        // Submission button
        if (Widgets.ButtonText(new Rect(inRect.width / 2 - largeButtonSize.x / 2, inRect.height - largeButtonSize.y, largeButtonSize.x, largeButtonSize.y), "Search")) {
            SeedFinderController.Instance.startFinding();
        }

        Text.Anchor = origAnchor;
    }
}

[HarmonyPatch(typeof (Page_SelectScenario), "DoWindowContents")]
public static class Page_SelectScenario_DoWindowContents_PostPatch {
    [HarmonyPostfix]
    public static void DrawFinderButton(Page_SelectScenario __instance, Rect rect) {
        var buttonSize = new Vector2(120f, 38f);
        if (Widgets.ButtonText(new Rect(rect.width - buttonSize.x,
                                        0, buttonSize.x, buttonSize.y), "Find Seeds")) {
            SeedFinderController.Instance.openFilterWindow();
        }
    }
}

/// <summary>
/// The hub of the mod. Instantiated by HugsLib.
/// </summary>
public class SeedFinderController : ModBase {

    public static SeedFinderController Instance { get; private set; }

    private int curSeedOffset;
    private int numFound;
    private Stack<int> validTiles;
    private bool isSeedFinding;
    private bool needCapture;
    private bool captureFinished;
    private SeedFinderFilterParameters filterParams;
    private Vector2 origAnimaSize;
    private float animaRadius;
    public List<ThingDef> allStones { get; private set; }
    public List<RiverDef> allRivers { get; private set; }

    public override string ModIdentifier {
        get { return "SeedFinder"; }
    }

    internal new ModLogger Logger {
        get { return base.Logger; }
    }

    private void reset() {
        curSeedOffset = 0;
        numFound = 0;
        validTiles = new Stack<int>();
        isSeedFinding = false;
        needCapture = false;
        captureFinished = false;
    }

    private SeedFinderController() {
        Instance = this;
        filterParams = new SeedFinderFilterParameters();
        origAnimaSize = new Vector2(0, 0);
        animaRadius = -1f;
        reset();
    }

    private static bool IsStone(ThingDef thingDef) {
        return thingDef.category == ThingCategory.Building &&
            thingDef.building.isNaturalRock &&
            !thingDef.building.isResourceRock;
    }

    internal void openFilterWindow() {
        Find.WindowStack.Add(new FilterWindow(filterParams));
    }

    internal void startFinding() {
        isSeedFinding = true;

        ThingDefOf.Plant_TreeAnima.graphicData.drawSize *= 2.5f;

        visitNextMap();
    }

    internal void stopFinding() {
        reset();

        ThingDefOf.Plant_TreeAnima.graphicData.drawSize = origAnimaSize;
    }

    public override void Initialize() {
        filterParams.outDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "RimworldSeedFinder");
        filterParams.baseSeed = GenText.RandomSeedString();
        filterParams.maxFound = 100;
        filterParams.clearFog = false;
        filterParams.highlightPOI = true;
        filterParams.planetCoverage = 1f;
        filterParams.rainfall = OverallRainfall.Normal;
        filterParams.temperature = OverallTemperature.Normal;
        filterParams.population = OverallPopulation.Normal;
        filterParams.pollution = (ModsConfig.BiotechActive ? 0.05f : 0f);
        filterParams.mapSize = 250;

        filterParams.factions = new List<FactionDef>();
        foreach (FactionDef faction in FactionGenerator.ConfigurableFactions) {
            filterParams.factions.Add(faction);
        }

        filterParams.biome = DefDatabase<BiomeDef>.AllDefsListForReading[0];
        filterParams.hilliness = Hilliness.Flat;

        filterParams.river = FeatureFilter.Either;
        filterParams.coastal = FeatureFilter.Either;
        filterParams.caves = FeatureFilter.Either;
        filterParams.hemisphere = Hemisphere.Either;

        filterParams.maxTemp = 200;
        filterParams.minTemp = -200;
        filterParams.minGrowingDays = 0;
        filterParams.seasonality = Seasonality.Any;
        filterParams.minGeysers = 0;
        filterParams.minRichSoilTiles = 0;
        filterParams.firstStone = null;

        filterParams.needCivilOutlanderNear = false;
        filterParams.needRoughOutlanderNear = false;
        filterParams.needCivilTribeNear = false;
        filterParams.needRoughTribeNear = false;
        filterParams.needEmpireNear = false;

        allStones = DefDatabase<ThingDef>.AllDefs.Where(SeedFinderController.IsStone).ToList();
        allRivers = DefDatabase<RiverDef>.AllDefsListForReading;

        filterParams.desiredRivers = new List<bool>();
        foreach (var riverDef in allRivers) {
            filterParams.desiredRivers.Add(true);
        }

        filterParams.desiredStones = new List<bool>();
        foreach (var riverDef in allStones) {
            filterParams.desiredStones.Add(false);
        }

            filterParams.stoneScroll = new Vector2(0, 0);
            filterParams.windowScroll = new Vector2(0, 0);

            origAnimaSize = ThingDefOf.Plant_TreeAnima.graphicData.drawSize;

        foreach (var animaComp in ThingDefOf.Plant_TreeAnima.comps) {
            var meditationComp = animaComp as CompProperties_MeditationFocus;
            if (meditationComp != null) {
                foreach (var offset in meditationComp.offsets) {
                    var radiusOffset = offset as FocusStrengthOffset_ArtificialBuildings;
                    if (radiusOffset != null) {
                        animaRadius = radiusOffset.radius;
                        break;
                    }
                }
            }

            if (animaRadius != -1f) {
                break;
            }
        }
    }

    internal float FertilityAt(Map map, IntVec3 loc) {
        float topFertility = map.terrainGrid.TerrainAt(loc).fertility;
        float bottomFertility = 0f;
        if (map.terrainGrid.CanRemoveTopLayerAt(loc)) {
            bottomFertility = map.terrainGrid.UnderTerrainAt(loc).fertility;
        }

        return Mathf.Max(topFertility, bottomFertility);
    }

    public override void MapLoaded(Map map) {
        if (!isSeedFinding) {
            return;
        }

        Find.MusicManagerPlay.ForceFadeoutAndSilenceFor(120f);
        
        foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction.ToList()) {
            pawn.inventory.DestroyAll();
            if (pawn.Spawned) {
                pawn.DeSpawn();
            }

            if (pawn.holdingOwner != null) {
                pawn.holdingOwner.Remove(pawn);
            }

            if (!pawn.IsWorldPawn()) {
                Find.WorldPawns.PassToWorld(pawn);
            }
        }

        foreach (var pawn in Find.WorldPawns.AllPawnsAliveOrDead.ToList()) {
            Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
        }

        // Get rid of error spam from triggers trying to run after the map has been destroyed
        foreach (var thing in map.listerThings.AllThings.ToList()) {
            if (thing.def == ThingDefOf.InsectJelly || thing.def == ThingDefOf.RectTrigger || thing.Faction == Faction.OfMechanoids ||
                thing.Faction == Faction.OfInsects || thing.Faction == Faction.OfAncients || thing.Faction == Faction.OfAncientsHostile) {
                if (thing.holdingOwner != null) {
                    thing.holdingOwner.Remove(thing);
                }
                thing.DeSpawn();
                thing.Destroy();
            }
        }

        int numGeysers = 0;
        foreach (var geyser in map.listerBuildings.AllBuildingsNonColonistOfDef(ThingDefOf.SteamGeyser)) {
            numGeysers++;
        }

        bool mapFilterPassed = true;

        if (numGeysers < filterParams.minGeysers) {
            mapFilterPassed = false;
        }

        if (filterParams.minRichSoilTiles != 0 && mapFilterPassed) {
            int numRichSoilTiles = 0;

            for (int z = 0; z < map.Size.z; z++) {
                for (int x = 0; x < map.Size.x; x++) {
                    var loc = new IntVec3(x, 0, z);

                    if (FertilityAt(map, loc) >= 1.1f) {
                        numRichSoilTiles++;
                    }
                }
            }

            if (numRichSoilTiles < filterParams.minRichSoilTiles) {
                mapFilterPassed = false;
            }
        }

        if (mapFilterPassed) {
            needCapture = true;

            if (filterParams.clearFog) {
                map.fogGrid.ClearAllFog();
            }

            float longitude = Find.WorldGrid.LongLatOf(map.Tile).x;
            long absTicks = Find.TickManager.TicksAbs;

            int dayTicks = GenDate.DayTick(absTicks, longitude);
            // Slight offset from noon to preserve shadows
            int advanceTicks = 25000;
            if (dayTicks > advanceTicks) {
                advanceTicks += 60000 - dayTicks;
            } else {
                advanceTicks -= dayTicks;
            }

            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + advanceTicks);

            // Uniform weather
            map.weatherManager.curWeather = WeatherDefOf.Clear;
        } else {
            captureFinished = true;
        }
    }

    public override void Tick(int tick) {
        if (!isSeedFinding) return;

        if (needCapture) {
            needCapture = false;

            int curTile = Find.CurrentMap.Tile;

            Vector2 longlat = Find.WorldGrid.LongLatOf(curTile);

            string latitudePostfix = longlat.y >= 0f ? "N" : "S";
            string longitudePostfix = longlat.x >= 0f ? "E" : "W";

            string seedStr = filterParams.baseSeed;
            if (curSeedOffset > 1) {
                seedStr += curSeedOffset.ToString();
            }

            string path = Path.Combine(filterParams.outDirectory,
                                       string.Concat(seedStr, "_",
                                                     Math.Abs(longlat.y).ToString("F2"), latitudePostfix,
                                                     "_", Math.Abs(longlat.x).ToString("F2"), longitudePostfix, ".png"));

            Find.CameraDriver.StartCoroutine(RenderAndSave(Find.CurrentMap, path));

            numFound++;
        }

        if (captureFinished) {
            captureFinished = false;

            if (numFound < filterParams.maxFound) {
                visitNextMap();
            } else {
                stopFinding();
                GenScene.GoToMainMenu();
            }
        }
    }

    // This function is based on code from the Progress-Render mod, authored by Lanilor
    // LGPL 3 License
    private IEnumerator RenderAndSave(Map map, string path) {
        yield return new WaitForFixedUpdate();

        if (filterParams.highlightPOI) {
            foreach (var animaThing in Find.CurrentMap.listerThings.ThingsOfDef(ThingDefOf.Plant_TreeAnima)) {
                GenDraw.DrawRadiusRing(animaThing.Position, animaRadius, MeditationUtility.ArtificialBuildingRingColor);
            }

            foreach (var geyser in map.listerBuildings.AllBuildingsNonColonistOfDef(ThingDefOf.SteamGeyser)) {
                GenDraw.DrawRadiusRing(geyser.Position, 4f, new Color(0.8f, 0.1f, 0.6f), (IntVec3 v) => {
                    float geyserDistX = geyser.Position.x + 0.5f - (float)v.x;
                    float geyserDistZ = geyser.Position.z + 0.5f - (float)v.z;

                    float geyserDist = Mathf.Sqrt(geyserDistX * geyserDistX + geyserDistZ * geyserDistZ);

                    return geyserDist <= 3.5f;
                });
            }

            var fertilityDrawer = new CellBoolDrawer((int idx) => {
                var loc = CellIndicesUtility.IndexToCell(idx, map.Size.x);
                if (loc.Filled(map) || loc.Fogged(map)) {
                    return false;
                }

                return FertilityAt(map, loc) >= 1.1f;
            }, () => {
                return Color.white;
            }, (int idx) => {
                return Color.green;
            }, map.Size.z, map.Size.z, 3610);

            fertilityDrawer.MarkForDraw();
            fertilityDrawer.CellBoolDrawerUpdate();
        }

        CameraJumper.TryHideWorld();
        float startX = 0;
        float startZ = 0;
        float endX = map.Size.x;
        float endZ = map.Size.z;

        float distX = endX - startX;
        float distZ = endZ - startZ;

        float pixelsPerCell = 8f;
        int imageWidth = (int)(distX * pixelsPerCell);
        int imageHeight = (int)(distZ * pixelsPerCell);

        int RenderTextureSize = 4096;

        int renderCountX = (int)Math.Ceiling((float)imageWidth / RenderTextureSize);
        int renderCountZ = (int)Math.Ceiling((float)imageHeight / RenderTextureSize);
        int renderWidth = (int)Math.Ceiling((float)imageWidth / renderCountX);
        int renderHeight = (int)Math.Ceiling((float)imageHeight / renderCountZ);

        float cameraPosX = (float)distX / 2 / renderCountX;
        float cameraPosZ = (float)distZ / 2 / renderCountZ;
        float orthographicSize = Math.Min(cameraPosX, cameraPosZ);
        orthographicSize = cameraPosZ;
        Vector3 cameraBasePos = new Vector3(cameraPosX, 15f + (orthographicSize - 11f) / 49f * 50f, cameraPosZ);

        RenderTexture renderTexture = RenderTexture.GetTemporary(renderWidth, renderHeight, 24);
        Texture2D imageTexture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);

        Camera camera = Find.Camera;
        CameraDriver camDriver = camera.GetComponent<CameraDriver>();
        camDriver.enabled = false;

        // Store current camera data
        Vector3 rememberedRootPos = map.rememberedCameraPos.rootPos;
        float rememberedRootSize = map.rememberedCameraPos.rootSize;
        float rememberedFarClipPlane = camera.farClipPlane;

        // Overwrite current view rect in the camera driver
        CellRect camViewRect = camDriver.CurrentViewRect;
        int camRectMinX = Math.Min((int)startX, camViewRect.minX);
        int camRectMinZ = Math.Min((int)startZ, camViewRect.minZ);
        int camRectMaxX = Math.Max((int)Math.Ceiling(endX), camViewRect.maxX);
        int camRectMaxZ = Math.Max((int)Math.Ceiling(endZ), camViewRect.maxZ);
        Traverse camDriverTraverse = Traverse.Create(camDriver);
        camDriverTraverse.Field("lastViewRect").SetValue(CellRect.FromLimits(camRectMinX, camRectMinZ, camRectMaxX, camRectMaxZ));
        camDriverTraverse.Field("lastViewRectGetFrame").SetValue(Time.frameCount);
        yield return new WaitForEndOfFrame();

        // Set camera values needed for rendering
        camera.orthographicSize = orthographicSize;
        camera.farClipPlane = cameraBasePos.y + 6.5f;

        camera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;

        for (int i = 0; i < renderCountZ; i++)
        {
            for (int j = 0; j < renderCountX; j++)
            {
                camera.transform.position = new Vector3(startX + cameraBasePos.x * (2 * j + 1), cameraBasePos.y, startZ + cameraBasePos.z * (2 * i + 1));
                camera.Render();
                imageTexture.ReadPixels(new Rect(0, 0, renderWidth, renderHeight), renderWidth * j, renderHeight * i, false);
            }
        }

        // Restore camera and viewport
        RenderTexture.active = null;
        camera.targetTexture = null;
        camera.farClipPlane = rememberedFarClipPlane;
        camDriver.SetRootPosAndSize(rememberedRootPos, rememberedRootSize);
        camDriver.enabled = true;

        RenderTexture.ReleaseTemporary(renderTexture);

        byte[] png = imageTexture.EncodeToPNG();

        var fileInfo = new FileInfo(path);
        fileInfo.Directory.Create();
        File.WriteAllBytes(fileInfo.FullName, png);

        UnityEngine.Object.Destroy(imageTexture);

        captureFinished = true;
        yield break;
    }

    private void resetGame() {
        MemoryUtility.ClearAllMapsAndWorld();
        Current.Game = null;

        Current.Game = new Game();
        Current.Game.InitData = new GameInitData();

        // Make custom scenario that doesn't spawn any items
        var scen = new Scenario();
        scen.Category = ScenarioCategory.CustomLocal;
        scen.name = "SeedFinderScenario";
        scen.description = null;
        scen.summary = null;

        var scenFaction = (ScenPart_PlayerFaction)ScenarioMaker.MakeScenPart(ScenPartDefOf.PlayerFaction);
        typeof(ScenPart_PlayerFaction).GetField("factionDef", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(scenFaction, FactionDefOf.PlayerColony);
        typeof(Scenario).GetField("playerFaction", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(scen, scenFaction);

        var pawnConfig = (ScenPart_ConfigPage_ConfigureStartingPawns)ScenarioMaker.MakeScenPart(ScenPartDefOf.ConfigPage_ConfigureStartingPawns);
        pawnConfig.pawnCount = 1;
        pawnConfig.pawnChoiceCount = 8;

        var scenParts = new List<ScenPart>();

        scenParts.Add(pawnConfig);
        scenParts.Add(ScenarioMaker.MakeScenPart(ScenPartDefOf.PlayerPawnsArriveMethod));
        typeof(Scenario).GetField("parts", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(scen, scenParts);

        Current.Game.Scenario = scen;

        Find.Scenario.PreConfigure();
        Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Rough);
    }

    private void visitNextMap() {
        LongEventHandler.ClearQueuedEvents();
        LongEventHandler.QueueLongEvent(delegate {
            if (validTiles.Count == 0) {
                resetGame();
                LongEventHandler.QueueLongEvent(delegate {
                    while (validTiles.Count == 0) {
                        curSeedOffset++;
                        generateWorld();
                        filterTiles();
                    }

                    int curTile = validTiles.Pop();
                    Find.GameInitData.startingTile = curTile;
                    Find.GameInitData.mapSize = filterParams.mapSize;
                    Find.Scenario.PostIdeoChosen();
                    Find.GameInitData.PrepForMapGen();
                    Find.Scenario.PreMapGenerate();
                }, "Play", "SeedFinder.FindingSeeds", doAsynchronously: true, GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);
            } else {
                int oldTile = Find.CurrentMap.Tile;
                int curTile = validTiles.Pop();

                LongEventHandler.QueueLongEvent(delegate {
                    var world = Current.Game.World;
                    Current.Game.World = null;
                    MemoryUtility.ClearAllMapsAndWorld();
                    Current.Game.World = world;

                    foreach (WorldObject item in Find.WorldObjects.ObjectsAt(oldTile).ToList()) {
                        item.Destroy();
                    }

                    MemoryUtility.UnloadUnusedUnityAssets();

                    Settlement settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                    settlement.SetFaction(Faction.OfPlayer);
                    settlement.Tile = curTile;
                    settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement, Faction.OfPlayer.def.playerInitialSettlementNameMaker);
                    Find.WorldObjects.Add(settlement);
                    var map = GetOrGenerateMapUtility.GetOrGenerateMap(curTile, WorldObjectDefOf.Settlement);
                    Current.Game.CurrentMap = map;

                    CameraJumper.TryJump(MapGenerator.PlayerStartSpot, settlement.Map);
                }, "GeneratingMap", doAsynchronously: true, null);
            }
        }, "Finding Seeds", doAsynchronously: true, null, showExtraUIInfo: false);
    }

    private void generateWorld() {
        Find.GameInitData.ResetWorldRelatedMapInitData();
        string seedString = filterParams.baseSeed;
        if (curSeedOffset > 1) {
            seedString += curSeedOffset.ToString();
        }

        Current.Game.World = WorldGenerator.GenerateWorld(filterParams.planetCoverage, seedString,
                                                          filterParams.rainfall,
                                                          filterParams.temperature,
                                                          filterParams.population,
                                                          filterParams.factions,
                                                          filterParams.pollution);
    }

    private void filterTiles() {
        var allSettlements = Find.WorldObjects.Settlements;

        var outlanderSettlements = new List<Settlement>();
        var roughOutlanderSettlements = new List<Settlement>();

        var tribeSettlements = new List<Settlement>();
        var roughTribeSettlements = new List<Settlement>();

        var empireSettlements = new List<Settlement>();

        {
            var civilOutlander = Find.FactionManager.AllFactionsVisible.Where(
                (Faction f) => f.def == FactionDefOf.OutlanderCivil).FirstOrDefault();
            var roughOutlander = Find.FactionManager.AllFactionsVisible.Where(
                (Faction f) => f.def == FactionDefOf.OutlanderRough).FirstOrDefault();
            var civilTribe = Find.FactionManager.AllFactionsVisible.Where(
                (Faction f) => f.def == FactionDefOf.TribeCivil).FirstOrDefault();
            var roughTribe = Find.FactionManager.AllFactionsVisible.Where(
                (Faction f) => f.def == FactionDefOf.TribeRough).FirstOrDefault();
            var empire = Find.FactionManager.OfEmpire;

            foreach (var settlement in allSettlements) {
                if (settlement.Faction == null ||
                    settlement.Faction == Faction.OfPlayer ||
                    settlement.Faction.def.permanentEnemy) {
                    continue;
                }

                if (settlement.Faction == civilOutlander) {
                    outlanderSettlements.Add(settlement);
                }

                if (settlement.Faction == roughOutlander) {
                    roughOutlanderSettlements.Add(settlement);
                }

                if (settlement.Faction == civilTribe) {
                    tribeSettlements.Add(settlement);
                }

                if (settlement.Faction == roughTribe) {
                    roughTribeSettlements.Add(settlement);
                }

                if (settlement.Faction == empire) {
                    empireSettlements.Add(settlement);
                }

            }
        }

        var tileCount = Current.Game.World.grid.TilesCount;
        for (var tileID = 0; tileID < tileCount; tileID++) {
            var tile = Current.Game.World.grid[tileID];

            if (!TileFinder.IsValidTileForNewSettlement(tileID)) {
                continue;
            }

            if (tile.biome != filterParams.biome || tile.hilliness != filterParams.hilliness) {
                continue;
            }

            // Northern hemisphere
            if (filterParams.hemisphere != Hemisphere.Either) {
                float lat = Find.WorldGrid.LongLatOf(tileID).y;
                if (filterParams.hemisphere == Hemisphere.Northern && lat < 0) continue;
                if (filterParams.hemisphere == Hemisphere.Southern && lat > 0) continue;
            }

            if (filterParams.river != FeatureFilter.Either) {
                bool hasRiver = tile.Rivers != null && tile.Rivers.Count > 0;

                if (filterParams.river == FeatureFilter.NotPresent && hasRiver) continue;
                if (filterParams.river == FeatureFilter.Present) {
                    if (!hasRiver) continue;

                    var tileRiver = tile.Rivers.MaxBy((Tile.RiverLink riverlink) => riverlink.river.degradeThreshold).river;

                    bool desiredRiverFound = false;

                    for (int riverIdx = 0; riverIdx < allRivers.Count; riverIdx++) {
                        var riverDef = allRivers[riverIdx];
                        bool wantRiver = filterParams.desiredRivers[riverIdx];

                        if (riverDef == tileRiver && wantRiver) {
                            desiredRiverFound = true;
                            break;
                        }
                    }

                    if (!desiredRiverFound) continue;
                }
            }

            if (filterParams.coastal != FeatureFilter.Either) {
                var rot = Current.Game.World.CoastDirectionAt(tileID);

                if (filterParams.coastal == FeatureFilter.Present && !rot.IsValid) continue;
                if (filterParams.coastal == FeatureFilter.NotPresent && rot.IsValid) continue;
            }

            if (filterParams.caves != FeatureFilter.Either) {
                bool hasCaves = Find.World.HasCaves(tileID);

                if (filterParams.caves == FeatureFilter.Present && !hasCaves) continue;
                if (filterParams.caves == FeatureFilter.NotPresent && hasCaves) continue;
            }

            float maxTemp = GenTemperature.CelsiusTo(GenTemperature.MaxTemperatureAtTile(tileID),
                                                     Prefs.TemperatureMode);

            float minTemp = GenTemperature.CelsiusTo(GenTemperature.MinTemperatureAtTile(tileID),
                                                     Prefs.TemperatureMode);

            if (maxTemp > (float)filterParams.maxTemp || minTemp < (float)filterParams.minTemp) {
                continue;
            }

            int numGrowingDays = GenTemperature.TwelfthsInAverageTemperatureRange(tileID, 6f, 42f).Count * 5;

            if (numGrowingDays < filterParams.minGrowingDays) {
                continue;
            }

            var tileStones = Find.World.NaturalRockTypesIn(tileID).ToList();
            if (filterParams.firstStone != null && tileStones[0] != filterParams.firstStone) {
                continue;
            }

            if (filterParams.seasonality != Seasonality.Any) {
                Vector2 longlat = Find.WorldGrid.LongLatOf(tileID);
                Season season = SeasonUtility.GetReportedSeason(0, longlat.y);

                if (season == Season.PermanentSummer) {
                    if (filterParams.seasonality != Seasonality.PermSummer) {
                        continue;
                    }
                } else if (season == Season.PermanentWinter) {
                    if (filterParams.seasonality != Seasonality.PermWinter) {
                        continue;
                    }
                } else {
                    if (filterParams.seasonality != Seasonality.Normal) {
                        continue;
                    }
                }
            }

            bool requiredStoneMissing = false;

            for (int stoneIdx = 0; stoneIdx < allStones.Count; stoneIdx++) {
                var stoneDef = allStones[stoneIdx];
                var desired = filterParams.desiredStones[stoneIdx];

                if (!desired) continue;

                bool stoneFound = false;
                foreach (var tileStone in tileStones) {
                    if (tileStone == stoneDef) {
                        stoneFound = true;
                        break;
                    }
                }

                if (!stoneFound) {
                    requiredStoneMissing = true;
                    break;
                }
            }

            if (requiredStoneMissing) {
                continue;
            }

            Func<List<Settlement>, bool> searchSettlements = (List<Settlement> settlements) => {
                foreach (var settlement in settlements) {
                    int dist = Find.WorldGrid.TraversalDistanceBetween(tileID, settlement.Tile, passImpassable: true, 66);
                    if (dist != int.MaxValue) {
                        return true;
                    }
                }

                return false;
            };

            if (filterParams.needCivilOutlanderNear && !searchSettlements(outlanderSettlements)) continue;
            if (filterParams.needRoughOutlanderNear && !searchSettlements(roughOutlanderSettlements)) continue;
            if (filterParams.needCivilTribeNear && !searchSettlements(tribeSettlements)) continue;
            if (filterParams.needRoughTribeNear && !searchSettlements(roughTribeSettlements)) continue;
            if (filterParams.needEmpireNear && !searchSettlements(empireSettlements)) continue;

            validTiles.Push(tileID);
        }
    }
}
}
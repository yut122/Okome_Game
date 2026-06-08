using UnityEngine;

public class MarketManager : MonoBehaviour
{
    public int baseSellPrice = 500; // 基本売値 ¥/kg

    // 今年のパラメータ
    public string EconomyName   { get; private set; }
    public string EconomyDesc   { get; private set; }
    public string OpinionName   { get; private set; }
    public string OpinionDesc   { get; private set; }
    public string HarvestName   { get; private set; }
    public string HarvestDesc   { get; private set; }
    public int    TodaySellPrice { get; private set; }

    static readonly (string name, string desc, float multiplier)[] economyEvents =
    {
        ("景気：良い",   "個人消費が増加しています。ブランド志向が高まっています。", 1.3f),
        ("景気：普通",   "特に変わった動きはありません。",                           1.0f),
        ("景気：悪い",   "個人消費が低迷しています。節約志向が強まっています。",     0.75f),
    };

    static readonly (string name, string desc, float multiplier)[] opinionEvents =
    {
        ("おいしさ重視", "街の人々は今年、米のおいしさを特に重視しています。",       1.2f),
        ("普通が一番",   "街の人々は今年、普通のお米を好む傾向があります。",          1.0f),
        ("安さ重視",     "街の人々は今年、少しでも安いお米を求めています。",          0.85f),
    };

    static readonly (string name, string desc, float multiplier)[] harvestEvents =
    {
        ("豊作",           "今年は全国的に豊作です。市場に米が溢れています。",         0.65f),
        ("まあまあの収穫", "今年の収穫は平年並みです。",                               1.0f),
        ("不作",           "今年は全国的に不作です。米の流通量が減っています。",       1.5f),
    };

    public void GenerateDailyMarket()
    {
        var eco  = economyEvents [Random.Range(0, economyEvents.Length)];
        var opi  = opinionEvents [Random.Range(0, opinionEvents.Length)];
        var har  = harvestEvents [Random.Range(0, harvestEvents.Length)];

        EconomyName  = eco.name;  EconomyDesc  = eco.desc;
        OpinionName  = opi.name;  OpinionDesc  = opi.desc;
        HarvestName  = har.name;  HarvestDesc  = har.desc;

        TodaySellPrice = Mathf.RoundToInt(baseSellPrice * eco.multiplier * opi.multiplier * har.multiplier);
    }
}

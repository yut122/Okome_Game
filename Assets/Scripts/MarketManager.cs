using UnityEngine;

public class MarketManager : MonoBehaviour
{
    public int baseSellPrice = 250000; // 基本売値 ¥/袋

    // 今年のパラメータ（GenerateDailyMarket前に参照されてもNPEにならないよう空文字で初期化）
    public string EconomyName   { get; private set; } = "";
    public string EconomyDesc   { get; private set; } = "";
    public string OpinionName   { get; private set; } = "";
    public string OpinionDesc   { get; private set; } = "";
    public string HarvestName   { get; private set; } = "";
    public string HarvestDesc   { get; private set; } = "";
    public int    TodaySellPrice { get; private set; }

    // 今年の景気・収穫の倍率（仕入れ価格の算出にも使う）
    public float EconomyMultiplier { get; private set; } = 1f;
    public float HarvestMultiplier { get; private set; } = 1f;

    // 仕入れ価格の変動係数（景気と収穫数の平均。1.0前後で振れる）
    public float PurchaseFactor => (EconomyMultiplier + HarvestMultiplier) * 0.5f;

    // 売値マークアップ（仕入れ値に対する上乗せ率）。需要が高い年ほど高く売れる。
    // 最低でも1.25倍は確保し、売り切れれば利益が出るようにする。
    public float SellMarkup { get; private set; } = 1.5f;

    // どの画面より先に値を用意しておく（NewsScreenが先に有効化されても安全）
    void Awake() => GenerateDailyMarket();

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

        EconomyMultiplier = eco.multiplier;
        HarvestMultiplier = har.multiplier;

        // 売値マークアップ：基準1.5倍。景気・世論が良いと上がり、豊作だと下がる。
        float m = 1.5f;
        m += (eco.multiplier - 1f) * 0.5f;  // 景気
        m += (opi.multiplier - 1f) * 0.5f;  // 世論
        m += (har.multiplier - 1f) * 0.15f; // 収穫（不作=品薄で高く売れる）
        SellMarkup = Mathf.Clamp(m, 1.25f, 2.0f);

        TodaySellPrice = Mathf.RoundToInt(baseSellPrice * eco.multiplier * opi.multiplier * har.multiplier);
    }
}

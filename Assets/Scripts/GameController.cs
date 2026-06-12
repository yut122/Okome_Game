using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    // ── パラメータ ──────────────────────────────
    public JudgeManager  judge;
    public MarketManager market;
    public int initialMoney = 10000000; // 初期資金1000万（Inspectorで調整可・全計算の基準）
    public int money;                  // 実行時の所持金（initialMoneyから開始）
    public int stock      = 0;
    public int currentDay = 1;
    public int maxDays    = 5;

    // 将来フェーズ用（現在は未使用）
    [HideInInspector] public int reputation = 80;

    // 年間トラッキング
    [HideInInspector] public int yearSellRevenue  = 0; // SellControllerが書き込む
    int moneyAtYearStart = 0;

    public List<SupplierData> purchasedSuppliers = new List<SupplierData>();
    public List<SupplierData> reportedSuppliers  = new List<SupplierData>();

    [Header("UI テキスト")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI stockText;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI resultText;      // NightScreen：項目名（左）
    public TextMeshProUGUI resultValueText; // NightScreen：金額（右揃え）
    public TextMeshProUGUI summaryText;   // BuyScreen購入サマリー
    public GameObject      summaryBox;    // 購入サマリーの枠（背景パネル）
    public TextMeshProUGUI endingText;

    [Header("業者カード")]
    public SupplierCardUI[] supplierCards;

    [Header("BuyScreen ボタン")]
    public GameObject buyButton;
    public GameObject resetButton;
    public GameObject sellButton;

    [Header("画面")]
    public ScreenManager screenManager;

    [Header("エンディング演出")]
    public GameObject endingCelebrationGroup; // 成功時に表示（派手なお祝い）
    public GameObject endingSadGroup;         // 失敗時に表示（ザンネーン）

    // ── 内部状態 ────────────────────────────────
    int selectedIndex = -1;
    readonly List<(SupplierData supplier, int cost)> yearPurchases
        = new List<(SupplierData, int)>();

    // 今年の各業者の仕入れ価格（judge.suppliers と同じ並び）。
    // ScriptableObject資産を書き換えないよう、価格はここで保持する。
    int[] currentPrices = new int[0];

    // 今年の各業者の流通期限（「YYYY年M月まで」。毎年ランダムに変動）
    string[] currentExpiry = new string[0];

    // 今年の転売屋のインデックス（judge.suppliers内）。-1＝転売屋なし。
    // 3年目以降にランダムで1業者が転売屋になり、カードの業者名だけ「アヤシイ商店」表示になる。
    int resellerIndex = -1;
    public const string ResellerName = "アヤシイ商店";
    const int ResellerAppearFromYear = 3;

    public bool IsReseller(int index) => index == resellerIndex && resellerIndex >= 0;

    SupplierData ResellerSupplier =>
        (judge != null && resellerIndex >= 0 && resellerIndex < judge.suppliers.Count)
            ? judge.suppliers[resellerIndex] : null;

    // 今年、転売屋から仕入れてしまったか
    public bool BoughtFromReseller()
    {
        var r = ResellerSupplier;
        return r != null && purchasedSuppliers.Contains(r);
    }

    // お米の「表示順（左→右）」「基準価格（円）」「米ランク」。claimedRiceNameで対応付け。
    static readonly (string rice, int basePrice, string rank)[] riceConfig =
    {
        ("あまひかり",1400000,  "A"),  // 約140万前後（左）
        ("春小町",     800000,  "C"),  // 約80万前後（中）
        ("カリカリ米", 400000,  "D"),  // 約40万前後（右）
    };

    static int OrderIndex(SupplierData s)
    {
        if (s == null) return int.MaxValue;
        for (int i = 0; i < riceConfig.Length; i++)
            if (riceConfig[i].rice == s.claimedRiceName) return i;
        return int.MaxValue; // 未知のお米は末尾
    }

    static int BasePriceOf(SupplierData s)
    {
        if (s != null)
            foreach (var c in riceConfig)
                if (c.rice == s.claimedRiceName) return c.basePrice;
        return s != null ? s.pricePerBag : 0; // 未知のお米は資産の既定値
    }

    static string RankOf(SupplierData s)
    {
        if (s != null)
            foreach (var c in riceConfig)
                if (c.rice == s.claimedRiceName) return c.rank;
        return "-";
    }

    // 値札用：数字を大きく「万」を小さくしたリッチテキスト（例: 1400000 → "<b>140</b><size=58%>万</size>"）。
    public static string ManYenTag(int yen)
    {
        int man = yen / 10000;
        int rem = yen % 10000;
        string num = (rem == 0) ? man.ToString() : (yen / 10000f).ToString("0.#");
        return "<b>" + num + "</b><size=58%>万</size>";
    }

    // 円を「◯◯万円」表記へ（例: 10000000 → "1000万円"、400000 → "40万円"）。
    // 1万未満の端数があれば「◯万◯◯◯◯円」と表示する。
    public static string ManYen(int yen)
    {
        bool neg = yen < 0;
        int v = Mathf.Abs(yen);
        int man = v / 10000;
        int rem = v % 10000;

        string s;
        if      (man > 0 && rem == 0) s = man + "万円";
        else if (man > 0)             s = man + "万" + rem + "円";
        else                          s = rem + "円";

        return (neg ? "-" : "") + s;
    }

    // ════════════════════════════════════════════
    // 初期化
    // ════════════════════════════════════════════

    void Awake() => money = initialMoney;

    void Start() => InitYear();

    void InitYear()
    {
        selectedIndex     = -1;
        yearSellRevenue   = 0;
        moneyAtYearStart  = money;
        stock             = 0; // 初期フェーズでは在庫の持ち越しなし（毎年リセット）
        yearPurchases.Clear();
        purchasedSuppliers.Clear();

        market.GenerateDailyMarket();
        ReorderAndPriceSuppliers();
        UpdateUI();
        RefreshCards();
        UpdateBottomUI();
    }

    // 業者を表示順に並べ替え、今年の仕入れ価格を景気×収穫で算出する。
    void ReorderAndPriceSuppliers()
    {
        if (judge == null || judge.suppliers == null) return;

        // 1) 表示順（あまひかり→春小町→カリカリ米）に並べ替え
        judge.suppliers.Sort((a, b) => OrderIndex(a).CompareTo(OrderIndex(b)));

        // 2) 基準価格 × 仕入れ係数(景気・収穫) × 小さなランダム振れ で今年の価格を決定
        float factor = market != null ? market.PurchaseFactor : 1f;
        currentPrices = new int[judge.suppliers.Count];
        currentExpiry = new string[judge.suppliers.Count];
        for (int i = 0; i < judge.suppliers.Count; i++)
        {
            int   basePrice = BasePriceOf(judge.suppliers[i]);
            float raw       = basePrice * factor * Random.Range(0.95f, 1.05f);
            // 1万円単位に丸めて「○万」表記がきれいになるようにする
            currentPrices[i] = Mathf.Max(10000, Mathf.RoundToInt(raw / 10000f) * 10000);
            // 流通期限：年=2000+(x年目+1〜2)、月=1〜12 をランダムに設定
            int kYear  = 2000 + currentDay + Random.Range(1, 3);
            int kMonth = Random.Range(1, 13);
            currentExpiry[i] = kYear + "年" + kMonth + "月まで";
        }

        // 3) 3年目以降はランダムで1業者が転売屋「アヤシイ商店」になる
        if (currentDay >= ResellerAppearFromYear && judge.suppliers.Count > 0)
            resellerIndex = Random.Range(0, judge.suppliers.Count);
        else
            resellerIndex = -1;
    }

    int PriceAt(int index)
        => (index >= 0 && index < currentPrices.Length) ? currentPrices[index] : 0;

    string ExpiryStrAt(int index)
        => (index >= 0 && index < currentExpiry.Length && currentExpiry[index] != null)
               ? currentExpiry[index] : "-";

    // 今年仕入れた1個あたりの平均仕入れ値（売値の基準に使う）
    public int AverageUnitCost()
    {
        if (yearPurchases.Count == 0) return 0;
        int sum = 0;
        foreach (var p in yearPurchases) sum += p.cost;
        return sum / yearPurchases.Count;
    }

    // ════════════════════════════════════════════
    // カード管理
    // ════════════════════════════════════════════

    void RefreshCards()
    {
        if (supplierCards == null) return;
        for (int i = 0; i < supplierCards.Length; i++)
        {
            if (supplierCards[i] == null) continue;
            bool active = i < judge.suppliers.Count;
            supplierCards[i].gameObject.SetActive(active);
            if (active) supplierCards[i].Setup(judge.suppliers[i], i, this, PriceAt(i), IsReseller(i),
                                               RankOf(judge.suppliers[i]), ExpiryStrAt(i));
        }
    }

    public void OnSupplierCardSelected(int index)
    {
        if (index < 0 || index >= judge.suppliers.Count) return;
        selectedIndex = index;
        for (int i = 0; i < supplierCards.Length; i++)
            if (supplierCards[i] != null)
                supplierCards[i].SetSelected(i == selectedIndex);
        UpdateBottomUI();
    }

    // ════════════════════════════════════════════
    // ボタン処理
    // ════════════════════════════════════════════

    public void OnBuyButton()
    {
        if (selectedIndex < 0) return;
        var s    = judge.suppliers[selectedIndex];
        int cost = PriceAt(selectedIndex); // 1回のBuyで1個だけ仕入れる
        if (money < cost) return;

        money -= cost;
        stock += 1;
        purchasedSuppliers.Add(s);
        yearPurchases.Add((s, cost));

        UpdateUI();
        UpdateBottomUI();
    }

    public void OnResetButton()
    {
        foreach (var p in yearPurchases)
        {
            money += p.cost;
            stock -= 1;
        }
        purchasedSuppliers.RemoveRange(
            purchasedSuppliers.Count - yearPurchases.Count,
            yearPurchases.Count);
        yearPurchases.Clear();

        selectedIndex = -1;
        foreach (var c in supplierCards)
            if (c != null) c.SetSelected(false);

        UpdateUI();
        UpdateBottomUI();
    }

    public void OnSellButton()
    {
        if (screenManager != null) screenManager.ShowSell();
    }

    // ════════════════════════════════════════════
    // UI 更新
    // ════════════════════════════════════════════

    void UpdateBottomUI()
    {
        bool hasSelection = selectedIndex >= 0;
        bool hasPurchase  = yearPurchases.Count > 0;
        bool canAfford    = false;

        if (hasSelection && selectedIndex < judge.suppliers.Count)
        {
            canAfford = money >= PriceAt(selectedIndex);
        }

        SetActive(buyButton,   hasSelection && canAfford);
        SetActive(resetButton, hasPurchase);
        SetActive(sellButton,  true); // 販売ボタンはBuyScreen中つねに表示

        if (summaryBox != null) summaryBox.SetActive(hasPurchase);
        if (summaryText != null && hasPurchase) summaryText.text = BuildSummaryText();
    }

    string BuildSummaryText()
    {
        var dict = new Dictionary<string, (int bags, int cost)>();
        foreach (var p in yearPurchases)
        {
            string key = p.supplier.claimedRiceName;
            if (dict.ContainsKey(key))
                dict[key] = (dict[key].bags + 1, dict[key].cost + p.cost);
            else
                dict[key] = (1, p.cost);
        }

        int total = 0;
        var sb = new System.Text.StringBuilder();
        foreach (var kv in dict)
        {
            sb.Append(kv.Key).Append(" × ").Append(kv.Value.bags).Append("個\n");
            total += kv.Value.cost;
        }
        sb.Append("合計：").Append(ManYen(total));
        return sb.ToString();
    }

    static void SetActive(GameObject obj, bool v) { if (obj != null) obj.SetActive(v); }

    public void UpdateUIPublic() => UpdateUI();

    void UpdateUI()
    {
        if (moneyText != null) moneyText.text = "所持金：" + ManYen(money);
        if (stockText != null) stockText.text = "在庫：" + stock + "個";
        if (dayText   != null) dayText.text   = currentDay + "年目";
    }

    // ════════════════════════════════════════════
    // 画面遷移
    // ════════════════════════════════════════════

    public void OnSellComplete()
    {
        ProcessNightResult();
        if (screenManager != null) screenManager.ShowNight();
    }

    int lastAdvanceFrame = -1; // 同一フレームでの二重発火を防ぐ

    public void OnNextDayButton()
    {
        // ボタンに配線が重複していても1クリックで1回しか進めない
        if (Time.frameCount == lastAdvanceFrame) return;
        lastAdvanceFrame = Time.frameCount;

        currentDay++;
        if (currentDay > maxDays) { ShowEnding(); return; }
        judge.ResetSuppliers();
        InitYear();
        if (screenManager != null) screenManager.ShowNews();
    }

    // ════════════════════════════════════════════
    // エンディング
    // ════════════════════════════════════════════

    void ShowEnding()
    {
        int totalProfit = money - initialMoney; // 初期資金（1000万）との差
        string trend    = totalProfit >= 0 ? "+" : "-";
        bool   success  = money >= initialMoney; // 1000万を基準に成功判定

        string msg =
            "【5年間の経営結果】\n\n" +
            "最終所持金：" + ManYen(money) + "\n" +
            "初期資金比：" + trend + ManYen(Mathf.Abs(totalProfit)) + "\n\n";

        if (money >= initialMoney * 2)
            msg += "★☆ 大 成 功 ☆★\nあなたは伝説の米商人だ！街中が祝福しています！";
        else if (success)
            msg += "『 成 功 』\n見事な経営でした。元手を増やしましたね！";
        else
            msg += "… ザ ン ネ ー ン …\n惜しくも元手割れ。次こそリベンジだ！";

        if (endingText != null) endingText.text = msg;

        // 成功＝派手にお祝い、失敗＝しょんぼり演出を切り替え
        if (endingCelebrationGroup != null) endingCelebrationGroup.SetActive(success);
        if (endingSadGroup         != null) endingSadGroup.SetActive(!success);

        if (screenManager != null) screenManager.ShowEnding();
    }

    // エンディングの「もう一度」ボタンから呼ぶ：最初からやり直す
    public void RestartGame()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
    }

    // ════════════════════════════════════════════
    // 夜の結果処理
    // ════════════════════════════════════════════

    public void ProcessNightResult()
    {
        // ── 損益計算 ──
        int purchaseCost = 0;
        foreach (var p in yearPurchases) purchaseCost += p.cost;

        int    netProfit   = yearSellRevenue - purchaseCost;
        string profitSign  = netProfit >= 0 ? "＋" : "−";
        string profitLabel = netProfit >= 0 ? "今年の利益" : "今年の損失";

        // ── 左側：項目名（左揃え・タイトル後は1文字字下げ）──
        string labels =
            currentDay + "年目の結果\n" +
            "\n" +
            "　仕入れ費用\n" +
            "　売　　上\n" +
            "\n" +
            "　" + profitLabel;

        // 転売屋から仕入れてしまった場合の警告
        if (BoughtFromReseller())
            labels += "\n\n！？ 転売屋（" + ResellerName + "）から\n　仕入れてしまった！？\n　お客さんの購入率が大きく下がった…";

        // 損益に応じた一言（利益→褒め言葉／損失→励まし）
        string comment;
        if      (netProfit > 0) comment = "お見事！今年はしっかり儲けが出たね。";
        else if (netProfit < 0) comment = "今年は赤字…でも大丈夫、次こそ巻き返そう！";
        else                    comment = "ちょうどトントン。次は利益を狙っていこう。";
        labels += "\n\n" + comment;

        if (currentDay >= maxDays) labels += "\n\n「つぎへ」でエンディングへ";

        // ── 右側：金額（右揃え）──
        string values =
            "\n" +   // タイトル行をスキップ
            "\n" +   // 空行をスキップ
            "-" + ManYen(purchaseCost) + "\n" +
            ManYen(yearSellRevenue) + "\n" +
            "\n" +
            profitSign + ManYen(Mathf.Abs(netProfit));

        if (resultText      != null) resultText.text      = labels;
        if (resultValueText != null) resultValueText.text = values;

        // リセット
        yearPurchases.Clear();
        purchasedSuppliers.Clear();
        reportedSuppliers.Clear();
    }
}

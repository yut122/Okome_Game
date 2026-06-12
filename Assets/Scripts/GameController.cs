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

    // お米の「表示順（左→右）」と「基準価格（円）」。claimedRiceNameで対応付け。
    static readonly (string rice, int basePrice)[] riceConfig =
    {
        ("ヤスノヒカリ", 400000),  // 約40万前後
        ("ひとめほれ",  1000000),  // 約100万前後
        ("あきたこひめ",1400000),  // 約140万前後
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

    // 円を「万」表記へ（例: 200000 → "20万"）。
    public static string ToMan(int yen)
    {
        if (yen % 10000 == 0) return (yen / 10000) + "万";
        return (yen / 10000f).ToString("0.#") + "万";
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
        yearPurchases.Clear();

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

        // 1) 表示順（ヤスノヒカリ→ひとめほれ→あきたこひめ）に並べ替え
        judge.suppliers.Sort((a, b) => OrderIndex(a).CompareTo(OrderIndex(b)));

        // 2) 基準価格 × 仕入れ係数(景気・収穫) × 小さなランダム振れ で今年の価格を決定
        float factor = market != null ? market.PurchaseFactor : 1f;
        currentPrices = new int[judge.suppliers.Count];
        for (int i = 0; i < judge.suppliers.Count; i++)
        {
            int   basePrice = BasePriceOf(judge.suppliers[i]);
            float raw       = basePrice * factor * Random.Range(0.95f, 1.05f);
            // 1万円単位に丸めて「○万」表記がきれいになるようにする
            currentPrices[i] = Mathf.Max(10000, Mathf.RoundToInt(raw / 10000f) * 10000);
        }
    }

    int PriceAt(int index)
        => (index >= 0 && index < currentPrices.Length) ? currentPrices[index] : 0;

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
            if (active) supplierCards[i].Setup(judge.suppliers[i], i, this, PriceAt(i));
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
        sb.Append("合計：¥").Append(ToMan(total));
        return sb.ToString();
    }

    static void SetActive(GameObject obj, bool v) { if (obj != null) obj.SetActive(v); }

    public void UpdateUIPublic() => UpdateUI();

    void UpdateUI()
    {
        if (moneyText != null) moneyText.text = "所持金：¥" + money.ToString("N0");
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

    public void OnNextDayButton()
    {
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
        string trend    = totalProfit >= 0 ? "+" : "";
        bool   success  = money >= initialMoney; // 1000万を基準に成功判定

        string msg =
            "【5年間の経営結果】\n\n" +
            "最終所持金：¥" + money.ToString("N0") + "\n" +
            "初期資金比：" + trend + totalProfit.ToString("N0") + "円\n\n";

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
            "-¥" + purchaseCost.ToString("N0") + "\n" +
            "¥" + yearSellRevenue.ToString("N0") + "\n" +
            "\n" +
            profitSign + "¥" + Mathf.Abs(netProfit).ToString("N0");

        if (resultText      != null) resultText.text      = labels;
        if (resultValueText != null) resultValueText.text = values;

        // リセット
        yearPurchases.Clear();
        purchasedSuppliers.Clear();
        reportedSuppliers.Clear();
    }
}

using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    // ── パラメータ ──────────────────────────────
    public JudgeManager  judge;
    public MarketManager market;
    public int money      = 10000000;
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

    // ── 内部状態 ────────────────────────────────
    int selectedIndex = -1;
    readonly List<(SupplierData supplier, int cost)> yearPurchases
        = new List<(SupplierData, int)>();

    // ════════════════════════════════════════════
    // 初期化
    // ════════════════════════════════════════════

    void Start() => InitYear();

    void InitYear()
    {
        selectedIndex     = -1;
        yearSellRevenue   = 0;
        moneyAtYearStart  = money;
        yearPurchases.Clear();

        market.GenerateDailyMarket();
        UpdateUI();
        RefreshCards();
        UpdateBottomUI();
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
            if (active) supplierCards[i].Setup(judge.suppliers[i], i, this);
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
        int cost = s.pricePerBag; // 1回のBuyで1袋だけ仕入れる
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
            var s  = judge.suppliers[selectedIndex];
            canAfford = money >= s.pricePerBag;
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
            sb.Append(kv.Key).Append(" × ").Append(kv.Value.bags).Append("袋\n");
            total += kv.Value.cost;
        }
        sb.Append("合計：¥").Append(total.ToString("N0"));
        return sb.ToString();
    }

    static void SetActive(GameObject obj, bool v) { if (obj != null) obj.SetActive(v); }

    public void UpdateUIPublic() => UpdateUI();

    void UpdateUI()
    {
        if (moneyText != null) moneyText.text = "所持金：¥" + money.ToString("N0");
        if (stockText != null) stockText.text = "在庫：" + stock + "袋";
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
        int totalProfit = money - 100000; // 初期資金との差
        string trend    = totalProfit >= 0 ? "+" : "";
        string msg =
            "【5年間の経営結果】\n\n" +
            "最終所持金：¥" + money.ToString("N0") + "\n" +
            "初期資金比：" + trend + totalProfit.ToString("N0") + "円\n\n";

        if (money >= 200000)
            msg += "【大成功】\nあなたは優れた米商人として街中に名を広めました。";
        else if (money >= 100000)
            msg += "【堅実経営】\n5年間、着実に経営を続けました。";
        else
            msg += "【苦しい経営】\n厳しい5年間でした。来年こそは。";

        if (endingText    != null) endingText.text = msg;
        if (screenManager != null) screenManager.ShowEnding();
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

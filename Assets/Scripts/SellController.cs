using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 販売フェーズ：お客さんが1人ずつ来店するアニメーションと購入判定を管理する
/// </summary>
public class SellController : MonoBehaviour
{
    // ── ステージ要素 ──────────────────────────────────────
    [Header("お客さんアニメーション")]
    public RectTransform customerRT;      // お客さん全体コンテナ
    public RectTransform carryBagRT;      // 持ち帰り袋コンテナ
    public RectTransform bubbleRT;        // 吹き出しコンテナ
    public TextMeshProUGUI bubbleText;    // 吹き出しテキスト
    public Image bubbleBg;                // 吹き出し背景（色変更用）

    [Header("お米袋の名前テキスト（全袋共通）")]
    public TextMeshProUGUI[] riceBagNameTexts; // bagName0〜4

    [Header("進捗ドット（最大12個）")]
    public Image[] progressDots;           // ドットのImage配列

    [Header("テキスト・ボタン")]
    public TextMeshProUGUI runningTotalText;
    public GameObject nextButton;          // 「つぎへ」初期非表示

    [Header("参照")]
    public GameController gameController;
    public MarketManager  marketManager;
    public JudgeManager   judgeManager;

    // ── アニメーション定数 ──────────────────────────────
    // ステージ内ローカル座標（Stageパネルの中心基準、bottom-left anchor想定）
    const float CUST_START_X   =  640f; // 右端（画面外）
    const float CUST_COUNTER_X = -148f; // カウンター前停止位置
    const float CUST_Y         = -129f; // キャラコンテナ中心Y（BuildCustomerの底辺-26から算出）
    const float CARRY_OFFSET   =   38f; // 袋の横オフセット
    const float CARRY_Y        = -139f; // 袋のY（CUST_Yより少し低め）
    const float BUBBLE_Y       =  -59f; // 吹き出しY（キャラ頭上 CUST_Y+70=−59）

    // 色
    static readonly Color ColorBuy      = new Color(0.22f, 0.72f, 0.22f, 1f);
    static readonly Color ColorNoBuy    = new Color(0.70f, 0.70f, 0.70f, 1f);
    static readonly Color ColorDotActive= new Color(1.0f,  0.75f, 0.35f, 1f);
    static readonly Color ColorDotOk    = new Color(0.91f, 0.52f, 0.10f, 1f);
    static readonly Color ColorDotNg    = new Color(0.60f, 0.60f, 0.60f, 1f);
    static readonly Color ColorDotEmpty = new Color(0.85f, 0.80f, 0.75f, 0.5f);

    int   totalSoldBags  = 0;
    int   totalRevenue = 0;
    int   todayUnitPrice = 0; // 今年の1個あたり売値（平均仕入れ値×マークアップ）

    // ════════════════════════════════════════════════════════
    void OnEnable()
    {
        totalSoldBags  = 0;
        totalRevenue = 0;
        gameController.yearSellRevenue = 0;

        SetActive(customerRT?.gameObject, false);
        SetActive(carryBagRT?.gameObject, false);
        SetActive(bubbleRT?.gameObject,   false);
        SetActive(nextButton,             false);
        if (runningTotalText) runningTotalText.text = "";

        // お米袋の名前を購入済み品種名に設定
        UpdateRiceBagNames();

        // ドットをすべて非表示に
        HideAllDots();

        StartCoroutine(SellPhaseCoroutine());
    }

    void OnDisable() => StopAllCoroutines();

    // ════════════════════════════════════════════════════════
    // お米袋の名前を購入済み品種に更新
    // ════════════════════════════════════════════════════════
    void UpdateRiceBagNames()
    {
        if (riceBagNameTexts == null) return;
        string name = "ヤスノヒカリ";
        if (gameController.purchasedSuppliers != null &&
            gameController.purchasedSuppliers.Count > 0)
            name = gameController.purchasedSuppliers[0].claimedRiceName;

        foreach (var t in riceBagNameTexts)
            if (t != null) t.text = name;
    }

    // ════════════════════════════════════════════════════════
    // メインコルーチン
    // ════════════════════════════════════════════════════════
    IEnumerator SellPhaseCoroutine()
    {
        yield return new WaitForSeconds(0.8f);

        int   count   = CalculateCustomerCount();
        float chance  = CalculateBuyChance();

        // 今年の売値：平均仕入れ値 × マークアップ（売り切れれば利益が出る）
        int avgCost   = gameController.AverageUnitCost();
        todayUnitPrice = Mathf.Max(1, Mathf.RoundToInt(avgCost * marketManager.SellMarkup));

        // ドットを来客数分だけ表示
        ShowDots(count);

        // 1人につき最大1個。完売したら即終了する。
        for (int i = 0; i < count; i++)
        {
            if (gameController.stock <= 0) break; // 完売：即終了
            SetDot(i, "active");

            yield return StartCoroutine(CustomerAnimation(i, chance));

            yield return new WaitForSeconds(0.25f);
        }

        // 売上を所持金に反映
        gameController.money += totalRevenue;
        gameController.UpdateUIPublic();

        SetActive(nextButton, true);
    }

    // ════════════════════════════════════════════════════════
    // 1人のお客さんアニメーション
    // ════════════════════════════════════════════════════════
    IEnumerator CustomerAnimation(int idx, float buyChance)
    {
        // ── 初期化 ──
        if (customerRT) { customerRT.anchoredPosition = new Vector2(CUST_START_X, CUST_Y); }
        if (carryBagRT) { carryBagRT.anchoredPosition = new Vector2(CUST_START_X + CARRY_OFFSET, CARRY_Y); }
        SetActive(customerRT?.gameObject, true);
        SetActive(carryBagRT?.gameObject, false);
        SetActive(bubbleRT?.gameObject,   false);
        if (bubbleText) bubbleText.text = "・・・";
        if (bubbleBg)   bubbleBg.color  = ColorNoBuy;

        // ── カウンターへ移動 ──
        yield return StartCoroutine(LerpX(customerRT, CUST_START_X, CUST_COUNTER_X, 1.1f));

        // ── 吹き出し表示（間） ──
        if (bubbleRT)
        {
            bubbleRT.anchoredPosition = new Vector2(CUST_COUNTER_X + 20f, BUBBLE_Y);
            SetActive(bubbleRT.gameObject, true);
        }
        yield return new WaitForSeconds(0.75f);

        // ── 購入判定（1人につき最大1個）──
        int actualBags = Mathf.Min(1, gameController.stock);
        bool bought  = Random.value < buyChance && actualBags > 0;

        if (bought)
        {
            int revenue   = actualBags * todayUnitPrice;
            totalSoldBags  += actualBags;
            totalRevenue += revenue;
            gameController.stock           -= actualBags;
            gameController.yearSellRevenue += revenue;
            gameController.UpdateUIPublic();

            if (bubbleText) bubbleText.text = "購入！　" + actualBags + "個";
            if (bubbleBg)   bubbleBg.color  = ColorBuy;
            SetDot(idx, "ok");

            if (runningTotalText)
                runningTotalText.text =
                    "売上：¥" + totalRevenue.ToString("N0") +
                    "　残在庫：" + gameController.stock + "個";

            // 袋を手に持つ
            SetActive(carryBagRT?.gameObject, true);
            if (carryBagRT)
                carryBagRT.anchoredPosition = new Vector2(CUST_COUNTER_X + CARRY_OFFSET, CARRY_Y);
        }
        else
        {
            if (bubbleText) bubbleText.text = "今日は結構です";
            if (bubbleBg)   bubbleBg.color  = ColorNoBuy;
            SetDot(idx, "ng");
        }

        yield return new WaitForSeconds(0.85f);

        // ── 吹き出しを消して退場 ──
        SetActive(bubbleRT?.gameObject, false);

        float exitX = CUST_START_X + 50f;
        if (bought)
        {
            // 袋と一緒に退場
            yield return StartCoroutine(LerpXBoth(
                customerRT, carryBagRT,
                CUST_COUNTER_X, exitX,
                CUST_COUNTER_X + CARRY_OFFSET, exitX + CARRY_OFFSET,
                1.0f));
        }
        else
        {
            yield return StartCoroutine(LerpX(customerRT, CUST_COUNTER_X, exitX, 0.9f));
        }

        SetActive(customerRT?.gameObject, false);
        SetActive(carryBagRT?.gameObject, false);
    }

    // ════════════════════════════════════════════════════════
    // 来客数・購入確率計算
    // ════════════════════════════════════════════════════════
    int CalculateCustomerCount()
    {
        int n = 5;
        if      (marketManager.EconomyName.Contains("良い")) n += 3;
        else if (marketManager.EconomyName.Contains("悪い")) n -= 2;
        if      (marketManager.HarvestName.Contains("不作")) n -= 1;
        else if (marketManager.HarvestName.Contains("豊作")) n += 1;
        n += Random.Range(-2, 3);
        return Mathf.Clamp(n, 1, 12);
    }

    float CalculateBuyChance()
    {
        float c = 0.60f;
        if      (marketManager.EconomyName.Contains("良い"))     c += 0.15f;
        else if (marketManager.EconomyName.Contains("悪い"))     c -= 0.20f;
        if      (marketManager.OpinionName.Contains("おいしさ")) c += 0.10f;
        else if (marketManager.OpinionName.Contains("安さ"))     c -= 0.10f;
        if      (marketManager.HarvestName.Contains("不作"))     c += 0.08f;
        else if (marketManager.HarvestName.Contains("豊作"))     c -= 0.08f;

        // 違反品を仕入れた場合は購入率が下がる
        foreach (var s in gameController.purchasedSuppliers)
        {
            if (judgeManager.CheckViolation(s) != "") { c -= 0.15f; break; }
        }
        return Mathf.Clamp(c, 0.10f, 0.90f);
    }

    // ════════════════════════════════════════════════════════
    // ドット管理
    // ════════════════════════════════════════════════════════
    void HideAllDots()
    {
        if (progressDots == null) return;
        foreach (var d in progressDots)
            if (d != null) { d.gameObject.SetActive(false); d.color = ColorDotEmpty; }
    }

    void ShowDots(int count)
    {
        if (progressDots == null) return;
        for (int i = 0; i < progressDots.Length; i++)
        {
            if (progressDots[i] == null) continue;
            progressDots[i].gameObject.SetActive(i < count);
            progressDots[i].color = ColorDotEmpty;
        }
    }

    void SetDot(int idx, string state)
    {
        if (progressDots == null || idx >= progressDots.Length || progressDots[idx] == null) return;
        progressDots[idx].color = state switch
        {
            "active" => ColorDotActive,
            "ok"     => ColorDotOk,
            "ng"     => ColorDotNg,
            _        => ColorDotEmpty
        };
    }

    // ════════════════════════════════════════════════════════
    // アニメーションヘルパー
    // ════════════════════════════════════════════════════════
    IEnumerator LerpX(RectTransform rt, float fromX, float toX, float dur)
    {
        if (rt == null) yield break;
        float t = 0;
        Vector2 pos = rt.anchoredPosition;
        while (t < dur)
        {
            t += Time.deltaTime;
            pos.x = Mathf.Lerp(fromX, toX, t / dur);
            rt.anchoredPosition = pos;
            yield return null;
        }
        pos.x = toX;
        rt.anchoredPosition = pos;
    }

    IEnumerator LerpXBoth(RectTransform rt1, RectTransform rt2,
        float from1, float to1, float from2, float to2, float dur)
    {
        float t = 0;
        Vector2 p1 = rt1 ? rt1.anchoredPosition : Vector2.zero;
        Vector2 p2 = rt2 ? rt2.anchoredPosition : Vector2.zero;
        while (t < dur)
        {
            t += Time.deltaTime;
            float ratio = t / dur;
            if (rt1) { p1.x = Mathf.Lerp(from1, to1, ratio); rt1.anchoredPosition = p1; }
            if (rt2) { p2.x = Mathf.Lerp(from2, to2, ratio); rt2.anchoredPosition = p2; }
            yield return null;
        }
        if (rt1) { p1.x = to1; rt1.anchoredPosition = p1; }
        if (rt2) { p2.x = to2; rt2.anchoredPosition = p2; }
    }

    static void SetActive(GameObject obj, bool v) { if (obj != null) obj.SetActive(v); }
}

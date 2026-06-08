using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public JudgeManager   judge;
    public MarketManager  market;
    public int money      = 100000;
    public int reputation = 80;
    public int stock      = 0;
    public int currentDay = 1;
    public int maxDays    = 5;
    public List<SupplierData> purchasedSuppliers = new List<SupplierData>();
    public List<SupplierData> reportedSuppliers  = new List<SupplierData>();

    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI stockText;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI endingText;
    public SupplierDisplay supplierDisplay;

    public GameObject actionButtons;
    public GameObject endDayButton;
    public ScreenManager screenManager;

    void Start()
    {
        market.GenerateDailyMarket();
        UpdateUI();
        ShowCurrentSupplier();
        if (endDayButton != null) endDayButton.SetActive(false);
    }

    void ShowCurrentSupplier()
    {
        if (supplierDisplay != null)
            supplierDisplay.ShowSupplier(judge.currentSupplier, judge.bannedOrigin);
    }

    void AfterDecision()
    {
        if (judge.IsLastSupplier())
        {
            if (actionButtons != null) actionButtons.SetActive(false);
            if (endDayButton != null) endDayButton.SetActive(true);
            if (resultText != null) resultText.text = "本日の業者は全員対応しました。\n「本日終了」を押してください。";
        }
        else
        {
            judge.NextSupplier();
            ShowCurrentSupplier();
        }
    }

    public void UpdateUIPublic() => UpdateUI();

    void UpdateUI()
    {
        if (moneyText != null) moneyText.text = "所持金：¥" + money.ToString("N0");
        if (stockText != null) stockText.text = "在庫：" + stock + "kg";
        if (dayText != null)   dayText.text   = currentDay + "年目";
    }

    public void OnBuyButton()
    {
        stock += judge.currentSupplier.volumeKg;
        money -= judge.currentSupplier.pricePerKg * judge.currentSupplier.volumeKg;
        purchasedSuppliers.Add(judge.currentSupplier);
        if (resultText != null) resultText.text = judge.currentSupplier.supplierName + " から仕入れました";
        UpdateUI();
        AfterDecision();
    }

    public void OnRefuseButton()
    {
        if (resultText != null) resultText.text = judge.currentSupplier.supplierName + " を断りました";
        AfterDecision();
    }

    public void OnReportButton()
    {
        reportedSuppliers.Add(judge.currentSupplier);
        if (resultText != null) resultText.text = judge.currentSupplier.supplierName + " を通報しました";
        AfterDecision();
    }

    // 仕入れ終了 → 販売画面へ
    public void OnEndDayButton()
    {
        if (screenManager != null) screenManager.ShowSell();
    }

    // 販売完了 → 夜の結果へ
    public void OnSellComplete()
    {
        ProcessNightResult();
        if (screenManager != null) screenManager.ShowNight();
    }

    public void OnNextDayButton()
    {
        currentDay++;

        if (currentDay > maxDays)
        {
            ShowEnding();
            return;
        }

        market.GenerateDailyMarket();
        judge.ResetSuppliers();
        if (actionButtons != null) actionButtons.SetActive(true);
        if (endDayButton != null) endDayButton.SetActive(false);
        UpdateUI();
        ShowCurrentSupplier();
        if (screenManager != null) screenManager.ShowNews();
    }

    void ShowEnding()
    {
        string endingMessage;

        if (reputation >= 70)
        {
            endingMessage =
                "【正義の米屋】\n\n" +
                "5年間、あなたは食の安全を守り続けました。\n\n" +
                "お店の評判は街中に広まり、\n" +
                "常連客がさらに増えていきました。\n\n" +
                "最終評判：" + reputation + "%\n" +
                "所持金：¥" + money.ToString("N0");
        }
        else if (reputation < 30)
        {
            endingMessage =
                "【廃業】\n\n" +
                "偽装米の噂が広まり、\n" +
                "お客さんが誰も来なくなりました。\n\n" +
                "あなたのお店は静かに幕を閉じました。\n\n" +
                "最終評判：" + reputation + "%\n" +
                "所持金：¥" + money.ToString("N0");
        }
        else
        {
            endingMessage =
                "【生活優先】\n\n" +
                "正直に生きることと、\n" +
                "生活を守ることの間で揺れながら、\n" +
                "あなたは5年間を乗り越えました。\n\n" +
                "最終評判：" + reputation + "%\n" +
                "所持金：¥" + money.ToString("N0");
        }

        if (endingText != null) endingText.text = endingMessage;
        if (screenManager != null) screenManager.ShowEnding();
    }

    public void ProcessNightResult()
    {
        string nightLog = currentDay + "年目の結果\n\n";

        foreach (SupplierData supplier in purchasedSuppliers)
        {
            judge.currentSupplier = supplier;
            string violation = judge.CheckViolation();
            if (violation != "")
            {
                reputation = Mathf.Clamp(reputation - 20, 0, 100);
                nightLog += "× " + supplier.supplierName + "：" + violation + "　評判-20\n";
            }
            else
            {
                nightLog += "○ " + supplier.supplierName + "：問題なし\n";
            }
        }

        foreach (SupplierData supplier in reportedSuppliers)
        {
            judge.currentSupplier = supplier;
            string violation = judge.CheckViolation();
            if (violation != "")
            {
                reputation = Mathf.Clamp(reputation + 10, 0, 100);
                nightLog += "○ 通報正解：" + supplier.supplierName + "　評判+10\n";
            }
            else
            {
                reputation = Mathf.Clamp(reputation - 10, 0, 100);
                nightLog += "× 誤報：" + supplier.supplierName + "　評判-10\n";
            }
        }

        nightLog += "\n所持金：¥" + money.ToString("N0") + "　在庫：" + stock + "kg　評判：" + reputation + "%";

        if (currentDay >= maxDays)
            nightLog += "\n\n「つぎへ」を押して結果へ";

        if (resultText != null) resultText.text = nightLog;

        purchasedSuppliers.Clear();
        reportedSuppliers.Clear();
    }
}

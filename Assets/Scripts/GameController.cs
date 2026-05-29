using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public JudgeManager judge;
    public int money = 100000;
    public int reputation = 80;
    public int stock = 0;
    public List<SupplierData> purchasedSuppliers = new List<SupplierData>();
    public List<SupplierData> reportedSuppliers = new List<SupplierData>();

    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI stockText;
    public TextMeshProUGUI resultText;
    public SupplierDisplay supplierDisplay;

    public GameObject actionButtons;   // 仕入れ・断る・通報ボタングループ
    public GameObject endDayButton;    // 本日終了ボタン
    public ScreenManager screenManager;

    void Start()
    {
        UpdateUI();
        ShowCurrentSupplier();
        if (endDayButton != null) endDayButton.SetActive(false);
    }

    void ShowCurrentSupplier()
    {
        if (supplierDisplay != null)
            supplierDisplay.ShowSupplier(judge.currentSupplier);
    }

    void AfterDecision()
    {
        if (judge.IsLastSupplier())
        {
            // 全業者対応済み → 終了ボタンを表示
            if (actionButtons != null) actionButtons.SetActive(false);
            if (endDayButton != null) endDayButton.SetActive(true);
            if (resultText != null) resultText.text = "本日の業者は全員対応しました。\n「本日終了」を押してください。";
        }
        else
        {
            // 次の業者へ
            judge.NextSupplier();
            ShowCurrentSupplier();
        }
    }

    void UpdateUI()
    {
        if (moneyText != null) moneyText.text = "所持金：¥" + money.ToString("N0");
        if (stockText != null) stockText.text = "在庫：" + stock + "kg";
    }

    public void OnBuyButton()
    {
        stock += judge.currentSupplier.volumeKg;
        money -= judge.currentSupplier.pricePerKg * judge.currentSupplier.volumeKg;
        purchasedSuppliers.Add(judge.currentSupplier);
        if (resultText != null) resultText.text = judge.currentSupplier.supplierName + " から仕入れました";
        UpdateUI();
        Debug.Log("仕入れました：" + judge.currentSupplier.supplierName);
        AfterDecision();
    }

    public void OnRefuseButton()
    {
        if (resultText != null) resultText.text = judge.currentSupplier.supplierName + " を断りました";
        Debug.Log("断りました：" + judge.currentSupplier.supplierName);
        AfterDecision();
    }

    public void OnReportButton()
    {
        reportedSuppliers.Add(judge.currentSupplier);
        if (resultText != null) resultText.text = judge.currentSupplier.supplierName + " を通報しました";
        Debug.Log("通報しました：" + judge.currentSupplier.supplierName);
        AfterDecision();
    }

    public void OnEndDayButton()
    {
        ProcessNightResult();
        if (screenManager != null) screenManager.ShowNight();
    }

    public void OnNextDayButton()
    {
        // 翌日の準備
        judge.ResetSuppliers();
        if (actionButtons != null) actionButtons.SetActive(true);
        if (endDayButton != null) endDayButton.SetActive(false);
        ShowCurrentSupplier();
        if (screenManager != null) screenManager.ShowBuy();
    }

    public void ProcessNightResult()
    {
        foreach (SupplierData supplier in purchasedSuppliers)
        {
            judge.currentSupplier = supplier;
            string violation = judge.CheckViolation();
            if (violation != "")
            {
                reputation = Mathf.Clamp(reputation - 20, 0, 100);
                Debug.Log("【夜】違反発覚：" + supplier.supplierName + " 評判:" + reputation + "%");
            }
            else
            {
                Debug.Log("【夜】問題なし：" + supplier.supplierName);
            }
        }

        foreach (SupplierData supplier in reportedSuppliers)
        {
            judge.currentSupplier = supplier;
            string violation = judge.CheckViolation();
            if (violation != "")
            {
                reputation = Mathf.Clamp(reputation + 10, 0, 100);
                Debug.Log("【夜】通報正解：" + supplier.supplierName);
            }
            else
            {
                reputation = Mathf.Clamp(reputation - 10, 0, 100);
                Debug.Log("【夜】誤報：" + supplier.supplierName);
            }
        }

        string nightSummary = "【本日の結果】\n所持金：¥" + money.ToString("N0") + "\n在庫：" + stock + "kg\n評判：" + reputation + "%";
        if (resultText != null) resultText.text = nightSummary;
        Debug.Log("【夜の結果】所持金:" + money + " 在庫:" + stock + "kg 評判:" + reputation + "%");

        purchasedSuppliers.Clear();
        reportedSuppliers.Clear();
    }
}

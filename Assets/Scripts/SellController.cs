using UnityEngine;
using TMPro;

public class SellController : MonoBehaviour
{
    public TextMeshProUGUI summaryText;   // 今年の相場・在庫・売上予測をまとめて表示

    public GameController gameController;
    public MarketManager  marketManager;

    void OnEnable()
    {
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (summaryText == null) return;

        int sellPrice   = marketManager.TodaySellPrice;
        int stock       = gameController.stock;
        int totalEarnings = stock * sellPrice;

        summaryText.text =
            "今年の売値：¥" + sellPrice.ToString("N0") + " / kg\n\n" +
            "現在の在庫：" + stock + " kg\n\n" +
            "全部売った場合：¥" + totalEarnings.ToString("N0");
    }

    // 全部売るボタン
    public void OnSellAllButton()
    {
        int earnings = gameController.stock * marketManager.TodaySellPrice;
        gameController.money += earnings;
        gameController.stock  = 0;
        gameController.UpdateUIPublic();
        gameController.OnSellComplete();
    }

    // 今年は売らないボタン
    public void OnSkipSellButton()
    {
        gameController.OnSellComplete();
    }
}

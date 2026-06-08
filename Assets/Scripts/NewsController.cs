using UnityEngine;
using TMPro;

public class NewsController : MonoBehaviour
{
    [Header("ニュース表示")]
    public TextMeshProUGUI newsTitleText;   // ニュースのタイトル
    public TextMeshProUGUI newsBodyText;    // ニュースの本文
    public TextMeshProUGUI pageIndicator;   // 「1/3」などのページ表示

    [Header("ボタン")]
    public GameObject nextButton;           // 「次のニュースへ」ボタン
    public GameObject goToBuyButton;        // 「仕入れへ」ボタン（最終ページのみ表示）

    [Header("参照")]
    public ScreenManager screenManager;
    public JudgeManager  judgeManager;
    public MarketManager marketManager;

    private int newsPage = 0;

    void OnEnable()
    {
        newsPage = 0;
        ShowPage();
    }

    void ShowPage()
    {
        string title = "";
        string body  = "";

        switch (newsPage)
        {
            case 0:
                title = "【今年の景気】";
                body  = marketManager.EconomyName + "\n\n" + marketManager.EconomyDesc;
                break;
            case 1:
                title = "【街の人の意見】";
                body  = marketManager.OpinionName + "\n\n" + marketManager.OpinionDesc;
                break;
            case 2:
                title = "【今年の収穫】";
                body  = marketManager.HarvestName + "\n\n" + marketManager.HarvestDesc +
                        "\n\n今年の売値：¥" + marketManager.TodaySellPrice.ToString("N0") + " / kg" +
                        "\n本日の仕入れ禁止産地：" +
                        (string.IsNullOrEmpty(judgeManager.bannedOrigin) ? "なし" : judgeManager.bannedOrigin);
                break;
        }

        if (newsTitleText != null) newsTitleText.text = title;
        if (newsBodyText  != null) newsBodyText.text  = body;
        if (pageIndicator != null) pageIndicator.text = (newsPage + 1) + " / 3";

        // ボタン切り替え
        bool isLastPage = newsPage >= 2;
        if (nextButton    != null) nextButton.SetActive(!isLastPage);
        if (goToBuyButton != null) goToBuyButton.SetActive(isLastPage);
    }

    public void OnNextNewsButton()
    {
        newsPage++;
        ShowPage();
    }

    public void OnGoToBuyButton()
    {
        if (screenManager != null) screenManager.ShowBuy();
    }
}

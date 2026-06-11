using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NewsController : MonoBehaviour
{
    [Header("ニュース表示")]
    public TextMeshProUGUI newsTitleText;   // ニュースのタイトル
    public TextMeshProUGUI newsBodyText;    // ニュースの本文
    public TextMeshProUGUI pageIndicator;   // 「/ 3」表示
    public TextMeshProUGUI newsNumberText;  // ニュース番号（大きい数字）

    [Header("ニュース画像エリア")]
    public GameObject economyIconGroup;     // 景気ページのアイコン群（キャスター＋棒グラフ）
    public GameObject opinionIconGroup;     // 街の人の意見ページのアイコン群（2人の会話）
    public GameObject harvestIconGroup;     // 今年の収穫ページのアイコン群（農家の人）
    public Image[] economyBars;             // 景気ページの棒グラフ（3本・高さと色を動的変更）

    [Header("ボタン")]
    public GameObject nextButton;           // 「次のニュースへ」ボタン
    public GameObject prevButton;           // 「もどる」ボタン（1ページ目では非表示）
    public GameObject goToBuyButton;        // 「仕入れへ」ボタン（最終ページのみ、つぎボタンと同位置）

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
                if (marketManager.EconomyName.Contains("良い"))
                    body = "「今年は消費が活発で、お店には活気が戻ってきています。少し贅沢なお買い物をする方も増えているようですね。」";
                else if (marketManager.EconomyName.Contains("悪い"))
                    body = "「今年は財布のひもが固くなっているご家庭が多いようです。お買い物にも慎重な姿勢が見られます。」";
                else
                    body = "「今年の景気は、これといった変化のない一年になりそうです。街の様子も、いつも通りといったところでしょうか。」";
                UpdateEconomyBars();
                break;

            case 1:
                title = "【街の人の意見】";
                if (marketManager.OpinionName.Contains("おいしさ"))
                    body = "「やっぱり毎日食べるものだしね、多少高くても美味しいお米がいいよねえ。」\n「わかる〜。安いだけのお米はもう飽きちゃった。」";
                else if (marketManager.OpinionName.Contains("安さ"))
                    body = "「最近物価高いから、お米くらいは安いの買いたいよね。」\n「だよね〜、1円でも安い方を選んじゃう。」";
                else
                    body = "「お米なんて、正直どれも同じじゃない？」\n「そうそう、普段使いならいつものやつで十分だよね。」";
                break;

            case 2:
                title = "【今年の収穫】";
                if (marketManager.HarvestName.Contains("豊作"))
                    body = "「いやあ、今年は田んぼが米でいっぱいでね。倉庫に入りきらないくらいですよ！」";
                else if (marketManager.HarvestName.Contains("不作"))
                    body = "「今年は天候に泣かされましてねえ…。例年の半分も収穫できませんでした。」";
                else
                    body = "「今年は例年通りといったところですかね。特に変わったこともなく、ひと安心です。」";
                break;
        }

        if (newsTitleText  != null) newsTitleText.text  = title;
        if (newsBodyText   != null) newsBodyText.text   = body;
        if (newsNumberText != null) newsNumberText.text = (newsPage + 1).ToString();
        if (pageIndicator  != null) pageIndicator.text  = "/ 3";

        // ニュース画像エリアの切り替え
        if (economyIconGroup != null) economyIconGroup.SetActive(newsPage == 0);
        if (opinionIconGroup != null) opinionIconGroup.SetActive(newsPage == 1);
        if (harvestIconGroup != null) harvestIconGroup.SetActive(newsPage == 2);

        // ボタン切り替え
        bool isLastPage = newsPage >= 2;
        if (nextButton    != null) nextButton.SetActive(!isLastPage);
        if (goToBuyButton != null) goToBuyButton.SetActive(isLastPage);
        if (prevButton    != null) prevButton.SetActive(newsPage > 0);
    }

    // 景気ページの棒グラフを今年の景気に合わせて更新
    void UpdateEconomyBars()
    {
        if (economyBars == null || economyBars.Length < 3 || marketManager == null) return;

        int[] heights;
        Color color;

        if (marketManager.EconomyName.Contains("良い"))
        {
            heights = new int[] { 50, 100, 150 }; // 右肩上がり
            color = HexColor("#6FAE3E");          // 良い：緑
        }
        else if (marketManager.EconomyName.Contains("悪い"))
        {
            heights = new int[] { 150, 100, 50 }; // 右肩下がり
            color = HexColor("#C9714A");          // 悪い：赤茶
        }
        else
        {
            heights = new int[] { 100, 100, 100 }; // 横ばい
            color = HexColor("#B0A48E");           // 普通：灰茶
        }

        for (int i = 0; i < economyBars.Length && i < heights.Length; i++)
        {
            if (economyBars[i] == null) continue;
            RectTransform rt = economyBars[i].rectTransform;
            Vector2 size = rt.sizeDelta;
            size.y = heights[i];
            rt.sizeDelta = size;
            economyBars[i].color = color;
        }
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }

    public void OnNextNewsButton()
    {
        newsPage++;
        ShowPage();
    }

    public void OnPrevNewsButton()
    {
        if (newsPage > 0)
        {
            newsPage--;
            ShowPage();
        }
    }

    public void OnGoToBuyButton()
    {
        if (screenManager != null) screenManager.ShowBuy();
    }
}

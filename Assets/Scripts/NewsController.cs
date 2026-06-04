using UnityEngine;
using TMPro;

public class NewsController : MonoBehaviour
{
    public TextMeshProUGUI bannedOriginText;
    public TextMeshProUGUI triviText;
    public ScreenManager screenManager;
    public JudgeManager judgeManager;

    static readonly string[] triviaList = new string[]
    {
        "コシノヒカルは粘りが強く、魚沼産は特に高品質とされています。",
        "あきたこひめは冷めても美味しく、お弁当に向いた品種です。",
        "ひとめほれは東北地方を代表する品種で、甘みとやわらかさが特徴です。",
        "ササニシカはあっさりとした味わいで、和食全般に合います。",
        "米の水分量は15〜16%が適切とされており、それを超えると品質が落ちます。",
        "新米は収穫から1年以内のもの。古米は味や香りが落ちやすい。",
        "産地偽装は食品表示法違反にあたり、刑事罰の対象になります。",
        "業者登録番号は毎年更新が必要です。期限切れは違法取引とみなされます。"
    };

    void OnEnable()
    {
        if (bannedOriginText != null && judgeManager != null)
        {
            if (!string.IsNullOrEmpty(judgeManager.bannedOrigin))
                bannedOriginText.text = "本日の仕入れ禁止産地：" + judgeManager.bannedOrigin;
            else
                bannedOriginText.text = "本日の仕入れ禁止産地：なし";
        }

        if (triviText != null)
            triviText.text = "【米豆知識】" + triviaList[Random.Range(0, triviaList.Length)];
    }

    public void OnGoToBuyButton()
    {
        if (screenManager != null) screenManager.ShowBuy();
    }
}

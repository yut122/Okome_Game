using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SupplierCardUI : MonoBehaviour
{
    [Header("カード表示テキスト")]
    public TextMeshProUGUI supplierNameText;
    public TextMeshProUGUI riceNameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI rankText;     // 米ランク（A〜D）
    public TextMeshProUGUI expiryText;   // 流通期限
    public TextMeshProUGUI volumeText;

    [Header("選択状態UI")]
    public Image  cardBackground;   // 選択時に色変更
    public GameObject selectedBorder;  // 選択枠
    public GameObject selectingBadge;  // 「選択中」バッジ

    [Header("参照")]
    public GameController gameController;

    private int supplierIndex = -1;

    static readonly Color normalColor   = new Color(1f,  0.97f, 0.94f, 1f); // #FFF8F0
    static readonly Color selectedColor = new Color(1f,  0.90f, 0.75f, 1f); // 選択中オレンジ薄

    public void Setup(SupplierData data, int index, GameController gc, int price, bool isReseller,
                      string rank, string expiry)
    {
        supplierIndex  = index;
        gameController = gc;
        Refresh(data, price, isReseller, rank, expiry);
        SetSelected(false);
    }

    public void Refresh(SupplierData data, int price, bool isReseller, string rank, string expiry)
    {
        if (data == null) return;
        // 転売屋のときは業者名の部分だけ「アヤシイ商店」に差し替える（お米・価格は通常通り）
        if (supplierNameText != null)
            supplierNameText.text = isReseller ? GameController.ResellerName : data.supplierName;
        if (riceNameText != null) riceNameText.text = data.claimedRiceName;
        if (priceText    != null) priceText.text    = GameController.ManYenTag(price); // 値札スタイル（数字大・万小）
        if (rankText     != null) { rankText.text = rank; rankText.color = RankColor(rank); }
        if (expiryText   != null) expiryText.text = expiry;
        HideVolumeLine();
    }

    static Color RankColor(string rank)
    {
        switch (rank)
        {
            case "A": return new Color(0.24f, 0.62f, 0.30f); // 緑（高評価）
            case "B": return new Color(0.43f, 0.68f, 0.24f); // 黄緑
            case "C": return new Color(0.88f, 0.55f, 0.16f); // オレンジ
            case "D": return new Color(0.69f, 0.40f, 0.23f); // 茶（低評価）
            default:  return new Color(0.24f, 0.17f, 0.12f);
        }
    }

    // 「数量 N袋」表示は廃止（仕入れは1クリックで1個に統一）。
    // 既存シーンに残るラベル/テキストGameObjectも非表示にする。
    void HideVolumeLine()
    {
        if (volumeText != null) volumeText.gameObject.SetActive(false);
        Transform label = transform.Find("LabelVolume");
        if (label != null) label.gameObject.SetActive(false);
    }

    public void OnCardClicked()
    {
        if (gameController != null)
            gameController.OnSupplierCardSelected(supplierIndex);
    }

    public void SetSelected(bool selected)
    {
        if (cardBackground  != null) cardBackground.color = selected ? selectedColor : normalColor;
        if (selectedBorder  != null) selectedBorder.SetActive(selected);
        if (selectingBadge  != null) selectingBadge.SetActive(selected);
    }
}

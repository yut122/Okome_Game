using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SupplierCardUI : MonoBehaviour
{
    [Header("カード表示テキスト")]
    public TextMeshProUGUI supplierNameText;
    public TextMeshProUGUI riceNameText;
    public TextMeshProUGUI priceText;
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

    public void Setup(SupplierData data, int index, GameController gc, int price)
    {
        supplierIndex  = index;
        gameController = gc;
        Refresh(data, price);
        SetSelected(false);
    }

    public void Refresh(SupplierData data, int price)
    {
        if (data == null) return;
        if (supplierNameText != null) supplierNameText.text = data.supplierName;
        if (riceNameText     != null) riceNameText.text     = data.claimedRiceName;
        if (priceText        != null) priceText.text        = "¥" + GameController.ToMan(price) + " / 個";
        HideVolumeLine();
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

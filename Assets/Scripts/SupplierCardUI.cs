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

    public void Setup(SupplierData data, int index, GameController gc)
    {
        supplierIndex  = index;
        gameController = gc;
        Refresh(data);
        SetSelected(false);
    }

    public void Refresh(SupplierData data)
    {
        if (data == null) return;
        if (supplierNameText != null) supplierNameText.text = data.supplierName;
        if (riceNameText     != null) riceNameText.text     = data.claimedRiceName;
        if (priceText        != null) priceText.text        = "¥" + data.pricePerKg.ToString("N0") + " / kg";
        if (volumeText       != null) volumeText.text       = data.volumeKg + " kg";
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

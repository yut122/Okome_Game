using UnityEngine;
using TMPro;

public class SupplierDisplay : MonoBehaviour
{
    [Header("業者情報パネル")]
    public TextMeshProUGUI supplierNameText;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI claimedRiceNameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI volumeText;

    [Header("証明書パネル")]
    public TextMeshProUGUI certRiceNameText;
    public TextMeshProUGUI certOriginText;
    public TextMeshProUGUI certRegistrationText;
    public TextMeshProUGUI registrationStatusText;

    public void ShowSupplier(SupplierData supplier, string bannedOrigin = "")
    {
        if (supplier == null) return;

        if (supplierNameText != null) supplierNameText.text = supplier.supplierName;
        if (dialogueText != null) dialogueText.text = "「" + supplier.dialogue + "」";
        if (claimedRiceNameText != null) claimedRiceNameText.text = supplier.claimedRiceName;
        if (certRiceNameText != null) certRiceNameText.text = supplier.certRiceName;
        if (certOriginText != null) certOriginText.text = supplier.certOrigin;
        if (certRegistrationText != null) certRegistrationText.text = supplier.certRegistrationNumber;
        if (registrationStatusText != null)
            registrationStatusText.text = supplier.registrationExpired ? "期限切れ" : "有効";
        if (priceText != null) priceText.text = GameController.ManYen(supplier.pricePerBag) + " / 個";
        if (volumeText != null) volumeText.text = supplier.bagCount + " 個";
    }
}

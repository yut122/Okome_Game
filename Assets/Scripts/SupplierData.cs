using UnityEngine;

[CreateAssetMenu(fileName = "SupplierData", menuName = "OkomeGame/SupplierData")]
public class SupplierData : ScriptableObject
{
    public string supplierName;
    public string dialogue;
    public string claimedRiceName;
    public int pricePerBag; // 1袋あたりの価格（円）
    public int bagCount;    // 持ち込む袋数

    public string certRiceName;
    public string certOrigin;
    public string certRegistrationNumber;
    public bool registrationExpired;
}

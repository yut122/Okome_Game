using UnityEngine;

[CreateAssetMenu(fileName = "SupplierData", menuName = "OkomeGame/SupplierData")]
public class SupplierData : ScriptableObject
{
    public string supplierName;
    public string dialogue;
    public string claimedRiceName;
    public int pricePerKg;
    public int volumeKg;

    public string certRiceName;
    public string certOrigin;
    public string certRegistrationNumber;
    public bool registrationExpired;
}

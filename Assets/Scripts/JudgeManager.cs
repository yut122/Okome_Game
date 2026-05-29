using System.Collections.Generic;
using UnityEngine;

public class JudgeManager : MonoBehaviour
{
    public List<SupplierData> suppliers = new List<SupplierData>();
    public string bannedOrigin;

    private int currentIndex = 0;

    public SupplierData currentSupplier
    {
        get
        {
            if (suppliers == null || suppliers.Count == 0) return null;
            if (currentIndex >= suppliers.Count) return null;
            return suppliers[currentIndex];
        }
        set
        {
            if (suppliers != null && currentIndex < suppliers.Count)
                suppliers[currentIndex] = value;
        }
    }

    public bool HasNextSupplier()
    {
        return currentIndex < suppliers.Count - 1;
    }

    public bool IsLastSupplier()
    {
        return currentIndex >= suppliers.Count - 1;
    }

    public void NextSupplier()
    {
        if (currentIndex < suppliers.Count - 1)
            currentIndex++;
    }

    public void ResetSuppliers()
    {
        currentIndex = 0;
    }

    public string CheckViolation()
    {
        if (currentSupplier == null) return "";

        if (currentSupplier.certOrigin == bannedOrigin)
            return $"{bannedOrigin}産は本日仕入れ禁止";

        if (currentSupplier.claimedRiceName != currentSupplier.certRiceName)
            return $"申告品種「{currentSupplier.claimedRiceName}」と証明書「{currentSupplier.certRiceName}」が一致しない";

        if (currentSupplier.registrationExpired)
            return "登録番号の有効期限が切れています";

        return "";
    }
}

using System.Collections.Generic;
using UnityEngine;

public class JudgeManager : MonoBehaviour
{
    public List<SupplierData> suppliers = new List<SupplierData>();
    public string bannedOrigin;

    private int currentIndex = 0;

    // 現在の業者（読み取り専用）。
    // ※以前は setter が suppliers[currentIndex] を上書きしており、
    //   違反チェックのたびに業者リストを破壊していたため setter を撤去した。
    public SupplierData currentSupplier
    {
        get
        {
            if (suppliers == null || suppliers.Count == 0) return null;
            if (currentIndex >= suppliers.Count) return null;
            return suppliers[currentIndex];
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

    // 現在の業者を判定（後方互換用の引数なし版）
    public string CheckViolation() => CheckViolation(currentSupplier);

    // 指定した業者の違反内容を返す（違反がなければ空文字）。
    // 状態を一切書き換えないため、任意の業者を安全に判定できる。
    public string CheckViolation(SupplierData supplier)
    {
        if (supplier == null) return "";

        if (!string.IsNullOrEmpty(bannedOrigin) && supplier.certOrigin == bannedOrigin)
            return $"{bannedOrigin}産は本日仕入れ禁止";

        if (supplier.claimedRiceName != supplier.certRiceName)
            return $"申告品種「{supplier.claimedRiceName}」と証明書「{supplier.certRiceName}」が一致しない";

        if (supplier.registrationExpired)
            return "登録番号の有効期限が切れています";

        return "";
    }
}

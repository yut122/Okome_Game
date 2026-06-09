using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    public GameObject titleScreen;
    public GameObject newsScreen;
    public GameObject buyScreen;
    public GameObject sellScreen;
    public GameObject nightScreen;
    public GameObject endingScreen;

    [Header("常時表示UI")]
    public GameObject globalTopBar; // タイトル画面では非表示にする

    void Start()
    {
        ShowTitle();
    }

    public void ShowTitle()
    {
        SetAll(false);
        if (titleScreen  != null) titleScreen.SetActive(true);
        if (globalTopBar != null) globalTopBar.SetActive(false); // タイトルでは非表示
    }

    public void ShowNews()
    {
        SetAll(false);
        if (newsScreen   != null) newsScreen.SetActive(true);
        if (globalTopBar != null) globalTopBar.SetActive(true);
    }

    public void ShowBuy()
    {
        SetAll(false);
        if (buyScreen    != null) buyScreen.SetActive(true);
        if (globalTopBar != null) globalTopBar.SetActive(true);
    }

    public void ShowSell()
    {
        SetAll(false);
        if (sellScreen   != null) sellScreen.SetActive(true);
        if (globalTopBar != null) globalTopBar.SetActive(true);
    }

    public void ShowNight()
    {
        SetAll(false);
        if (nightScreen  != null) nightScreen.SetActive(true);
        if (globalTopBar != null) globalTopBar.SetActive(true);
    }

    public void ShowEnding()
    {
        SetAll(false);
        if (endingScreen != null) endingScreen.SetActive(true);
        if (globalTopBar != null) globalTopBar.SetActive(false); // エンディングでも非表示
    }

    void SetAll(bool active)
    {
        if (titleScreen  != null) titleScreen.SetActive(active);
        if (newsScreen   != null) newsScreen.SetActive(active);
        if (buyScreen    != null) buyScreen.SetActive(active);
        if (sellScreen   != null) sellScreen.SetActive(active);
        if (nightScreen  != null) nightScreen.SetActive(active);
        if (endingScreen != null) endingScreen.SetActive(active);
    }
}

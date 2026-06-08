using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    public GameObject shopScreen;
    public GameObject newsScreen;
    public GameObject buyScreen;
    public GameObject sellScreen;
    public GameObject nightScreen;
    public GameObject endingScreen;

    void Start()
    {
        ShowNews();
    }

    public void ShowShop()   { SetAll(false); if (shopScreen   != null) shopScreen.SetActive(true); }
    public void ShowNews()   { SetAll(false); if (newsScreen   != null) newsScreen.SetActive(true); }
    public void ShowBuy()    { SetAll(false); if (buyScreen    != null) buyScreen.SetActive(true); }
    public void ShowSell()   { SetAll(false); if (sellScreen   != null) sellScreen.SetActive(true); }
    public void ShowNight()  { SetAll(false); if (nightScreen  != null) nightScreen.SetActive(true); }
    public void ShowEnding() { SetAll(false); if (endingScreen != null) endingScreen.SetActive(true); }

    void SetAll(bool active)
    {
        if (shopScreen   != null) shopScreen.SetActive(active);
        if (newsScreen   != null) newsScreen.SetActive(active);
        if (buyScreen    != null) buyScreen.SetActive(active);
        if (sellScreen   != null) sellScreen.SetActive(active);
        if (nightScreen  != null) nightScreen.SetActive(active);
        if (endingScreen != null) endingScreen.SetActive(active);
    }
}

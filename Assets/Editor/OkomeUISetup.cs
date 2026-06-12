using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor.Events;
using TMPro;

/// <summary>
/// おこめゲーム UIセットアップツール
/// メニュー → Tools → おこめゲーム → から各機能を実行
/// </summary>
public class OkomeUISetup : EditorWindow
{
    // ===== カラーパレット =====
    static readonly Color colorBackground = HexColor("#FAF0D7"); // 画面背景：クリーム
    static readonly Color colorPanel      = HexColor("#FFF8F0"); // パネル：温かい白
    static readonly Color colorButton     = HexColor("#E8841A"); // ボタン：オレンジ
    static readonly Color colorButtonText = HexColor("#FFFFFF"); // ボタンテキスト：白
    static readonly Color colorText       = HexColor("#3D2B1F"); // 本文テキスト：濃い茶
    static readonly Color colorAccent     = HexColor("#C8621A"); // ボタンHover：濃いオレンジ

    // ===== メニュー項目 =====

    [MenuItem("Tools/おこめゲーム/⓪ Canvas解像度を固定（最初に実行）")]
    static void FixCanvasResolution()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvasが見つかりません"); return; }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.AddComponent<CanvasScaler>();

        Undo.RecordObject(scaler, "Fix Canvas Resolution");
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("✓ Canvas解像度を1280×720に固定しました");
    }

    [MenuItem("Tools/おこめゲーム/TitleScreenを構築して配線")]
    static void BuildAndWireTitleScreen()
    {
        // Canvas直下にTitleScreenがなければ作成
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvasが見つかりません"); return; }

        GameObject titleScreen = GameObject.Find("TitleScreen");
        if (titleScreen == null)
        {
            titleScreen = new GameObject("TitleScreen", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(titleScreen, "Create TitleScreen");
            titleScreen.transform.SetParent(canvas.transform, false);
            var rt = titleScreen.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta  = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }
        else
        {
            while (titleScreen.transform.childCount > 0)
                Undo.DestroyObjectImmediate(titleScreen.transform.GetChild(0).gameObject);
        }

        SetImageColor(titleScreen, HexColor("#FAF0D7"));

        // ── 背景装飾（オレンジ帯・上下）──
        CreatePanel(titleScreen, "TopBand",
            new Vector2(0, 305), new Vector2(1280, 90), HexColor("#E8841A"));
        CreatePanel(titleScreen, "BotBand",
            new Vector2(0, -305), new Vector2(1280, 90), HexColor("#E8841A"));

        // ── ゲームタイトル（大きく）──
        CreateText(titleScreen, "TitleText",
            new Vector2(0, 90), new Vector2(1000, 160),
            "お米ゲーム", 120, HexColor("#3D2B1F"), TextAlignmentOptions.Center);

        // ── サブタイトル（小さめ）──
        CreateText(titleScreen, "SubTitleText",
            new Vector2(0, -20), new Vector2(600, 40),
            "～お米を見極めろ！～", 22, HexColor("#E8841A"), TextAlignmentOptions.Center);

        // ── 「はじめる」ボタン ──
        GameObject startBtn = CreateButton(titleScreen, "StartButton", "はじめる",
            new Vector2(0, -140), new Vector2(260, 72));
        // ボタンを大きめのフォントに
        var btnText = FindInChildren(startBtn, "Text")?.GetComponent<TextMeshProUGUI>();
        if (btnText != null) btnText.fontSize = 34;

        // ── 自動配線 ──
        var sm = Object.FindObjectOfType<ScreenManager>();
        if (sm != null)
        {
            // ScreenManager の titleScreen フィールドを設定
            var soSm = new SerializedObject(sm);
            SetField(soSm, "titleScreen", titleScreen);
            soSm.ApplyModifiedProperties();

            // StartButton → ScreenManager.ShowNews
            WireButton("StartButton", sm, "ShowNews");
        }

        // GlobalTopBarはタイトル画面では非表示にする（Canvas最前面にあるため別途対応）

        // HierarchyでGlobalTopBarより手前に配置（タイトルの後に来るよう最後尾へ）
        titleScreen.transform.SetAsLastSibling();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("✓ TitleScreen 構築+配線完了");
    }

    [MenuItem("Tools/おこめゲーム/⓪-B グローバルTopBarを作成")]
    static void CreateGlobalTopBar()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvasが見つかりません"); return; }

        // 既存のGlobalTopBarがあれば削除
        GameObject existing = GameObject.Find("GlobalTopBar");
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        // GlobalTopBar（Canvas直下・常時表示）
        GameObject topBar = CreatePanel(canvas, "GlobalTopBar",
            new Vector2(0, 330), new Vector2(1280, 60),
            HexColor("#E8841A"));

        // 年数テキスト（左）
        CreateText(topBar, "GlobalDayText",
            new Vector2(-480, 0), new Vector2(160, 50),
            "1年目", 28, Color.white, TextAlignmentOptions.Left);

        // 所持金テキスト（中央左）
        CreateText(topBar, "GlobalMoneyText",
            new Vector2(-100, 0), new Vector2(280, 50),
            "所持金：1000万円", 26, Color.white, TextAlignmentOptions.Center);

        // 在庫テキスト（中央右）
        CreateText(topBar, "GlobalStockText",
            new Vector2(220, 0), new Vector2(200, 50),
            "在庫：0個", 26, Color.white, TextAlignmentOptions.Center);

        // HierarchyでCanvas内の最後（前面表示）に移動
        topBar.transform.SetAsLastSibling();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("✓ GlobalTopBarを作成しました。\nGameControllerのMoneyText・StockText・DayTextを GlobalMoneyText・GlobalStockText・GlobalDayText に付け替えてください。");
    }

    [MenuItem("Tools/おこめゲーム/① 全UIにカラーを適用")]
    static void ApplyAllColors()
    {
        ApplyScreenColors();
        ApplyButtonColors();
        ApplyTextColors();
        Debug.Log("✓ 全UIカラーの適用が完了しました");
    }

    [MenuItem("Tools/おこめゲーム/② 画面パネルのカラーを適用")]
    static void ApplyScreenColors()
    {
        string[] screenNames = { "NewsScreen", "BuyScreen", "SellScreen", "NightScreen", "EndingScreen", "ShopScreen" };

        foreach (string name in screenNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null) continue;

            Image img = obj.GetComponent<Image>();
            if (img != null)
            {
                Undo.RecordObject(img, "Apply Screen Color");
                img.color = colorPanel;
            }
        }

        // Canvas背景
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            Image img = canvas.GetComponent<Image>();
            if (img != null)
            {
                Undo.RecordObject(img, "Apply Canvas Color");
                img.color = colorBackground;
            }
        }

        Debug.Log("✓ 画面パネルのカラーを適用しました");
    }

    [MenuItem("Tools/おこめゲーム/③ ボタンのカラーを適用")]
    static void ApplyButtonColors()
    {
        Button[] buttons = GameObject.FindObjectsOfType<Button>(true);

        foreach (Button btn in buttons)
        {
            Undo.RecordObject(btn, "Apply Button Color");

            ColorBlock colors = btn.colors;
            colors.normalColor      = colorButton;
            colors.highlightedColor = colorAccent;
            colors.pressedColor     = HexColor("#A04010");
            colors.selectedColor    = colorButton;
            btn.colors = colors;

            // ボタンのImage背景色
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                Undo.RecordObject(img, "Apply Button Image Color");
                img.color = colorButton;
            }

            // ボタン内のテキスト
            TextMeshProUGUI[] texts = btn.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                Undo.RecordObject(t, "Apply Button Text Color");
                t.color = colorButtonText;
            }
        }

        Debug.Log("✓ ボタンのカラーを適用しました（" + buttons.Length + "個）");
    }

    [MenuItem("Tools/おこめゲーム/④ テキストのカラーを適用")]
    static void ApplyTextColors()
    {
        TextMeshProUGUI[] texts = GameObject.FindObjectsOfType<TextMeshProUGUI>(true);
        int count = 0;

        foreach (var t in texts)
        {
            // ボタン内のテキストはスキップ（白のまま）
            if (t.GetComponentInParent<Button>() != null) continue;

            Undo.RecordObject(t, "Apply Text Color");
            t.color = colorText;
            count++;
        }

        Debug.Log("✓ テキストのカラーを適用しました（" + count + "個）");
    }

    [MenuItem("Tools/おこめゲーム/⑤ TopBarを半透明オレンジに")]
    static void ApplyTopBarColor()
    {
        GameObject topBar = GameObject.Find("TopBar");
        if (topBar == null) { Debug.LogWarning("TopBarが見つかりません"); return; }

        Image img = topBar.GetComponent<Image>();
        if (img != null)
        {
            Undo.RecordObject(img, "Apply TopBar Color");
            img.color = HexColor("#E8841ACD"); // 半透明オレンジ
        }

        // TopBar内のテキストを白に
        TextMeshProUGUI[] texts = topBar.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in texts)
        {
            Undo.RecordObject(t, "Apply TopBar Text Color");
            t.color = colorButtonText;
        }

        Debug.Log("✓ TopBarのカラーを適用しました");
    }

    // ===== 全自動配線 =====

    [MenuItem("Tools/おこめゲーム/★ 全コンポーネントを自動配線")]
    static void AutoWireAll()
    {
        GameObject gameManager = GameObject.Find("GameManager");
        if (gameManager == null) { Debug.LogError("GameManagerが見つかりません"); return; }

        GameObject newsScreen   = GameObject.Find("NewsScreen");
        GameObject buyScreen    = GameObject.Find("BuyScreen");
        GameObject sellScreen   = GameObject.Find("SellScreen");
        GameObject nightScreen  = GameObject.Find("NightScreen");
        GameObject endingScreen = GameObject.Find("EndingScreen");
        GameObject supplierPanel = GameObject.Find("SupplierPanel");

        GameController  gc = gameManager.GetComponent<GameController>();
        ScreenManager   sm = gameManager.GetComponent<ScreenManager>();
        JudgeManager    jm = gameManager.GetComponent<JudgeManager>();
        MarketManager   mm = gameManager.GetComponent<MarketManager>();
        NewsController  nc = newsScreen?.GetComponent<NewsController>();
        SellController  sc = sellScreen?.GetComponent<SellController>();
        SupplierDisplay sd = supplierPanel?.GetComponent<SupplierDisplay>();

        // ── ScreenManager ──
        if (sm != null)
        {
            var so = new SerializedObject(sm);
            SetField(so, "newsScreen",   newsScreen);
            SetField(so, "buyScreen",    buyScreen);
            SetField(so, "sellScreen",   sellScreen);
            SetField(so, "nightScreen",  nightScreen);
            SetField(so, "endingScreen", endingScreen);
            so.ApplyModifiedProperties();
            Debug.Log("✓ ScreenManager 配線完了");
        }

        // ── GameController ──
        if (gc != null)
        {
            var so = new SerializedObject(gc);
            SetField(so, "judge",          jm);
            SetField(so, "market",         mm);
            SetField(so, "screenManager",  sm);
            SetField(so, "supplierDisplay", sd);
            SetField(so, "moneyText",  FindDeep("GlobalMoneyText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "stockText",  FindDeep("GlobalStockText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "dayText",    FindDeep("GlobalDayText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "resultText", FindDeep("NightResultText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "endingText", FindDeep("EndingText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "actionButtons", GameObject.Find("ActionButtons"));
            SetField(so, "endDayButton",  GameObject.Find("EndDayButton"));
            so.ApplyModifiedProperties();
            Debug.Log("✓ GameController 配線完了");
        }

        // ── NewsController ──
        if (nc != null)
        {
            var so = new SerializedObject(nc);
            SetField(so, "newsTitleText",  FindDeep("NewsTitleText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "newsBodyText",   FindDeep("NewsBodyText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "newsNumberText", FindDeep("NewsNumberText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "pageIndicator",  FindDeep("PageIndicator")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "nextButton",     GameObject.Find("NextNewsButton"));
            SetField(so, "prevButton",     FindDeep("PrevNewsButton"));
            SetField(so, "goToBuyButton",  FindDeep("GoToBuyButton"));
            SetField(so, "economyIconGroup", FindDeep("EconomyIconGroup"));
            SetField(so, "opinionIconGroup", FindDeep("OpinionIconGroup"));
            SetField(so, "harvestIconGroup", FindDeep("HarvestIconGroup"));
            SetField(so, "screenManager",  sm);
            SetField(so, "judgeManager",   jm);
            SetField(so, "marketManager",  mm);
            SetEconomyBars(so);
            so.ApplyModifiedProperties();
            Debug.Log("✓ NewsController 配線完了");
        }

        // ── SellController ──
        if (sc != null)
        {
            var so = new SerializedObject(sc);
            SetField(so, "summaryText",    FindDeep("SummaryText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "gameController", gc);
            SetField(so, "marketManager",  mm);
            so.ApplyModifiedProperties();
            Debug.Log("✓ SellController 配線完了");
        }

        // ── SupplierDisplay ──
        if (sd != null)
        {
            var so = new SerializedObject(sd);
            SetField(so, "supplierNameText",     FindDeep("SupplierText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "dialogueText",          FindDeep("DialogueText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "claimedRiceNameText",   FindDeep("ClaimedRiceText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "priceText",             FindDeep("PriceText")?.GetComponent<TextMeshProUGUI>()
                                               ?? FindDeep("SupplierPriceText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "volumeText",            FindDeep("VolumeText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "certRiceNameText",      FindDeep("CertRiceNameText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "certOriginText",        FindDeep("CertOrigineText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "certRegistrationText",  FindDeep("CertRegistrationText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "registrationStatusText",FindDeep("RegistrationStatusText")?.GetComponent<TextMeshProUGUI>());
            so.ApplyModifiedProperties();
            Debug.Log("✓ SupplierDisplay 配線完了");
        }

        // ── ボタンのOnClick ──
        WireButton("SellButton",      gc, "OnBuyButton");
        WireButton("RefuseButton",    gc, "OnRefuseButton");
        WireButton("ReportButton",    gc, "OnReportButton");
        WireButton("EndDayButton",    gc, "OnEndDayButton");
        WireButton("NextDayButton",   gc, "OnNextDayButton");
        WireButton("NextNewsButton",  nc, "OnNextNewsButton");
        WireButton("PrevNewsButton",  nc, "OnPrevNewsButton");
        WireButton("GoToBuyButton",   nc, "OnGoToBuyButton");
        WireButton("SellAllButton",   sc, "OnSellAllButton");
        WireButton("SkipSellButton",  sc, "OnSkipSellButton");

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("★ 全自動配線が完了しました！Ctrl+S で保存してください。");
    }

    // ===== 配線ヘルパー =====

    static void SetField(SerializedObject so, string fieldName, Object value)
    {
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null) prop.objectReferenceValue = value;
        else Debug.LogWarning("フィールドが見つかりません: " + fieldName);
    }

    // NewsControllerのeconomyBars配列（棒グラフ3本）を配線
    static void SetEconomyBars(SerializedObject so)
    {
        SerializedProperty barsProp = so.FindProperty("economyBars");
        if (barsProp == null) return;

        string[] barNames = { "EconomyBar1", "EconomyBar2", "EconomyBar3" };
        barsProp.arraySize = barNames.Length;
        for (int i = 0; i < barNames.Length; i++)
            barsProp.GetArrayElementAtIndex(i).objectReferenceValue = FindDeep(barNames[i])?.GetComponent<Image>();
    }

    static void WireButton(string buttonName, Component target, string methodName)
    {
        if (target == null) return;
        // 非アクティブなボタン（PrevNewsButton/GoToBuyButtonなど）も見つけられるようFindDeepを使用
        GameObject btnObj = FindDeep(buttonName);
        if (btnObj == null) { Debug.LogWarning("ボタンが見つかりません: " + buttonName); return; }

        Button btn = btnObj.GetComponent<Button>();
        if (btn == null) return;

        Undo.RecordObject(btn, "Wire Button " + buttonName);
        btn.onClick.RemoveAllListeners();
        // 永続リスナー（Inspectorに登録されるOnClick）も全削除して重複配線を防ぐ
        for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(btn.onClick, i);

        var method = target.GetType().GetMethod(methodName);
        if (method == null) { Debug.LogWarning("メソッドが見つかりません: " + methodName); return; }

        var action = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), target, method);
        UnityEventTools.AddPersistentListener(btn.onClick, action);
        Debug.Log("✓ ボタン配線: " + buttonName + " → " + methodName);
    }

    static GameObject FindDeep(string name)
    {
        // シーン内全オブジェクトを検索
        foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
            if (obj.name == name && obj.scene.isLoaded) return obj;
        return null;
    }

    // ===== 個別配線メニュー =====

    [MenuItem("Tools/おこめゲーム/業者データを初期設定")]
    static void SetupSupplierData()
    {
        SetSupplier("Supplier01",
            supplierName:        "田中農場",
            dialogue:            "うちの米は自慢の品です。どうぞよろしく！",
            claimedRiceName:     "あまひかり",
            pricePerBag:         1400000,
            bagCount:            6,
            certRiceName:        "あまひかり",
            certOrigin:          "南部地区",
            certRegistrationNumber: "A-2024-001",
            registrationExpired: false);

        SetSupplier("Supplier02",
            supplierName:        "丸山米穀",
            dialogue:            "今日は特別価格でご提供します！お得ですよ！",
            claimedRiceName:     "春小町",
            pricePerBag:         1000000,
            bagCount:            10,
            certRiceName:        "春小町",
            certOrigin:          "西部地区",
            certRegistrationNumber: "B-2024-015",
            registrationExpired: false);

        SetSupplier("Supplier03",
            supplierName:        "佐藤商店",
            dialogue:            "品質には自信があります。ぜひご検討を。",
            claimedRiceName:     "カリカリ米",
            pricePerBag:         400000,
            bagCount:            12,
            certRiceName:        "カリカリ米",
            certOrigin:          "東部地区",
            certRegistrationNumber: "C-2024-008",
            registrationExpired: false);

        Debug.Log("✓ 業者データを初期設定しました（初期フェーズ：全業者 正規品）。\n田中農場 あまひかり(A) / 丸山米穀 春小町(C) / 佐藤商店 カリカリ米(D)");
    }

    static void SetSupplier(string assetName, string supplierName, string dialogue,
        string claimedRiceName, int pricePerBag, int bagCount,
        string certRiceName, string certOrigin, string certRegistrationNumber, bool registrationExpired)
    {
        string[] guids = AssetDatabase.FindAssets(assetName + " t:SupplierData");
        if (guids.Length == 0) { Debug.LogWarning(assetName + "が見つかりません"); return; }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        SupplierData data = AssetDatabase.LoadAssetAtPath<SupplierData>(path);
        if (data == null) return;

        Undo.RecordObject(data, "Set Supplier Data");
        data.supplierName          = supplierName;
        data.dialogue              = dialogue;
        data.claimedRiceName       = claimedRiceName;
        data.pricePerBag           = pricePerBag;
        data.bagCount              = bagCount;
        data.certRiceName          = certRiceName;
        data.certOrigin            = certOrigin;
        data.certRegistrationNumber = certRegistrationNumber;
        data.registrationExpired   = registrationExpired;

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log("✓ " + assetName + "（" + supplierName + "）を更新しました");
    }

    [MenuItem("Tools/おこめゲーム/個別配線/ScreenManager を配線")]
    static void WireScreenManager()
    {
        var sm = GetComponent<ScreenManager>("GameManager");
        if (sm == null) return;
        var so = new SerializedObject(sm);
        SetField(so, "titleScreen",  GameObject.Find("TitleScreen"));
        SetField(so, "newsScreen",   GameObject.Find("NewsScreen"));
        SetField(so, "buyScreen",    GameObject.Find("BuyScreen"));
        SetField(so, "sellScreen",   GameObject.Find("SellScreen"));
        SetField(so, "nightScreen",  GameObject.Find("NightScreen"));
        SetField(so, "endingScreen", GameObject.Find("EndingScreen"));
        so.ApplyModifiedProperties();
        MarkDirtyAndLog("ScreenManager");
    }

    [MenuItem("Tools/おこめゲーム/個別配線/GameController を配線")]
    static void WireGameController()
    {
        var gc = GetComponent<GameController>("GameManager");
        var sm = GetComponent<ScreenManager>("GameManager");
        var jm = GetComponent<JudgeManager>("GameManager");
        var mm = GetComponent<MarketManager>("GameManager");
        var sd = GameObject.Find("SupplierPanel")?.GetComponent<SupplierDisplay>();
        if (gc == null) return;
        var so = new SerializedObject(gc);
        SetField(so, "judge",          jm);
        SetField(so, "market",         mm);
        SetField(so, "screenManager",  sm);
        SetField(so, "supplierDisplay", sd);
        SetField(so, "moneyText",  FindDeep("GlobalMoneyText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "stockText",  FindDeep("GlobalStockText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "dayText",    FindDeep("GlobalDayText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "resultText", FindDeep("NightResultText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "endingText", FindDeep("EndingText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "actionButtons", GameObject.Find("ActionButtons"));
        SetField(so, "endDayButton",  GameObject.Find("EndDayButton"));
        so.ApplyModifiedProperties();
        MarkDirtyAndLog("GameController");
    }

    [MenuItem("Tools/おこめゲーム/個別配線/NewsController を配線")]
    static void WireNewsController()
    {
        var nc = GetComponent<NewsController>("NewsScreen");
        var sm = GetComponent<ScreenManager>("GameManager");
        var jm = GetComponent<JudgeManager>("GameManager");
        var mm = GetComponent<MarketManager>("GameManager");
        if (nc == null) return;
        var so = new SerializedObject(nc);
        SetField(so, "newsTitleText",  FindDeep("NewsTitleText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "newsBodyText",   FindDeep("NewsBodyText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "newsNumberText", FindDeep("NewsNumberText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "pageIndicator",  FindDeep("PageIndicator")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "nextButton",     GameObject.Find("NextNewsButton"));
        SetField(so, "prevButton",     FindDeep("PrevNewsButton"));
        SetField(so, "goToBuyButton",  FindDeep("GoToBuyButton"));
        SetField(so, "economyIconGroup", FindDeep("EconomyIconGroup"));
        SetField(so, "opinionIconGroup", FindDeep("OpinionIconGroup"));
        SetField(so, "harvestIconGroup", FindDeep("HarvestIconGroup"));
        SetField(so, "screenManager",  sm);
        SetField(so, "judgeManager",   jm);
        SetField(so, "marketManager",  mm);
        SetEconomyBars(so);
        so.ApplyModifiedProperties();
        WireButton("PrevNewsButton", nc, "OnPrevNewsButton");
        MarkDirtyAndLog("NewsController");
    }

    [MenuItem("Tools/おこめゲーム/個別配線/SellController を配線")]
    static void WireSellController()
    {
        var sc = GetComponent<SellController>("SellScreen");
        var gc = GetComponent<GameController>("GameManager");
        var mm = GetComponent<MarketManager>("GameManager");
        if (sc == null) return;
        var so = new SerializedObject(sc);
        SetField(so, "summaryText",    FindDeep("SummaryText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "gameController", gc);
        SetField(so, "marketManager",  mm);
        so.ApplyModifiedProperties();
        MarkDirtyAndLog("SellController");
    }

    [MenuItem("Tools/おこめゲーム/個別配線/SupplierDisplay を配線")]
    static void WireSupplierDisplay()
    {
        var sd = GameObject.Find("SupplierPanel")?.GetComponent<SupplierDisplay>();
        if (sd == null) return;
        var so = new SerializedObject(sd);
        SetField(so, "supplierNameText",      FindDeep("SupplierText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "dialogueText",          FindDeep("DialogueText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "claimedRiceNameText",   FindDeep("ClaimedRiceText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "priceText",             FindDeep("PriceText")?.GetComponent<TextMeshProUGUI>()
                                           ?? FindDeep("SupplierPriceText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "volumeText",            FindDeep("VolumeText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "certRiceNameText",      FindDeep("CertRiceNameText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "certOriginText",        FindDeep("CertOrigineText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "certRegistrationText",  FindDeep("CertRegistrationText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "registrationStatusText",FindDeep("RegistrationStatusText")?.GetComponent<TextMeshProUGUI>());
        so.ApplyModifiedProperties();
        MarkDirtyAndLog("SupplierDisplay");
    }

    [MenuItem("Tools/おこめゲーム/個別配線/ボタンのOnClickを配線")]
    static void WireButtons()
    {
        var gc = GetComponent<GameController>("GameManager");
        var nc = GetComponent<NewsController>("NewsScreen");
        var sc = GetComponent<SellController>("SellScreen");
        WireButton("SellButton",      gc, "OnBuyButton");
        WireButton("RefuseButton",    gc, "OnRefuseButton");
        WireButton("ReportButton",    gc, "OnReportButton");
        WireButton("EndDayButton",    gc, "OnEndDayButton");
        WireButton("NextDayButton",   gc, "OnNextDayButton");
        WireButton("NextNewsButton",  nc, "OnNextNewsButton");
        WireButton("PrevNewsButton",  nc, "OnPrevNewsButton");
        WireButton("GoToBuyButton",   nc, "OnGoToBuyButton");
        WireButton("SellAllButton",   sc, "OnSellAllButton");
        WireButton("SkipSellButton",  sc, "OnSkipSellButton");
        MarkDirtyAndLog("全ボタン");
    }

    static T GetComponent<T>(string objectName) where T : Component
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj == null) { Debug.LogError(objectName + "が見つかりません"); return null; }
        T comp = obj.GetComponent<T>();
        if (comp == null) Debug.LogError(typeof(T).Name + "が" + objectName + "にありません");
        return comp;
    }

    static void MarkDirtyAndLog(string name)
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("✓ " + name + " の配線が完了しました");
    }

    // ===== NewsScreen レイアウト構築 =====

    [MenuItem("Tools/おこめゲーム/NewsScreenレイアウトを構築")]
    static void BuildNewsScreenLayout()
    {
        GameObject newsScreen = GameObject.Find("NewsScreen");
        if (newsScreen == null) { Debug.LogError("NewsScreenが見つかりません"); return; }

        while (newsScreen.transform.childCount > 0)
            Undo.DestroyObjectImmediate(newsScreen.transform.GetChild(0).gameObject);

        // 背景：クリーム
        SetImageColor(newsScreen, HexColor("#FAF0D7"));

        // ① テレビ外枠（グレー）左寄せして右にボタンスペース確保
        GameObject tvFrame = CreatePanel(newsScreen, "TVFrame",
            new Vector2(-55, 10), new Vector2(950, 540),
            HexColor("#999999"));

        // ② テレビ画面（温かい白）
        GameObject tvScreen = CreatePanel(tvFrame, "TVScreen",
            Vector2.zero, new Vector2(870, 460),
            HexColor("#FFF8F0"));

        // ③ ニュースタイトル（左上寄せ）
        CreateText(tvScreen, "NewsTitleText",
            new Vector2(0, 175), new Vector2(800, 55),
            "【今年の景気】", 36, HexColor("#3D2B1F"), TextAlignmentOptions.Left);

        // ④ ニュース画像エリア（中央）
        GameObject newsImageArea = CreatePanel(tvScreen, "NewsImageArea",
            new Vector2(0, 30), new Vector2(400, 240),
            HexColor("#E8D8B0"));

        // ④-a 景気ページ：ニュースキャスター＋棒グラフ
        GameObject economyGroup = CreateGroup(newsImageArea, "EconomyIconGroup");
        BuildCasterIcon(economyGroup);

        // ④-b 街の人の意見ページ：2人が話している様子
        GameObject opinionGroup = CreateGroup(newsImageArea, "OpinionIconGroup");
        BuildCitizenIcon(opinionGroup);
        opinionGroup.SetActive(false);

        // ④-c 今年の収穫ページ：田んぼに立つ農家の人
        GameObject harvestGroup = CreateGroup(newsImageArea, "HarvestIconGroup");
        BuildFarmerIcon(harvestGroup);
        harvestGroup.SetActive(false);

        // ⑤ 本文ボックス（下部ストリップ）
        GameObject bodyBox = CreatePanel(tvScreen, "BodyBox",
            new Vector2(0, -148), new Vector2(830, 108),
            HexColor("#FFF0DC"));
        CreateText(bodyBox, "NewsBodyText",
            new Vector2(8, 0), new Vector2(800, 90),
            "ニュース本文がここに表示されます。", 26, HexColor("#3D2B1F"), TextAlignmentOptions.Left);

        // ⑥ ニュース番号バッジ（1/3を1つの枠に）
        GameObject newsBadge = CreatePanel(newsScreen, "NewsBadge",
            new Vector2(545, 205), new Vector2(120, 90),
            HexColor("#444444"));
        CreateText(newsBadge, "NewsNumberText",
            new Vector2(0, 8), new Vector2(110, 55),
            "1", 46, HexColor("#44DD44"), TextAlignmentOptions.Center);
        CreateText(newsBadge, "PageIndicator",
            new Vector2(0, -28), new Vector2(110, 30),
            "/ 3", 20, HexColor("#FFFFFF"), TextAlignmentOptions.Center);

        // ⑧「つぎ」ボタン
        CreateButton(newsScreen, "NextNewsButton", "つぎ",
            new Vector2(545, 20), new Vector2(130, 65));

        // ⑨「仕入れへ」ボタン（最終ページのみ・「つぎ」と同じ位置に表示、初期非表示）
        GameObject goBtn = CreateButton(newsScreen, "GoToBuyButton", "仕入れへ",
            new Vector2(545, 20), new Vector2(130, 65));
        goBtn.SetActive(false);

        // ⑩「もどる」ボタン（1ページ目では非表示）
        GameObject prevBtn = CreateButton(newsScreen, "PrevNewsButton", "もどる",
            new Vector2(545, -60), new Vector2(130, 65));
        prevBtn.SetActive(false);

        Selection.activeGameObject = newsScreen;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("✓ NewsScreenレイアウト構築完了。NewsControllerのInspectorフィールドを再設定してください。");
    }

    // ── ニュース画像エリア用：アイコングループの空コンテナを生成 ──
    static GameObject CreateGroup(GameObject parent, string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(400, 240);
        return obj;
    }

    // ── 景気ページ：ニュースキャスター＋棒グラフ ──
    static void BuildCasterIcon(GameObject parent)
    {
        // ニュースキャスター（左側）
        CreatePanel(parent, "CasterDesk", new Vector2(-130, -100), new Vector2(120, 26), HexColor("#444444"));
        CreatePanel(parent, "CasterBody", new Vector2(-130, -52),  new Vector2(80, 70),  HexColor("#8B5E3C"));
        CreatePanel(parent, "CasterTie",  new Vector2(-130, -40),  new Vector2(18, 40),  HexColor("#E8841A"));
        CreatePanel(parent, "CasterHead", new Vector2(-130, 8),    new Vector2(50, 50),  HexColor("#E8C39E"));

        // 棒グラフの枠（右側）
        CreatePanel(parent, "GraphFrame",      new Vector2(60, 0), new Vector2(220, 220), HexColor("#3D2B1F"));
        CreatePanel(parent, "GraphFrameInner", new Vector2(60, 0), new Vector2(214, 214), HexColor("#FFF8F0"));

        // ベースライン（軸線）
        CreatePanel(parent, "GraphBaseline", new Vector2(60, -90), new Vector2(196, 4), HexColor("#3D2B1F"));

        // 棒グラフ本体（3本・高さと色はNewsControllerが今年の景気に合わせて変更）
        float[] barX = { -10f, 60f, 130f };
        string[] barNames = { "EconomyBar1", "EconomyBar2", "EconomyBar3" };
        for (int i = 0; i < barNames.Length; i++)
        {
            GameObject bar = CreatePanel(parent, barNames[i],
                new Vector2(barX[i], -90), new Vector2(50, 100), HexColor("#B0A48E"));
            // 下端を基準に伸び縮みするようピボットを下中央に変更
            bar.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
        }
    }

    // ── 街の人の意見ページ：2人が話している様子 ──
    static void BuildCitizenIcon(GameObject parent)
    {
        // 街の人 1人目（左）
        CreatePanel(parent, "Citizen1Body", new Vector2(-90, -50), new Vector2(80, 100), HexColor("#C9A063"));
        CreatePanel(parent, "Citizen1Head", new Vector2(-90, 25),  new Vector2(50, 50),  HexColor("#E8C39E"));

        // 街の人 2人目（右）
        CreatePanel(parent, "Citizen2Body", new Vector2(90, -50), new Vector2(80, 100), HexColor("#7FA8C9"));
        CreatePanel(parent, "Citizen2Head", new Vector2(90, 25),  new Vector2(50, 50),  HexColor("#E8C39E"));

        // 会話の吹き出し（2人の間・上部）
        GameObject bubble = CreatePanel(parent, "ChatBubble",
            new Vector2(0, 75), new Vector2(110, 50), HexColor("#FFFFFF"));
        Outline ol = bubble.AddComponent<Outline>();
        ol.effectColor = HexColor("#3D2B1F");
        ol.effectDistance = new Vector2(2, -2);

        // 会話中を示す「・・・」のドット
        float[] dotX = { -25f, 0f, 25f };
        for (int i = 0; i < dotX.Length; i++)
            CreatePanel(bubble, "Dot" + i, new Vector2(dotX[i], 0), new Vector2(14, 14), HexColor("#3D2B1F"));
    }

    // ── 今年の収穫ページ：田んぼに立つ農家の人 ──
    static void BuildFarmerIcon(GameObject parent)
    {
        // 田んぼ（地面）
        CreatePanel(parent, "Field", new Vector2(0, -95), new Vector2(400, 50), HexColor("#9CC76B"));

        // 稲の株（地面から少し顔を出す程度）
        float[] stalkX = { -150f, -110f, 120f, 160f };
        float[] stalkH = { 40f, 46f, 42f, 48f };
        for (int i = 0; i < stalkX.Length; i++)
        {
            float h = stalkH[i];
            CreatePanel(parent, "Stalk" + i,
                new Vector2(stalkX[i], -95f + h * 0.5f), new Vector2(6, h), HexColor("#E8C77E"));
        }

        // 農家の人（中央）
        CreatePanel(parent, "FarmerBody", new Vector2(0, -30), new Vector2(80, 90), HexColor("#7A9A52"));
        CreatePanel(parent, "FarmerHead", new Vector2(0, 40),  new Vector2(50, 50), HexColor("#E8C39E"));
        CreatePanel(parent, "FarmerHat",  new Vector2(0, 73),  new Vector2(90, 16), HexColor("#D9B36A"));
    }

    // ===== BuyScreen レイアウト構築 =====

    [MenuItem("Tools/おこめゲーム/BuyScreenレイアウトを構築（初期フェーズ）")]
    static void BuildBuyScreenLayout()
    {
        GameObject buyScreen = GameObject.Find("BuyScreen");
        if (buyScreen == null) { Debug.LogError("BuyScreenが見つかりません"); return; }

        while (buyScreen.transform.childCount > 0)
            Undo.DestroyObjectImmediate(buyScreen.transform.GetChild(0).gameObject);

        SetImageColor(buyScreen, HexColor("#FAF0D7"));

        // タイトル
        CreateText(buyScreen, "SelectTitle",
            new Vector2(0, 272), new Vector2(900, 44),
            "今年の仕入れ先を選んでください", 26, HexColor("#3D2B1F"), TextAlignmentOptions.Center);

        // ── 3枚カード（クリックで選択）1280×720に収まる配置 ──
        // カード幅340×高さ360、横間隔20px → 合計幅340×3+20×2=1060px（中心から±530→左端-530,右端+530 < 640 ✓）
        string[] cardNames = { "SupplierCard0", "SupplierCard1", "SupplierCard2" };
        float[] cardX = { -360f, 0f, 360f };

        for (int i = 0; i < 3; i++)
        {
            GameObject card = new GameObject(cardNames[i],
                typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(card, "Create " + cardNames[i]);
            card.transform.SetParent(buyScreen.transform, false);
            var rt = card.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(cardX[i], 45);
            rt.sizeDelta        = new Vector2(330, 360);
            card.GetComponent<Image>().color = HexColor("#FFF8F0");
            var cb = card.GetComponent<Button>().colors;
            cb.normalColor      = HexColor("#FFF8F0");
            cb.highlightedColor = HexColor("#FFF0DC");
            cb.pressedColor     = HexColor("#FFE0B0");
            card.GetComponent<Button>().colors = cb;
            card.AddComponent<SupplierCardUI>();

            // 選択枠（オレンジ背景、初期非表示）
            GameObject border = CreatePanel(card, "SelectedBorder",
                Vector2.zero, new Vector2(330, 360), HexColor("#E8841A"));
            border.GetComponent<Image>().color = new Color(0.91f, 0.52f, 0.10f, 0.25f);
            var outline = border.AddComponent<Outline>();
            outline.effectColor    = HexColor("#E8841A");
            outline.effectDistance = new Vector2(4, -4);
            border.SetActive(false);

            // ── ヘッダー（お米の名前）──
            CreatePanel(card, "CardHeader",
                new Vector2(0, 150), new Vector2(310, 44), HexColor("#E8841A"));
            CreateText(card, "RiceNameText",
                new Vector2(0, 150), new Vector2(300, 40),
                "〇〇", 25, Color.white, TextAlignmentOptions.Center);

            // 業者名（小さく）
            CreateText(card, "SupplierNameText",
                new Vector2(0, 114), new Vector2(290, 26),
                "業者名", 15, HexColor("#888888"), TextAlignmentOptions.Center);

            CreatePanel(card, "Divider",
                new Vector2(0, 94), new Vector2(286, 2), HexColor("#E8D0B0"));

            // ── 米ランク ──
            CreateText(card, "LabelRank",
                new Vector2(-80, 64), new Vector2(120, 26), "米ランク", 15, HexColor("#888888"), TextAlignmentOptions.Left);
            CreateText(card, "RankText",
                new Vector2(60, 64), new Vector2(150, 32), "A", 23, HexColor("#3FA34D"), TextAlignmentOptions.Left);

            // ── 流通期限 ──
            CreateText(card, "LabelExpiry",
                new Vector2(-80, 28), new Vector2(120, 26), "流通期限", 15, HexColor("#888888"), TextAlignmentOptions.Left);
            CreateText(card, "ExpiryText",
                new Vector2(58, 28), new Vector2(180, 26), "2002年4月まで", 15, HexColor("#3D2B1F"), TextAlignmentOptions.Left);

            // 「選択中」バッジ（初期非表示・値札の上）
            GameObject badge = CreatePanel(card, "SelectingBadge",
                new Vector2(0, -26), new Vector2(200, 32), HexColor("#E8841A"));
            CreateText(badge, "SelectingLabel",
                Vector2.zero, new Vector2(190, 30), "選択中", 17, Color.white, TextAlignmentOptions.Center);
            badge.SetActive(false);

            // ── 価格（値札スタイル：白い札＋ドロップシャドウ＋右の折り返し、価格はオレンジ太字）──
            // カード下部に配置。折り返し（右の三角）は札の後ろに描画されるよう先に生成
            GameObject fold = CreatePanel(card, "TagFold",
                new Vector2(104, -110), new Vector2(46, 46), HexColor("#CFCFCF"));
            fold.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, 45);
            // 白い値札本体
            GameObject tag = CreatePanel(card, "PriceTag",
                new Vector2(-6, -110), new Vector2(238, 92), Color.white);
            var tagShadow = tag.AddComponent<Shadow>();
            tagShadow.effectColor    = new Color(0f, 0f, 0f, 0.25f);
            tagShadow.effectDistance = new Vector2(3, -4);
            CreateText(tag, "PriceText",
                new Vector2(0, 0), new Vector2(220, 74),
                "<b>40</b><size=58%>万</size>", 40, HexColor("#E8841A"), TextAlignmentOptions.Center);
        }

        // ══════════════════════════════════════════
        // 下段エリア（カード下部 y=-155 〜 y=-340）
        // ══════════════════════════════════════════

        // 購入サマリーパネル（「買う」ボタンの真上、初期非表示）
        // カード下端y=-135、ボタン上端y=-265 → その中間y=-200に配置
        GameObject summaryBg = CreatePanel(buyScreen, "SummaryBg",
            new Vector2(0, -200), new Vector2(440, 85), HexColor("#FFF0DC"));
        // パネルに薄いボーダー
        summaryBg.GetComponent<Image>().color = HexColor("#FFF0DC");
        var sumOutline = summaryBg.AddComponent<Outline>();
        sumOutline.effectColor    = HexColor("#E8D0A0");
        sumOutline.effectDistance = new Vector2(2, -2);
        CreateText(summaryBg, "SummaryText",
            Vector2.zero, new Vector2(420, 80),
            "コシノヒカル × 1 個\n合計：¥280,000", 19, HexColor("#3D2B1F"), TextAlignmentOptions.Center);
        summaryBg.SetActive(false);

        // ── 3ボタン横並び（中央揃え）──
        // 「選びなおす」(-230) | 「買う」(0) | 「販売へ」(+230)
        // 全てy=-295、初期は全て非表示

        // 「選びなおす」（左・グレー）
        GameObject resetBtn = CreateButton(buyScreen, "ResetButton", "選びなおす",
            new Vector2(-230, -295), new Vector2(210, 60));
        SetImageColor(resetBtn, HexColor("#888888"));
        resetBtn.SetActive(false);

        // 「買う」（中央・オレンジ）
        GameObject buyBtn = CreateButton(buyScreen, "BuyButton", "買う",
            new Vector2(0, -295), new Vector2(210, 60));
        SetImageColor(buyBtn, HexColor("#E8841A"));
        buyBtn.SetActive(false);

        // 「販売へ」（右・グリーン）
        GameObject sellBtn = CreateButton(buyScreen, "SellButton", "販売へ",
            new Vector2(230, -295), new Vector2(210, 60));
        SetImageColor(sellBtn, HexColor("#4A9A4A"));
        sellBtn.SetActive(false);

        Selection.activeGameObject = buyScreen;

        // レイアウト後そのまま自動配線（カードのランク・流通期限なども結線）
        RewireBuyScreen();

        // 「ニュースに戻る」ボタンも再生成（レイアウト再構築で消えないよう一緒に作る）
        AddBackToNewsButton();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("✓ BuyScreen構築＋自動配線が完了しました（価格を強調・米ランク/流通期限・ニュースに戻るボタンつき）。");
    }

    // ════════════════════════════════════════════════════════
    // SellScreen レイアウト + 自動配線（1ボタン完結）
    // ════════════════════════════════════════════════════════

    [MenuItem("Tools/おこめゲーム/SellScreenを構築して配線")]
    static void BuildAndWireSellScreen()
    {
        GameObject sellScreen = GameObject.Find("SellScreen");
        if (sellScreen == null) { Debug.LogError("SellScreenが見つかりません"); return; }

        while (sellScreen.transform.childCount > 0)
            Undo.DestroyObjectImmediate(sellScreen.transform.GetChild(0).gameObject);

        SetImageColor(sellScreen, HexColor("#FAF0D7"));

        // ══════════════════════════════════════════════════════
        // 座標設計（ステージローカル座標、ステージ 1200×350）
        //  ステージ中心=(0,0)、top=+175、bottom=-175、left=-600、right=+600
        //  地面top: y=-155  →  キャラ足元
        //  キャラ中心: y=-155+26=-129（BuildCustomer/Shopkeeperの底辺 -26から算出）
        //  カウンター表面: y=-113（キャラ中心+16=胸あたり）
        //  カウンター前面: bottom=-155 to top=-113、height=42、center=-134
        //  店員x=-245、お客さん開始x=640、カウンター前x=-165
        // ══════════════════════════════════════════════════════

        const float GROUND_TOP    = -155f;
        const float CHAR_Y        = -129f; // キャラコンテナ中心
        const float COUNTER_Y     = -113f; // カウンター表面
        const float COUNTER_FACE_X= -165f; // カウンター前面のX
        const float SHOPKEEPER_X  = -245f;
        const float CUST_TARGET_X = -148f; // お客さんがカウンター前で止まる位置
        const float PILE_X        = -530f; // 袋の山の中心

        // ── 進捗ドット（中央上部、40px間隔で均等配置）──
        GameObject dotsParent = new GameObject("DotsContainer", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(dotsParent, "DotsContainer");
        dotsParent.transform.SetParent(sellScreen.transform, false);
        dotsParent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 262);
        dotsParent.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 28);

        var dotImages = new List<Image>();
        for (int i = 0; i < 12; i++)
        {
            // 12個を40px間隔で中央揃え: start=-220, end=+220
            float dotX = -220f + i * 40f;
            GameObject dot = CreatePanel(dotsParent, "Dot" + i,
                new Vector2(dotX, 0), new Vector2(24, 24),
                new Color(0.85f, 0.80f, 0.75f, 0.5f));
            var ol = dot.AddComponent<Outline>();
            ol.effectColor = HexColor("#C8A870"); ol.effectDistance = new Vector2(2,-2);
            dotImages.Add(dot.GetComponent<Image>());
            dot.SetActive(false);
        }

        // ── ステージ（RectMask2Dでお客さんをクリッピング）──
        // GlobalTopBar下端y=300。ステージ中心y=15、高さ350 → top=190, bottom=-160 ✓
        GameObject stage = CreatePanel(sellScreen, "SellStage",
            new Vector2(0, 15), new Vector2(1200, 350),
            HexColor("#D4EAF0")); // 空色を背景に
        stage.AddComponent<RectMask2D>();

        // 地面（全幅、底部）
        CreatePanel(stage, "Ground",
            new Vector2(0, GROUND_TOP - 7f), new Vector2(1200, 20),
            HexColor("#C8A870"));

        // 店の壁（左40%）x=-600〜x=-120
        CreatePanel(stage, "ShopWall",
            new Vector2(-360f, 0), new Vector2(480f, 350),
            HexColor("#F5E6D0"));

        // カウンター：一体型ブロック（地面からカウンター天板まで）
        //   x=-600 〜 x=COUNTER_FACE_X=-165  幅=435px  中心x=-382
        float cfH    = COUNTER_Y - GROUND_TOP;            // 42px
        float cfCenY = GROUND_TOP + cfH * 0.5f;           // -134
        float cfW    = Mathf.Abs(COUNTER_FACE_X - (-600f)); // 435
        float cfCenX = (-600f + COUNTER_FACE_X) * 0.5f;   // -382

        // ブロック本体（前面の木目色）
        CreatePanel(stage, "CounterBlock",
            new Vector2(cfCenX, cfCenY), new Vector2(cfW, cfH),
            HexColor("#B8885A"));

        // カウンター天板（少し濃い色の帯）
        CreatePanel(stage, "CounterTop",
            new Vector2(cfCenX, COUNTER_Y), new Vector2(cfW, 11),
            HexColor("#A0784A"));

        // カウンター右端（お客さん向きの縦縁）
        CreatePanel(stage, "CounterEdge",
            new Vector2(COUNTER_FACE_X + 4f, cfCenY - 2f), new Vector2(8, cfH + 12),
            HexColor("#8A6030"));

        // ── 店員（カウンターより先にaddしてcounterが上に描画されるよう）──
        // 店員はカウンターの後ろに立ち、下半身がカウンターで隠れる
        BuildShopkeeper(stage, new Vector2(SHOPKEEPER_X, CHAR_Y));

        // ── お米袋の山（カウンター天板上）──
        // pileのy=COUNTER_Y+25: 袋(高さ50)の中心を天板上25pxに置く
        GameObject pile = new GameObject("RicePile", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(pile, "RicePile");
        pile.transform.SetParent(stage.transform, false);
        pile.GetComponent<RectTransform>().anchoredPosition = new Vector2(PILE_X, COUNTER_Y + 25f);
        pile.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 80);

        var bagNameTexts = new List<TextMeshProUGUI>();
        for (int i = 0; i < 3; i++) // 左の山
            BuildRiceBag(pile, "BagL" + i, new Vector2(i * 8f, i * 12f), bagNameTexts);
        for (int i = 0; i < 2; i++) // 右の山
            BuildRiceBag(pile, "BagR" + i, new Vector2(80 + i * 8f, i * 12f), bagNameTexts);

        // ── お客さんコンテナ（アニメーション用）──
        GameObject custContainer = new GameObject("CustomerContainer", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(custContainer, "CustomerContainer");
        custContainer.transform.SetParent(stage.transform, false);
        var custRT = custContainer.GetComponent<RectTransform>();
        custRT.anchoredPosition = new Vector2(640f, CHAR_Y);
        custRT.sizeDelta = new Vector2(44, 70);
        BuildCustomer(custContainer);
        custContainer.SetActive(false);

        // ── 持ち帰り袋コンテナ（アニメーション用）──
        GameObject carryContainer = new GameObject("CarryBagContainer", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(carryContainer, "CarryBagContainer");
        carryContainer.transform.SetParent(stage.transform, false);
        var carryRT = carryContainer.GetComponent<RectTransform>();
        carryRT.anchoredPosition = new Vector2(678f, CHAR_Y - 10f);
        carryRT.sizeDelta = new Vector2(28, 38);
        BuildCarryBag(carryContainer);
        carryContainer.SetActive(false);

        // ── 吹き出し（アニメーション用）──
        // キャラ中心CHAR_Y=-129、頭top≈-129+32=-97、吹き出しはその上
        GameObject bubble = CreatePanel(stage, "BubbleContainer",
            new Vector2(CUST_TARGET_X + 20f, CHAR_Y + 70f), new Vector2(190, 42),
            Color.white);
        var bOl = bubble.AddComponent<Outline>();
        bOl.effectColor = HexColor("#E8D0B0"); bOl.effectDistance = new Vector2(2,-2);
        CreateText(bubble, "BubbleText",
            Vector2.zero, new Vector2(178, 36), "・・・", 21,
            HexColor("#3D2B1F"), TextAlignmentOptions.Center);
        bubble.SetActive(false);

        // ── ステージ外：集計テキスト + つぎへボタン ──
        CreateText(sellScreen, "RunningTotalText",
            new Vector2(0, -185), new Vector2(700, 36),
            "", 22, HexColor("#3D2B1F"), TextAlignmentOptions.Center);

        GameObject nextBtn = CreateButton(sellScreen, "SellNextButton", "つぎへ",
            new Vector2(0, -280), new Vector2(180, 55));
        nextBtn.SetActive(false);

        // ── 自動配線 ──
        AutoWireSellScreenV2(sellScreen, dotImages, bagNameTexts);

        MarkDirtyAndLog("SellScreen 構築+配線完了");
    }

    // ── お米袋1枚を生成 ──────────────────────────────────
    static GameObject BuildRiceBag(GameObject parent, string name, Vector2 offset,
                                    List<TextMeshProUGUI> bagNames)
    {
        GameObject bag = CreatePanel(parent, name, offset, new Vector2(40, 50), HexColor("#F8F6EE"));
        // 黒枠
        var outline = bag.AddComponent<Outline>();
        outline.effectColor    = new Color(0.15f, 0.15f, 0.15f, 1f);
        outline.effectDistance = new Vector2(2, -2);
        // 上帯
        CreatePanel(bag, "TopBand", new Vector2(0, 19), new Vector2(40, 12), HexColor("#E8841A"));
        CreateText(bag, "TopText", new Vector2(0, 19), new Vector2(38, 10),
            "新米", 7, Color.white, TextAlignmentOptions.Center);
        // 中央・品種名
        CreateText(bag, "KanjiText", new Vector2(0, 3), new Vector2(38, 18),
            "米", 16, HexColor("#2A2A2A"), TextAlignmentOptions.Center);
        var nameT = bag.transform.Find("KanjiText")?.GetComponent<TextMeshProUGUI>();
        // 品種テキスト（別行）
        GameObject nameObj = new GameObject("BagNameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(nameObj, "BagNameText");
        nameObj.transform.SetParent(bag.transform, false);
        nameObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -6);
        nameObj.GetComponent<RectTransform>().sizeDelta = new Vector2(38, 12);
        var nt = nameObj.GetComponent<TextMeshProUGUI>();
        nt.text = "コシノヒカル"; nt.fontSize = 6; nt.color = HexColor("#3D2B1F");
        nt.alignment = TextAlignmentOptions.Center;
        bagNames.Add(nt);
        // 下帯
        CreatePanel(bag, "BotBand", new Vector2(0, -19), new Vector2(40, 11), HexColor("#E8841A"));
        CreateText(bag, "BotText", new Vector2(0, -19), new Vector2(38, 9),
            "10kg", 7, Color.white, TextAlignmentOptions.Center);
        return bag;
    }

    // ── 店員を生成 ──────────────────────────────────────
    static void BuildShopkeeper(GameObject parent, Vector2 pos)
    {
        GameObject sk = new GameObject("Shopkeeper", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(sk, "Shopkeeper");
        sk.transform.SetParent(parent.transform, false);
        sk.GetComponent<RectTransform>().anchoredPosition = pos;
        sk.GetComponent<RectTransform>().sizeDelta = new Vector2(32, 70);

        // 髪
        CreatePanel(sk, "Hair", new Vector2(0, 29), new Vector2(30, 8), HexColor("#4A2800"));
        // 頭
        var head = CreatePanel(sk, "Head", new Vector2(0, 17), new Vector2(28, 28), HexColor("#FAD5A5"));
        var headOutline = head.AddComponent<Outline>();
        headOutline.effectColor = HexColor("#E8C090"); headOutline.effectDistance = new Vector2(2,-2);
        // 目（客側＝右側を向く）
        CreatePanel(head, "EyeR", new Vector2(5, 2), new Vector2(5, 5), HexColor("#2A1800"));
        CreatePanel(head, "EyeL", new Vector2(-3, 2), new Vector2(5, 5), HexColor("#2A1800"));
        // 体（白エプロン）
        var body = CreatePanel(sk, "Body", new Vector2(0, -10), new Vector2(28, 32), Color.white);
        var bodyOutline = body.AddComponent<Outline>();
        bodyOutline.effectColor = HexColor("#DDDDDD"); bodyOutline.effectDistance = new Vector2(1,-1);
        // エプロン
        CreatePanel(body, "Apron", new Vector2(0, -6), new Vector2(20, 20), HexColor("#E8F0FF"));
        // 腕（客方向に伸ばす）
        CreatePanel(sk, "ArmR", new Vector2(18, -4), new Vector2(13, 7), HexColor("#FAD5A5"));
    }

    // ── お客さんを生成 ──────────────────────────────────
    static void BuildCustomer(GameObject parent)
    {
        CreatePanel(parent, "CHair", new Vector2(0, 29), new Vector2(32, 7), HexColor("#4A3820"));
        var head = CreatePanel(parent, "CHead", new Vector2(0, 17), new Vector2(26, 26), HexColor("#FAD5A5"));
        var ho = head.AddComponent<Outline>();
        ho.effectColor = HexColor("#E8C090"); ho.effectDistance = new Vector2(2,-2);
        // 目（左向き＝カウンター向き）
        CreatePanel(head, "EyeL", new Vector2(-5, 2), new Vector2(5, 5), HexColor("#2A1800"));
        CreatePanel(head, "EyeR", new Vector2(3, 2), new Vector2(5, 5), HexColor("#2A1800"));
        var body = CreatePanel(parent, "CBody", new Vector2(0, -10), new Vector2(24, 32), HexColor("#4A90D9"));
        var bo = body.AddComponent<Outline>();
        bo.effectColor = HexColor("#2A6AAA"); bo.effectDistance = new Vector2(2,-2);
    }

    // ── 持ち帰り袋を生成 ────────────────────────────────
    static void BuildCarryBag(GameObject parent)
    {
        var bag = CreatePanel(parent, "CBag", Vector2.zero, new Vector2(28, 36), HexColor("#F8F6EE"));
        var o = bag.AddComponent<Outline>();
        o.effectColor = new Color(0.15f,0.15f,0.15f,1f); o.effectDistance = new Vector2(2,-2);
        CreatePanel(bag, "CBTop", new Vector2(0, 13), new Vector2(28, 10), HexColor("#E8841A"));
        CreateText(bag, "CBKanji", new Vector2(0, 0), new Vector2(26, 18),
            "米", 14, HexColor("#2A2A2A"), TextAlignmentOptions.Center);
    }

    // ── 新・SellScreen自動配線 ──────────────────────────
    static void AutoWireSellScreenV2(GameObject sellScreen,
                                      List<Image> dotImages,
                                      List<TextMeshProUGUI> bagNameTexts)
    {
        var sc = sellScreen.GetComponent<SellController>();
        if (sc == null) sc = Undo.AddComponent<SellController>(sellScreen);

        var gc = Object.FindObjectOfType<GameController>();
        var mm = Object.FindObjectOfType<MarketManager>();
        var jm = Object.FindObjectOfType<JudgeManager>();

        var stage    = FindInChildren(sellScreen, "SellStage");
        var custCont = FindInChildren(sellScreen, "CustomerContainer");
        var carryCont= FindInChildren(sellScreen, "CarryBagContainer");
        var bubbleCont = FindInChildren(sellScreen, "BubbleContainer");

        var so = new SerializedObject(sc);

        // RectTransform references
        SetField(so, "customerRT",  custCont?.GetComponent<RectTransform>());
        SetField(so, "carryBagRT",  carryCont?.GetComponent<RectTransform>());
        SetField(so, "bubbleRT",    bubbleCont?.GetComponent<RectTransform>());
        SetField(so, "bubbleText",  FindInChildren(sellScreen, "BubbleText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "bubbleBg",    bubbleCont?.GetComponent<Image>());
        SetField(so, "runningTotalText", FindInChildren(sellScreen, "RunningTotalText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "nextButton",  FindInChildren(sellScreen, "SellNextButton"));
        SetField(so, "gameController", gc);
        SetField(so, "marketManager",  mm);
        SetField(so, "judgeManager",   jm);

        // ProgressDots配列
        SerializedProperty dotsProp = so.FindProperty("progressDots");
        if (dotsProp != null)
        {
            dotsProp.arraySize = dotImages.Count;
            for (int i = 0; i < dotImages.Count; i++)
                dotsProp.GetArrayElementAtIndex(i).objectReferenceValue = dotImages[i];
        }

        // RiceBagNameTexts配列
        SerializedProperty bagProp = so.FindProperty("riceBagNameTexts");
        if (bagProp != null)
        {
            bagProp.arraySize = bagNameTexts.Count;
            for (int i = 0; i < bagNameTexts.Count; i++)
                bagProp.GetArrayElementAtIndex(i).objectReferenceValue = bagNameTexts[i];
        }

        so.ApplyModifiedProperties();

        if (gc != null) WireButton("SellNextButton", gc, "OnSellComplete");
        Debug.Log("✓ SellScreen配線完了（ドット:" + dotImages.Count + "個、袋テキスト:" + bagNameTexts.Count + "個）");
    }

    static void AutoWireSellScreen(GameObject sellScreen)
    {
        var sc = sellScreen.GetComponent<SellController>();
        if (sc == null) sc = Undo.AddComponent<SellController>(sellScreen);

        // GameManagerの名前に依存しないようにFindObjectOfTypeを使用
        var gc = Object.FindObjectOfType<GameController>();
        var mm = Object.FindObjectOfType<MarketManager>();
        var jm = Object.FindObjectOfType<JudgeManager>();

        if (gc == null) { Debug.LogWarning("GameControllerが見つかりません。手動で設定してください。"); }
        if (mm == null) { Debug.LogWarning("MarketManagerが見つかりません。手動で設定してください。"); }
        if (jm == null) { Debug.LogWarning("JudgeManagerが見つかりません。手動で設定してください。"); }

        var so = new SerializedObject(sc);
        SetField(so, "marketInfoText",       FindInChildren(sellScreen, "MarketInfoText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "stockDisplayText",     FindInChildren(sellScreen, "StockDisplayText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "customerPanel",        FindInChildren(sellScreen, "CustomerPanel"));
        SetField(so, "customerNumberText",   FindInChildren(sellScreen, "CustomerNumberText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "customerDecisionText", FindInChildren(sellScreen, "CustomerDecisionText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "customerDecisionBg",   FindInChildren(sellScreen, "CustomerDecisionBg")?.GetComponent<Image>());
        SetField(so, "runningTotalText",     FindInChildren(sellScreen, "RunningTotalText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "summaryPanel",         FindInChildren(sellScreen, "SummaryPanel"));
        SetField(so, "summaryText",          FindInChildren(sellScreen, "SummaryText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "nextButton",           FindInChildren(sellScreen, "SellNextButton"));
        SetField(so, "gameController",       gc);
        SetField(so, "marketManager",        mm);
        SetField(so, "judgeManager",         jm);
        so.ApplyModifiedProperties();

        // SellNextButton → GameController.OnSellComplete
        if (gc != null) WireButton("SellNextButton", gc, "OnSellComplete");
    }

    // ════════════════════════════════════════════════════════
    // NightScreen レイアウト + 自動配線（1ボタン完結）
    // ════════════════════════════════════════════════════════

    [MenuItem("Tools/おこめゲーム/NightScreenを構築して配線")]
    static void BuildAndWireNightScreen()
    {
        GameObject nightScreen = GameObject.Find("NightScreen");
        if (nightScreen == null) { Debug.LogError("NightScreenが見つかりません"); return; }

        while (nightScreen.transform.childCount > 0)
            Undo.DestroyObjectImmediate(nightScreen.transform.GetChild(0).gameObject);

        SetImageColor(nightScreen, HexColor("#FAF0D7"));

        // 結果パネル（GlobalTopBar下端y=300。パネル上端y=270 ✓）
        GameObject resultBg = CreatePanel(nightScreen, "ResultBg",
            new Vector2(0, 30), new Vector2(700, 460), HexColor("#FFF8F0"));

        // 左カラム：項目名（左揃え・TopLeft）
        CreateText(resultBg, "NightResultText",
            new Vector2(-100, 0), new Vector2(300, 440),
            "項目がここに", 28, HexColor("#3D2B1F"), TextAlignmentOptions.TopLeft);

        // 区切り線（縦）
        CreatePanel(resultBg, "Divider",
            new Vector2(80, 0), new Vector2(2, 440), HexColor("#E8D0B0"));

        // 右カラム：金額（右揃え・TopRight）
        CreateText(resultBg, "NightValueText",
            new Vector2(240, 0), new Vector2(280, 440),
            "金額がここに", 28, HexColor("#3D2B1F"), TextAlignmentOptions.TopRight);

        // 「つぎへ」ボタン
        GameObject nextBtn = CreateButton(nightScreen, "NextDayButton", "つぎへ",
            new Vector2(0, -270), new Vector2(200, 60));

        // ── 自動配線 ──
        AutoWireNightScreen(nightScreen, nextBtn);

        MarkDirtyAndLog("NightScreen 構築+配線");
    }

    static void AutoWireNightScreen(GameObject nightScreen, GameObject nextDayButton)
    {
        var gc = Object.FindObjectOfType<GameController>();
        if (gc == null) { Debug.LogWarning("GameControllerが見つかりません"); return; }

        var so = new SerializedObject(gc);
        SetField(so, "resultText",      FindInChildren(nightScreen, "NightResultText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "resultValueText", FindInChildren(nightScreen, "NightValueText")?.GetComponent<TextMeshProUGUI>());
        so.ApplyModifiedProperties();

        WireButton("NextDayButton", gc, "OnNextDayButton");
    }

    // ════════════════════════════════════════════════════════
    // EndingScreen レイアウト + 配線（成功＝お祝い／失敗＝ザンネーン）
    // ════════════════════════════════════════════════════════

    [MenuItem("Tools/おこめゲーム/EndingScreenを構築して配線")]
    static void BuildAndWireEndingScreen()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("Canvasが見つかりません"); return; }

        GameObject ending = FindDeep("EndingScreen"); // 非アクティブでも検出（重複生成防止）
        if (ending == null)
        {
            ending = new GameObject("EndingScreen", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(ending, "Create EndingScreen");
            ending.transform.SetParent(canvas.transform, false);
            var ert = ending.GetComponent<RectTransform>();
            ert.anchorMin = Vector2.zero; ert.anchorMax = Vector2.one;
            ert.sizeDelta = Vector2.zero; ert.anchoredPosition = Vector2.zero;
        }
        else
        {
            while (ending.transform.childCount > 0)
                Undo.DestroyObjectImmediate(ending.transform.GetChild(0).gameObject);
        }
        SetImageColor(ending, HexColor("#2A2620"));

        // ── お祝い演出（成功時）──
        GameObject celebrate = CreateGroup(ending, "CelebrationGroup");
        CreatePanel(celebrate, "CelebBg",  Vector2.zero,      new Vector2(1300, 760), HexColor("#E8841A"));
        CreatePanel(celebrate, "CelebTop", new Vector2(0,330),new Vector2(1300, 110), HexColor("#FFC94D"));
        BuildConfetti(celebrate);
        BuildTrophy(celebrate);
        CreateText(celebrate, "CelebrateBanner",
            new Vector2(0, 240), new Vector2(1100, 100),
            "おめでとう！", 70, HexColor("#FFFFFF"), TextAlignmentOptions.Center);

        // ── ザンネーン演出（失敗時）──
        GameObject sad = CreateGroup(ending, "SadGroup");
        CreatePanel(sad, "SadBg",     Vector2.zero,        new Vector2(1300, 760), HexColor("#3A4654"));
        CreatePanel(sad, "SadGround", new Vector2(0,-330), new Vector2(1300, 120), HexColor("#2C3640"));
        BuildRain(sad);
        BuildDroop(sad);
        CreateText(sad, "SadBanner",
            new Vector2(0, 240), new Vector2(1100, 100),
            "ザンネーン…", 70, HexColor("#9FB4C8"), TextAlignmentOptions.Center);

        celebrate.SetActive(false);
        sad.SetActive(false);

        // ── 結果テキスト（両演出の前面・共通）──
        GameObject resultPanel = CreatePanel(ending, "EndingResultPanel",
            new Vector2(0, -30), new Vector2(640, 300), new Color(1f, 1f, 1f, 0.92f));
        var rpOl = resultPanel.AddComponent<Outline>();
        rpOl.effectColor = HexColor("#3D2B1F"); rpOl.effectDistance = new Vector2(3, -3);
        CreateText(resultPanel, "EndingText",
            Vector2.zero, new Vector2(600, 280),
            "結果がここに表示されます", 27, HexColor("#3D2B1F"), TextAlignmentOptions.Center);

        // ── 「もう一度」ボタン ──
        CreateButton(ending, "RetryButton", "もう一度",
            new Vector2(0, -255), new Vector2(220, 64));

        // ── 配線 ──
        var gc = Object.FindObjectOfType<GameController>();
        var sm = Object.FindObjectOfType<ScreenManager>();
        if (gc != null)
        {
            var so = new SerializedObject(gc);
            SetField(so, "endingText",              FindInChildren(ending, "EndingText")?.GetComponent<TextMeshProUGUI>());
            SetField(so, "endingCelebrationGroup",  celebrate);
            SetField(so, "endingSadGroup",          sad);
            so.ApplyModifiedProperties();
            WireButton("RetryButton", gc, "RestartGame");
        }
        if (sm != null)
        {
            var soSm = new SerializedObject(sm);
            SetField(soSm, "endingScreen", ending);
            soSm.ApplyModifiedProperties();
        }

        ending.transform.SetAsLastSibling();
        MarkDirtyAndLog("EndingScreen 構築+配線");
    }

    // 紙吹雪（ランダムな色・回転の小片）
    static void BuildConfetti(GameObject parent)
    {
        string[] cols = { "#FF5D5D", "#FFD24D", "#5DD0FF", "#7DDB6B", "#FF8AD1", "#FFFFFF" };
        var rnd = new System.Random(12345);
        for (int i = 0; i < 40; i++)
        {
            float x = rnd.Next(-620, 620);
            float y = rnd.Next(-110, 360);
            var piece = CreatePanel(parent, "Confetti" + i,
                new Vector2(x, y), new Vector2(16, 24), HexColor(cols[i % cols.Length]));
            piece.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, rnd.Next(0, 360));
        }
    }

    // 金トロフィー
    static void BuildTrophy(GameObject parent)
    {
        var cup = CreatePanel(parent, "TrophyCup", new Vector2(0, 70), new Vector2(130, 95), HexColor("#FFD24D"));
        CreatePanel(cup, "HandleL", new Vector2(-78, 8), new Vector2(34, 56), HexColor("#FFC94D"));
        CreatePanel(cup, "HandleR", new Vector2(78, 8),  new Vector2(34, 56), HexColor("#FFC94D"));
        CreatePanel(parent, "TrophyStem", new Vector2(0, 8),   new Vector2(26, 40), HexColor("#E0A93A"));
        CreatePanel(parent, "TrophyBase", new Vector2(0, -18), new Vector2(100, 20), HexColor("#C8902E"));
    }

    // 雨（斜めの細い線）
    static void BuildRain(GameObject parent)
    {
        var rnd = new System.Random(54321);
        for (int i = 0; i < 50; i++)
        {
            float x = rnd.Next(-620, 620);
            float y = rnd.Next(-100, 360);
            var drop = CreatePanel(parent, "Rain" + i,
                new Vector2(x, y), new Vector2(3, 26), HexColor("#6E8AA6"));
            drop.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, 18);
        }
    }

    // しょんぼり顔（涙つき）
    static void BuildDroop(GameObject parent)
    {
        var face = CreatePanel(parent, "SadFace", new Vector2(0, 70), new Vector2(150, 150), HexColor("#D9C7A0"));
        CreatePanel(face, "EyeL", new Vector2(-38, 22), new Vector2(20, 20), HexColor("#3A4654"));
        CreatePanel(face, "EyeR", new Vector2(38, 22),  new Vector2(20, 20), HexColor("#3A4654"));
        CreatePanel(face, "Tear", new Vector2(-38, -8), new Vector2(11, 30), HexColor("#6EC6FF"));
        CreatePanel(face, "Mouth", new Vector2(0, -42), new Vector2(64, 10), HexColor("#3A4654"));
    }

    // ════════════════════════════════════════════════════════
    // WebGL ビルド（ブラウザ向け書き出し）
    // ════════════════════════════════════════════════════════

    [MenuItem("Tools/おこめゲーム/WebGLビルド")]
    static void BuildWebGL()
    {
        string folder = EditorUtility.SaveFolderPanel("WebGLビルドの出力先を選択", "", "OkomeGame_WebGL");
        if (string.IsNullOrEmpty(folder)) { Debug.Log("ビルドをキャンセルしました"); return; }

        // プロジェクトのルート/Assets等を直接出力先にできないので、選んだ場合はサブフォルダを作る
        string projectRoot = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, ".."));
        string chosen = System.IO.Path.GetFullPath(folder);
        if (chosen == projectRoot ||
            chosen == System.IO.Path.GetFullPath(Application.dataPath) ||
            chosen.StartsWith(System.IO.Path.GetFullPath(System.IO.Path.Combine(projectRoot, "Library"))))
        {
            folder = System.IO.Path.Combine(chosen, "WebGLBuild");
            Debug.Log("出力先にプロジェクト直下が選ばれたため、サブフォルダ " + folder + " に出力します。");
        }

        var scenes = new List<string>();
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled) scenes.Add(s.path);
        if (scenes.Count == 0)
            scenes.Add(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);

        var options = new BuildPlayerOptions
        {
            scenes           = scenes.ToArray(),
            locationPathName = folder,
            target           = BuildTarget.WebGL,
            options          = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log("✓ WebGLビルド成功：" + folder + "\nローカルサーバで配信して index.html を開いてください。");
        else
            Debug.LogError("✗ WebGLビルド失敗：" + report.summary.result +
                "\nWebGLモジュール未導入の可能性。Unity Hub → Installs → Add Modules → WebGL Build Support を確認してください。");
    }

    // ── GitHub Pages 公開用：リポジトリの docs/ へ直接ビルド ──
    [MenuItem("Tools/おこめゲーム/GitHub Pages用にWebGLビルド（docs/へ出力）")]
    static void BuildWebGLForPages()
    {
        // GitHub Pages は Brotli/Gzip を適切なヘッダで配信できないため圧縮を無効化
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        string projectRoot = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, ".."));
        string docs = System.IO.Path.Combine(projectRoot, "docs");

        var scenes = new List<string>();
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled) scenes.Add(s.path);
        if (scenes.Count == 0)
            scenes.Add(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);

        var options = new BuildPlayerOptions
        {
            scenes           = scenes.ToArray(),
            locationPathName = docs,
            target           = BuildTarget.WebGL,
            options          = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            // Jekyll処理を無効化（Unity出力をそのまま配信させる）
            System.IO.File.WriteAllText(System.IO.Path.Combine(docs, ".nojekyll"), "");
            Debug.Log("✓ docs/ へWebGLビルド成功。\n次の手順:\n" +
                "1) git add docs && git commit -m \"Add WebGL build\" && git push\n" +
                "2) GitHub → Settings → Pages → Source=Deploy from a branch, Branch=main /docs\n" +
                "3) 数分後 https://yut122.github.io/Okome_Game/ で公開");
        }
        else
            Debug.LogError("✗ ビルド失敗：" + report.summary.result +
                "\nWebGLモジュール未導入の可能性。Unity Hub → Installs → Add Modules → WebGL Build Support を確認してください。");
    }

    // ════════════════════════════════════════════════════════
    // BuyScreen に「ニュースに戻る」ボタンを追加（左上・常時表示）
    // 既存ボタンの配置は一切変更しない
    // ════════════════════════════════════════════════════════

    [MenuItem("Tools/おこめゲーム/BuyScreenに「ニュースに戻る」ボタンを追加")]
    static void AddBackToNewsButton()
    {
        GameObject buyScreen = GameObject.Find("BuyScreen");
        if (buyScreen == null) { Debug.LogError("BuyScreenが見つかりません"); return; }

        // 既存があれば作り直し（重複防止）
        GameObject existing = FindInChildren(buyScreen, "BackToNewsButton");
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        // 左上に配置（タイトル左横・カードの上。他ボタンには干渉しない）
        GameObject backBtn = CreateButton(buyScreen, "BackToNewsButton", "ニュースに戻る",
            new Vector2(-545, 270), new Vector2(190, 52));
        SetImageColor(backBtn, HexColor("#888888")); // 副ボタンらしくグレー
        var t = FindInChildren(backBtn, "Text")?.GetComponent<TextMeshProUGUI>();
        if (t != null) t.fontSize = 22;
        backBtn.transform.SetAsLastSibling(); // 常に前面・表示のまま

        // ScreenManager.ShowNews に配線
        var sm = Object.FindObjectOfType<ScreenManager>();
        if (sm != null) WireButton("BackToNewsButton", sm, "ShowNews");
        else Debug.LogWarning("ScreenManagerが見つかりません。手動で配線してください。");

        MarkDirtyAndLog("BuyScreenに「ニュースに戻る」ボタンを追加");
    }

    // ════════════════════════════════════════════════════════
    // BuyScreen 配線のみ更新（レイアウト変更なし）
    // ════════════════════════════════════════════════════════

    [MenuItem("Tools/おこめゲーム/BuyScreen配線を更新")]
    static void RewireBuyScreen()
    {
        var gc = GetComponent<GameController>("GameManager");
        var sm = GetComponent<ScreenManager>("GameManager");
        if (gc == null) return;

        var so = new SerializedObject(gc);

        // GlobalTopBar
        SetField(so, "moneyText",  FindDeep("GlobalMoneyText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "stockText",  FindDeep("GlobalStockText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "dayText",    FindDeep("GlobalDayText")?.GetComponent<TextMeshProUGUI>());

        // BuyScreen
        SetField(so, "summaryText", FindDeep("SummaryText")?.GetComponent<TextMeshProUGUI>());
        SetField(so, "summaryBox",  FindDeep("SummaryBg"));
        SetField(so, "buyButton",   FindDeep("BuyButton"));
        SetField(so, "resetButton", FindDeep("ResetButton"));
        SetField(so, "sellButton",  FindDeep("SellButton"));
        SetField(so, "judge",       GetComponent<JudgeManager>("GameManager"));
        SetField(so, "market",      GetComponent<MarketManager>("GameManager"));
        SetField(so, "screenManager", sm);

        // Supplier Cards
        var cards = new GameObject[] {
            FindDeep("SupplierCard0"),
            FindDeep("SupplierCard1"),
            FindDeep("SupplierCard2")
        };
        SerializedProperty cardsProp = so.FindProperty("supplierCards");
        if (cardsProp != null)
        {
            cardsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var elemProp = cardsProp.GetArrayElementAtIndex(i);
                elemProp.objectReferenceValue = cards[i]?.GetComponent<SupplierCardUI>();
            }
        }

        so.ApplyModifiedProperties();

        // ボタン配線
        WireButton("BuyButton",   gc, "OnBuyButton");
        WireButton("ResetButton", gc, "OnResetButton");
        WireButton("SellButton",  gc, "OnSellButton");

        // 各カードのOnClick
        for (int i = 0; i < 3; i++)
        {
            if (cards[i] == null) continue;
            var cardUi = cards[i].GetComponent<SupplierCardUI>();
            if (cardUi == null) continue;

            // SupplierCardUI のフィールド設定
            var cso = new SerializedObject(cardUi);
            SetField(cso, "cardBackground",  cards[i].GetComponent<Image>());
            SetField(cso, "selectedBorder",  FindInChildren(cards[i], "SelectedBorder"));
            SetField(cso, "selectingBadge",  FindInChildren(cards[i], "SelectingBadge"));
            SetField(cso, "gameController",  gc);
            SetField(cso, "supplierNameText", FindInChildrenTMP(cards[i], "SupplierNameText"));
            SetField(cso, "riceNameText",    FindInChildrenTMP(cards[i], "RiceNameText"));
            SetField(cso, "priceText",       FindInChildrenTMP(cards[i], "PriceText"));
            SetField(cso, "rankText",        FindInChildrenTMP(cards[i], "RankText"));
            SetField(cso, "expiryText",      FindInChildrenTMP(cards[i], "ExpiryText"));
            SetField(cso, "volumeText",      FindInChildrenTMP(cards[i], "VolumeText"));
            cso.ApplyModifiedProperties();

            // カード Button → SupplierCardUI.OnCardClicked
            var btn = cards[i].GetComponent<Button>();
            if (btn != null)
            {
                Undo.RecordObject(btn, "Wire Card Button");
                btn.onClick.RemoveAllListeners();
                var method = cardUi.GetType().GetMethod("OnCardClicked");
                if (method != null)
                {
                    var action = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), cardUi, method);
                    UnityEventTools.AddPersistentListener(btn.onClick, action);
                }
            }
        }

        MarkDirtyAndLog("BuyScreen配線を更新");
    }

    // ── 子オブジェクト検索ヘルパー ──────────────────────────

    static GameObject FindInChildren(GameObject parent, string name)
    {
        if (parent == null) return null;
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t.gameObject;
        return null;
    }

    static TextMeshProUGUI FindInChildrenTMP(GameObject parent, string name)
        => FindInChildren(parent, name)?.GetComponent<TextMeshProUGUI>();

    // ===== UIパーツ生成ヘルパー =====

    static GameObject CreatePanel(GameObject parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        obj.GetComponent<Image>().color = color;
        return obj;
    }

    static void CreateText(GameObject parent, string name, Vector2 pos, Vector2 size,
                           string defaultText, int fontSize, Color color, TextAlignmentOptions align)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
    }

    static GameObject CreateButton(GameObject parent, string name, string label, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        obj.GetComponent<Image>().color = HexColor("#E8841A");
        ColorBlock cb = obj.GetComponent<Button>().colors;
        cb.normalColor = HexColor("#E8841A");
        cb.highlightedColor = HexColor("#C8621A");
        obj.GetComponent<Button>().colors = cb;

        // ボタンテキスト
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(textObj, "Create ButtonText");
        textObj.transform.SetParent(obj.transform, false);
        RectTransform trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 30;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        return obj;
    }

    static void SetImageColor(GameObject obj, Color color)
    {
        Image img = obj.GetComponent<Image>();
        if (img == null) img = obj.AddComponent<Image>();
        Undo.RecordObject(img, "Set Color");
        img.color = color;
    }

    // ===== 画面プレビュー =====

    static readonly string[] allScreens =
    {
        "NewsScreen", "BuyScreen", "SellScreen", "NightScreen", "EndingScreen", "ShopScreen"
    };

    [MenuItem("Tools/おこめゲーム/画面プレビュー/NewsScreenを表示")]
    static void PreviewNews()    => PreviewScreen("NewsScreen");

    [MenuItem("Tools/おこめゲーム/画面プレビュー/BuyScreenを表示")]
    static void PreviewBuy()     => PreviewScreen("BuyScreen");

    [MenuItem("Tools/おこめゲーム/画面プレビュー/SellScreenを表示")]
    static void PreviewSell()    => PreviewScreen("SellScreen");

    [MenuItem("Tools/おこめゲーム/画面プレビュー/NightScreenを表示")]
    static void PreviewNight()   => PreviewScreen("NightScreen");

    [MenuItem("Tools/おこめゲーム/画面プレビュー/EndingScreenを表示")]
    static void PreviewEnding()  => PreviewScreen("EndingScreen");

    [MenuItem("Tools/おこめゲーム/画面プレビュー/全画面を表示（元に戻す）")]
    static void PreviewAll()
    {
        foreach (string name in allScreens)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                Undo.RecordObject(obj, "Preview All");
                obj.SetActive(true);
            }
        }
        Debug.Log("✓ 全画面を表示しました");
    }

    static void PreviewScreen(string targetName)
    {
        foreach (string name in allScreens)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null) continue;
            Undo.RecordObject(obj, "Preview Screen");
            obj.SetActive(name == targetName);
        }
        Debug.Log("✓ " + targetName + " をプレビュー中");
    }

    // ===== ユーティリティ =====

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}

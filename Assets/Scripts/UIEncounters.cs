using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using System;

public class UIEncounters : MonoBehaviour
{
    //Prefabs for instantiation
    public GameObject PrefabLoadedEncounterPanel;
    public GameObject PrefabLoadedEncounterPullButton;

    private CGameManager gmInstance;
    //private List<TextAsset> logFiles = new List<TextAsset>();

    //INT = ENCOUNTERSTRUCT.ENCOUNTERID
    private Dictionary<int, RectTransform> EncounterPanels = new Dictionary<int, RectTransform>();
    //KVP = ENCOUNTERSTRUCT.ENCOUNTERID, ENCOUNTERSTRUCT.PULL
    //private Dictionary<KeyValuePair<int,int>, RectTransform> EncounterButtons = new Dictionary<KeyValuePair<int, int>, RectTransform>();


    // Start is called before the first frame update
    void Start()
    {
        gmInstance = GameObject.Find("GameMap").GetComponent<CGameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("Panels: " + EncounterPanels.Count);
        //Debug.Log("Buttons: " + EncounterButtons.Count);
    }

    //MAKE AFTER DEBUGGING
    //void CheckForLogs()
    //{

    //}

    //public void CheckForEncounters(TextAsset t)
    //{

    //}

    //void DEBUGCheckForLogs()
    //{
    //    gmInstance.ParseLogForEncounters();
    //}

    public void CreateEncounterPanel(CGameManager.EncounterStruct en)
    {
        Debug.Log("MAKING A PANEL...");
        RectTransform newEnPanel = Instantiate(PrefabLoadedEncounterPanel).GetComponent<RectTransform>();
        newEnPanel.gameObject.name = en.EncounterName;

        Vector3 originalSize = newEnPanel.localScale;
        Vector3 originalPos = newEnPanel.localPosition;
        newEnPanel.SetParent(this.transform, false);
        newEnPanel.localScale = originalSize;
        newEnPanel.localPosition = originalPos;

        newEnPanel.GetChild(0).name = en.EncounterName + "PanelText";
        //newEnPanel.GetComponentInChildren<Text>().text = en.EncounterName;
        EncounterPanels.Add(en.EncounterID, newEnPanel);
        //Debug.Log("MADE A PANEL: " + newEnPanel.gameObject.name);
    }

    public void CreateEncounterButton(CGameManager.EncounterStruct en)
    {
        Debug.Log("MAKING A BUTTON...");
        RectTransform newEnButton = Instantiate(PrefabLoadedEncounterPullButton).GetComponent<RectTransform>(); ;
        newEnButton.gameObject.name = en.EncounterName + " " + en.EncounterPull;

        Vector3 originalSize = newEnButton.localScale;
        Vector3 originalPos = newEnButton.localPosition;
        newEnButton.SetParent(EncounterPanels[en.EncounterID].transform, false);
        newEnButton.localScale = originalSize;
        newEnButton.localPosition = originalPos;

        //newEnButton.GetComponentInChildren<Text>().text = en.EncounterPull + " (" + TimeSpan.FromMilliseconds(en.EncounterTime) + ")";
        //KeyValuePair<int, int> k = new KeyValuePair<int, int>(en.EncounterID, en.EncounterPull);
        UpdateEncounterButtonTransform(newEnButton, en.EncounterPull);
        newEnButton.gameObject.GetComponent<Button>().onClick.AddListener(delegate { EncounterButtonClicked(en); });
        //EncounterButtons.Add(k, newEnButton);
        Debug.Log("MADE A BUTTON: " + newEnButton.gameObject.name);
    }

    void UpdateEncounterPanelTransform(RectTransform p)
    {

    }

    void UpdateEncounterButtonTransform(RectTransform b, int pullnum)
    {
        Vector3 originalPos = b.localPosition;
        float Yoff = Mathf.Floor((pullnum - 1) / 4) * -100;
        float Xoff = 200 * ((pullnum - 1) % 4);
        Vector3 newPos = new Vector3(originalPos.x + Xoff, originalPos.y + Yoff, 0);
        b.transform.localPosition = newPos;

    }

    void EncounterButtonClicked(CGameManager.EncounterStruct enst)
    {
        gmInstance.LoadGameMapToScale(enst);
    }
    
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using System;
using TMPro;

public class UITimeElapsed : MonoBehaviour
{
    CGameManager gmInstance;
    
    // Start is called before the first frame update
    void Start()
    {
        gmInstance = GameObject.Find("GameMap").GetComponent<CGameManager>();
        string timeString = TimeSpan.FromSeconds(gmInstance.elapsedGameTime).ToString();
        gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = timeString;
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Time.timeScale > 0)
        {
            string timeString = TimeSpan.FromSeconds(gmInstance.elapsedGameTime).ToString();
            gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = timeString;
        }
    }
}

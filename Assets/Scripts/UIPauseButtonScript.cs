using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using System;

public class UIPauseButtonScript : MonoBehaviour
{
    public Sprite playSprite;
    public Sprite pauseSprite;
    CGameManager gmInstance;

    // Start is called before the first frame update
    void Start()
    {
        gmInstance = GameObject.Find("GameMap").GetComponent<CGameManager>();
        TogglePauseGame();
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.GetComponent<Image>().sprite == pauseSprite && Time.timeScale == 0)
        {
            gameObject.GetComponent<Image>().sprite = playSprite;
        }
        if (gameObject.GetComponent<Image>().sprite == playSprite && Time.timeScale == 1)
        {
            gameObject.GetComponent<Image>().sprite = pauseSprite;
        }
    }

    public void TogglePauseGame()
    {
        if (gmInstance.isReplayLoaded)
        {
            if (Time.timeScale == 0)
            {
                Time.timeScale = 1;
            }
            else
            {
                Time.timeScale = 0;
            }
        }
        else
        {
            if (Time.timeScale != 0)
            {
                Time.timeScale = 0;
            }
        }
        
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIWindowToggle : MonoBehaviour
{

    public Vector3 WindowPositionHidden;
    public Vector3 WindowPositionShown;
    private bool isHidden;
    private GameObject goRef;
    private RectTransform rectRef;
    private float heightRef;
    private float widthRef;

    // Start is called before the first frame update
    void Start()
    {
        isHidden = true;
        goRef = this.gameObject;
        rectRef = GetComponent<RectTransform>();
        heightRef = rectRef.rect.height;
        widthRef = rectRef.rect.width;
        WindowPositionHidden = Vector3.zero;
        WindowPositionShown = new Vector3(0f, heightRef, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToggleWindowVisibility()
    {
        
        if (isHidden)
        {
            isHidden = false;
            rectRef.anchoredPosition3D = WindowPositionShown;
        }
        else
        {
            isHidden = true;
            rectRef.anchoredPosition3D = WindowPositionHidden;
        }
    }


}

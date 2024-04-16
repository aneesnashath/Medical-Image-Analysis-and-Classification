using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class manager : MonoBehaviour
{
    public static manager INSTANCE;


    [SerializeField] GameObject SelectObj;
    [SerializeField] GameObject UploadObj;
    [SerializeField] GameObject ClassifyObj;
    [SerializeField] GameObject NextObj;

    

    private void Awake()
    {
        INSTANCE = this;

        openselect();
    }

    public void openselect()
    {
        if (NextObj.activeInHierarchy)
        {
            ImageClassifier.Instance.removetext();
        }

        SelectObj.SetActive(true);
        UploadObj.SetActive(false);
        ClassifyObj.SetActive(false);
        NextObj.SetActive(false);


    }
    public void openuplaod()
    {
        SelectObj.SetActive(false);
        UploadObj.SetActive(true);
        ClassifyObj.SetActive(false);
        NextObj.SetActive(false);
    }
    public void openclassify()
    {
        SelectObj.SetActive(false);
        UploadObj.SetActive(false);
        ClassifyObj.SetActive(true);
        NextObj.SetActive(false);
    }
    public void opennext()
    {
       
        SelectObj.SetActive(false);
        UploadObj.SetActive(false);
        ClassifyObj.SetActive(false);
        NextObj.SetActive(true);
    }
}

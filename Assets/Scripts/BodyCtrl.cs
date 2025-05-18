using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyCtrl : MonoBehaviour
{
    public static BodyCtrl Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SwitchGender(int gender)
    {
        transform.Find("Player").GetChild(gender).gameObject.SetActive(true);
        transform.Find("Player").GetChild(1 - gender).gameObject.SetActive(false);
    }
}

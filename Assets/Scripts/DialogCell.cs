using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogCell : MonoBehaviour
{
    
    public void Initial(string playerName, string content)
    {
        transform.Find("Name").GetComponent<TMP_Text>().text = playerName;
        transform.Find("Content").GetComponent<TMP_Text>().text = content;
    }
}

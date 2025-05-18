using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerListCell : MonoBehaviour
{
    [SerializeField]
    TMP_Text _name;
    TMP_Text _ready;
    public PlayerInfo PlayerInfo { get; private set; }

    public void Initial(PlayerInfo playerInfo)
    {
        PlayerInfo = playerInfo;
        _name = transform.Find("Name").GetComponent<TMP_Text>();
        _ready = transform.Find("Ready").GetComponent<TMP_Text>();
        _name.text = "Player" + playerInfo.id.ToString();
        _ready.text = playerInfo.isReady ? "已准备" : "未准备";
    }

    internal void SetReady(bool isOn)
    {
        _ready.text = isOn ? "准备" : "未准备";
    }
}

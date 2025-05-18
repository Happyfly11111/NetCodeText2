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
    TMP_Text _gender;
    public PlayerInfo PlayerInfo { get; private set; }

    public void Initial(PlayerInfo playerInfo)
    {
        PlayerInfo = playerInfo;
        _name = transform.Find("Name").GetComponent<TMP_Text>();
        _name.text = playerInfo.name;
        _ready = transform.Find("Ready").GetComponent<TMP_Text>();
        _ready.text = playerInfo.isReady ? "已准备" : "未准备";
        _gender = transform.Find("Gender").GetComponent<TMP_Text>();
        _gender.text = playerInfo.gender == 0 ? "男" : "女";
    }

    public void UpdatePlayerInfo(PlayerInfo playerInfo)
    {
        PlayerInfo = playerInfo;
        _ready.text = playerInfo.isReady ? "已准备" : "未准备";
        _gender.text = playerInfo.gender == 0 ? "男" : "女";
        _name.text = playerInfo.name;
    }

    public void SetReady(bool isOn)
    {
        _ready.text = isOn ? "准备" : "未准备";
    }

    public void SetGender(int gender)
    {
        _gender.text = gender == 0 ? "男" : "女";
    }
}

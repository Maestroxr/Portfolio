using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Text moneyText;
    [SerializeField] private GameObject nowPlaying;

    void Awake()
    {
        player.MoneyChangedEvent += MoneyChanged;
    }


    private void MoneyChanged(int money)
    {
        moneyText.text = $"{string.Format("{0:n0}", money)}$";
    }

    public void NowPlaying(Player playingPlayer)
    {
        nowPlaying.gameObject.SetActive(playingPlayer.PlayerId == player.PlayerId);
    }
}

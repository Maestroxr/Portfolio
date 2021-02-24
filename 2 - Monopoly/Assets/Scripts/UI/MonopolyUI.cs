using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MonopolyUI : MonoBehaviour
{
    public Sprite AssetFree => assetFree;
    public Dictionary<PlayerId, Sprite> AssetOwnedSprites { get; private set; } = new Dictionary<PlayerId, Sprite>();
    public Dictionary<PlayerId, Image> PlayerInTileImages { get; private set; } = new Dictionary<PlayerId, Image>();
    public MonopolySettings Settings { get; private set; }

    [SerializeField] private Monopoly monopoly;
    [SerializeField] private List<PlayerUI> playerUIs;
    [SerializeField] private Dice dice;
    [SerializeField] private List<Sprite> diceValues;
    [SerializeField] private Image DiceImage;
    [SerializeField] private List<Image> playerInTileImagesList;
    [SerializeField] private List<Sprite> assetOwnedSpritesList;
    [SerializeField] private Sprite assetFree;
    [SerializeField] private Text playerHeaderDialog;
    [SerializeField] private Text playerBodyDialog;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip diceSound;
    [SerializeField] private AudioClip playerMoveSound;
    
    private Coroutine diceRolling;
    private float diceRollChangeImageTime;
    private float playerMoveTime;


    void Awake()
    {
        Settings = monopoly.Settings;
        monopoly.PlayerMovedEvent += PlayerMoved;
        for (int i = 0; i < monopoly.Settings.MaxPlayers; i++)
        {
            PlayerId playerId = (PlayerId)i + 1;
            AssetOwnedSprites.Add(playerId, assetOwnedSpritesList[i]);
            PlayerInTileImages.Add(playerId, playerInTileImagesList[i]);
            monopoly.NowPlayingEvent += playerUIs[i].NowPlaying;
        }

        dice.DieRollingEvent += DieRolling;
        dice.DieCastEvent += DieCast;

        diceRollChangeImageTime = Settings.DiceRollChangeValueTime;
        playerMoveTime = Settings.PlayerMoveTime;
    }


    public void DisplayPlayerDialog(string header, string body)
    {
        playerHeaderDialog.text = header;
        playerBodyDialog.text = body;
    }


    private void PlayerMoved(Player player)
    {
        audioSource.clip = playerMoveSound;
        audioSource.time = Math.Max(1, playerMoveSound.length - playerMoveTime * player.DistanceFromLastLocation() - 1);
        audioSource.Play();
    }


    private void DieRolling(float rollForSeconds)
    {
        diceRolling = StartCoroutine(AnimateDice(rollForSeconds));
        audioSource.clip = diceSound;
        audioSource.time = 0;
        audioSource.Play();
    }


    private IEnumerator AnimateDice(float rollForSeconds)
    {
        while (rollForSeconds > 0)
        {
            int dieValue = UnityEngine.Random.Range(1, 6);
            DiceImage.sprite = diceValues[dieValue];
            yield return new WaitForSeconds(diceRollChangeImageTime);
            rollForSeconds -= diceRollChangeImageTime;
        }
    }


    private void DieCast(int result)
    {
        if (diceRolling != null)
        {
            StopCoroutine(diceRolling);
        }
        DiceImage.sprite = diceValues[result - 1];
    }
}

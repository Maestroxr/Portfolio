using System;
using System.Collections;
using System.Collections.Generic;
using Gamebox.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// Board UI of the Monopoly module: dice animation, player tokens, dialogs and sounds. The shared menu
    /// (start, save, load, settings, exit) is wired through the <see cref="GameUI"/> base.
    /// </summary>
    public class MonopolyUI : GameUI
    {
        public Sprite AssetFree => assetFree;
        public Dictionary<PlayerId, Sprite> AssetOwnedSprites { get; private set; } = new Dictionary<PlayerId, Sprite>();
        public Dictionary<PlayerId, Image> PlayerInTileImages { get; private set; } = new Dictionary<PlayerId, Image>();
        public MonopolySettings Settings => Monopoly != null ? Monopoly.MonopolySettings : null;

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

        public MonopolyGameManager Monopoly => Manager as MonopolyGameManager;


        protected override void Awake()
        {
            base.Awake();
            if (Monopoly == null)
            {
                Debug.LogError("MonopolyUI has no MonopolyGameManager assigned.", this);
                return;
            }
            Monopoly.PlayerMovedEvent += PlayerMoved;
            Monopoly.PlayerResetEvent += PlayerReset;
            MonopolySettings settings = Monopoly.MonopolySettings;
            for (int i = 0; i < settings.MaxPlayers; i++)
            {
                PlayerId playerId = (PlayerId)i + 1;
                if (i < assetOwnedSpritesList.Count)
                {
                    AssetOwnedSprites[playerId] = assetOwnedSpritesList[i];
                }
                if (i < playerInTileImagesList.Count)
                {
                    PlayerInTileImages[playerId] = playerInTileImagesList[i];
                }
                if (i < playerUIs.Count)
                {
                    Monopoly.NowPlayingEvent += playerUIs[i].NowPlaying;
                }
            }

            if (dice != null)
            {
                dice.DieRollingEvent += DieRolling;
                dice.DieCastEvent += DieCast;
            }
        }


        public void DisplayPlayerDialog(string header, string body)
        {
            if (playerHeaderDialog != null)
            {
                playerHeaderDialog.text = header;
            }
            if (playerBodyDialog != null)
            {
                playerBodyDialog.text = body;
            }
        }


        /// <summary>Puts the token of <paramref name="player"/> on <paramref name="tile"/> without animating it.</summary>
        public void PlaceToken(MonopolyPlayer player, Tile tile)
        {
            if (tile == null || !PlayerInTileImages.TryGetValue(player.PlayerId, out Image token))
            {
                return;
            }
            token.transform.SetParent(tile.transform, false);
            token.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            token.gameObject.SetActive(true);
        }


        private void PlayerReset(MonopolyPlayer player)
        {
            PlaceToken(player, Monopoly.GetTile(player.Location));
        }


        private void PlayerMoved(MonopolyPlayer player)
        {
            if (audioSource == null || playerMoveSound == null)
            {
                return;
            }
            float playerMoveTime = Settings != null ? Settings.PlayerMoveTime : 0f;
            audioSource.clip = playerMoveSound;
            audioSource.time = Math.Max(1, playerMoveSound.length - playerMoveTime * player.DistanceFromLastLocation() - 1);
            audioSource.Play();
        }


        private void DieRolling(float rollForSeconds)
        {
            diceRolling = StartCoroutine(AnimateDice(rollForSeconds));
            if (audioSource != null && diceSound != null)
            {
                audioSource.clip = diceSound;
                audioSource.time = 0;
                audioSource.Play();
            }
        }


        private IEnumerator AnimateDice(float rollForSeconds)
        {
            float diceRollChangeImageTime = Settings != null ? Mathf.Max(0.01f, Settings.DiceRollChangeValueTime) : 0.1f;
            while (rollForSeconds > 0)
            {
                int dieValue = UnityEngine.Random.Range(0, diceValues.Count);
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
            if (result >= 1 && result <= diceValues.Count)
            {
                DiceImage.sprite = diceValues[result - 1];
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    public class PlayerUI : MonoBehaviour
    {
        [SerializeField] private MonopolyPlayer player;
        [SerializeField] private Text moneyText;
        [SerializeField] private GameObject nowPlaying;

        void Awake()
        {
            if (player == null)
            {
                return;
            }
            player.MoneyChangedEvent += MoneyChanged;
            MoneyChanged(player.Money);
        }


        private void MoneyChanged(int money)
        {
            if (moneyText != null)
            {
                moneyText.text = $"{string.Format("{0:n0}", money)}$";
            }
        }

        public void NowPlaying(MonopolyPlayer playingPlayer)
        {
            if (nowPlaying != null && player != null)
            {
                nowPlaying.gameObject.SetActive(playingPlayer != null && playingPlayer.PlayerId == player.PlayerId);
            }
        }
    }
}

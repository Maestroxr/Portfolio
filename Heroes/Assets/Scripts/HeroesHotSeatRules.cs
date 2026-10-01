using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// What Heroes offers for several people at one device: a hot seat on a skirmish map, where two to four people take
    /// their turns at the device and the computer may play any other realm, the screen covered between the turns of two
    /// people. The map fixes how many realms play and their factions; its size and where battles are fought are chosen
    /// with it, its riches and wandering armies are the settings' (as for a skirmish). The seats take the colours the
    /// realms fly on the map, and each says which faction it plays.
    /// </summary>
    public sealed class HeroesHotSeatRules : LocalPlayRules
    {
        public const string MapOption = "map";
        public const string SizeOption = "size";
        public const string BattlesOption = "battles";

        private static readonly string[] Sizes = { "Small", "Medium", "Large" };
        private static readonly string[] BattleStyles = { "On the map", "On a battlefield" };

        private readonly HeroesCampaign campaign;
        private readonly List<int> maps;

        public HeroesHotSeatRules(HeroesCampaign campaign)
        {
            this.campaign = campaign;
            maps = campaign != null ? campaign.Skirmishes : new List<int>();
            Game = GameType.Heroes;
            Title = "Hot Seat";
            Hint = "Take turns at this device; the computer can play any realm.";
            AllowComputers = true;
            MinHumans = 2;
            OffersHandOver = true;
            HandOverByDefault = true;
            var titles = new string[maps.Count];
            for (int i = 0; i < maps.Count; i++)
            {
                titles[i] = campaign.Scenario(maps[i]).Title;
            }
            Options.Add(new LocalOption(MapOption, "Map", titles));
            Options.Add(new LocalOption(SizeOption, "Map size", Sizes, 1));
            Options.Add(new LocalOption(BattlesOption, "Battles", BattleStyles, (int)BattleStyle.Battlefield));
            Refresh();
        }

        /// <summary>The campaign indices of the maps the setup offers, in its order.</summary>
        public IReadOnlyList<int> Maps => maps;

        /// <summary>
        /// Before the setup opens: the seats take the colours of the realms in the look shown, and the number of players
        /// is the first map's, as a first setup has it (a remembered setup fits it to its own map).
        /// </summary>
        public void Refresh()
        {
            SeatColors = new Color[4];
            for (int i = 0; i < SeatColors.Length; i++)
            {
                SeatColors[i] = HeroesArt.PlayerColor(i);
            }
            Fit(MapAt(FindOption(MapOption).Index));
        }

        /// <summary>The campaign index of the map <paramref name="match"/> is played on.</summary>
        public int LevelOf(LocalMatch match)
        {
            int option = match.Option(MapOption);
            return maps.Count == 0 ? -1 : maps[Mathf.Clamp(option, 0, maps.Count - 1)];
        }

        /// <summary>The option that picks the map at campaign index <paramref name="level"/>, or -1 for none of them.</summary>
        public int OptionOf(int level)
        {
            return maps.IndexOf(level);
        }

        private HeroesLevel MapAt(int option)
        {
            return campaign != null && maps.Count > 0 ? campaign.Scenario(maps[Mathf.Clamp(option, 0, maps.Count - 1)]) : null;
        }

        /// <summary>The map seats as many realms as it has, no more and no fewer.</summary>
        private void Fit(HeroesLevel level)
        {
            int seats = level != null ? Mathf.Clamp(level.Map.players.Count, 2, 4) : 2;
            MinPlayers = seats;
            MaxPlayers = seats;
            DefaultPlayers = seats;
        }

        public override void OptionsChanged(LocalMatch match)
        {
            Fit(MapAt(match.Option(MapOption)));
            match.Count = MaxPlayers;
        }

        /// <summary>The faction the seat plays on the map chosen, in the language shown.</summary>
        public override string SeatDetail(LocalMatch match, int seat)
        {
            HeroesLevel level = MapAt(match.Option(MapOption));
            if (level == null || seat < 0 || seat >= level.Map.players.Count)
            {
                return null;
            }
            return Words.T(Land.FactionName(level.Map.players[seat].faction));
        }

        /// <summary>The seats of <paramref name="match"/> as the rules take them: a name, a person or the computer at a level.</summary>
        public static List<HotSeatPlayer> Players(LocalMatch match)
        {
            var players = new List<HotSeatPlayer>();
            foreach (LocalSeat seat in match.Players)
            {
                players.Add(new HotSeatPlayer { name = seat.Name, human = seat.IsHuman, aiLevel = seat.Level });
            }
            return players;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Portfolio.Monopoly
{
    public enum SpaceKind { Go, Street, Railroad, Utility, Chance, CommunityChest, Tax, Jail, FreeParking, GoToJail }

    /// <summary>The colour sets of the streets, plus the stations and the utilities, which are sets of their own.</summary>
    public enum ColorGroup { None, Brown, LightBlue, Pink, Orange, Red, Yellow, Green, DarkBlue, Railroad, Utility }

    public enum CardDeckKind { Chance, CommunityChest }

    public enum CardAction
    {
        /// <summary>Move forward to space <see cref="CardData.value"/>, collecting the salary when passing GO.</summary>
        AdvanceTo,
        /// <summary>Move forward to the nearest space of <see cref="CardData.group"/>; <see cref="CardData.value"/> multiplies the rent (0: normal).</summary>
        AdvanceToNearest,
        /// <summary>Move by <see cref="CardData.value"/> spaces (negative: backwards, no salary).</summary>
        MoveBy,
        Collect,
        Pay,
        PayEachPlayer,
        CollectFromEachPlayer,
        /// <summary>Pay <see cref="CardData.value"/> for every house and <see cref="CardData.value2"/> for every hotel.</summary>
        Repairs,
        GoToJail,
        GetOutOfJail,
        /// <summary>The player with the highest net worth (other than the drawer) pays <see cref="CardData.value"/>.</summary>
        CollectFromRichest,
        /// <summary>Every player pays <see cref="CardData.value"/> into the Free Parking pot.</summary>
        EveryonePaysPot,
        /// <summary>A free house on the cheapest property of a complete set that can take one.</summary>
        FreeHouse
    }

    /// <summary>One space of the board: its kind, name, colour set and prices.</summary>
    [Serializable]
    public class SpaceData
    {
        public string name;
        /// <summary>A second line of the name, or where the place is (the city of a street, the kind of a station).</summary>
        public string city;
        public SpaceKind kind;
        public ColorGroup group;
        public int price;
        /// <summary>Rent unimproved, with 1 to 4 houses and with a hotel (streets); unused for stations and utilities.</summary>
        public int[] rent = new int[6];
        public int houseCost;
        /// <summary>What a tax space charges.</summary>
        public int tax;

        public bool IsProperty => kind == SpaceKind.Street || kind == SpaceKind.Railroad || kind == SpaceKind.Utility;

        public int MortgageValue => price / 2;

        /// <summary>What lifting the mortgage costs: the mortgage plus ten percent interest, rounded up.</summary>
        public int UnmortgageCost => MortgageValue + (MortgageValue + 9) / 10;

        public string DisplayName => name;

        public SpaceData Clone()
        {
            var copy = (SpaceData)MemberwiseClone();
            copy.rent = rent != null ? (int[])rent.Clone() : new int[6];
            return copy;
        }
    }

    /// <summary>A Chance or Community Chest card.</summary>
    [Serializable]
    public class CardData
    {
        public CardDeckKind deck;
        public string text;
        public CardAction action;
        public int value;
        public int value2;
        public ColorGroup group;
        /// <summary>Only in the deck when the party cards house rule is on.</summary>
        public bool party;

        public CardData Clone()
        {
            return (CardData)MemberwiseClone();
        }
    }

    /// <summary>
    /// The board a match is played on: its 40 spaces, the two decks and the bank's supply. Plain data, so the rules
    /// engine and the tests use it without Unity objects; the <see cref="Board"/> asset holds one for the game.
    /// </summary>
    [Serializable]
    public class BoardLayout
    {
        public string title = "Monopoly";
        public string edition = "World Tour";
        public List<SpaceData> spaces = new List<SpaceData>();
        public List<CardData> chance = new List<CardData>();
        public List<CardData> communityChest = new List<CardData>();
        public int houses = 32;
        public int hotels = 12;

        [NonSerialized] private Dictionary<ColorGroup, int[]> groups;

        public int Count => spaces.Count;

        public SpaceData this[int index] => spaces[index];

        public int JailIndex => IndexOf(SpaceKind.Jail);

        public int GoToJailIndex => IndexOf(SpaceKind.GoToJail);

        public int FreeParkingIndex => IndexOf(SpaceKind.FreeParking);

        public int IndexOf(SpaceKind kind)
        {
            for (int i = 0; i < spaces.Count; i++)
            {
                if (spaces[i].kind == kind)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>The spaces of a set, in board order.</summary>
        public int[] Group(ColorGroup group)
        {
            if (groups == null)
            {
                groups = new Dictionary<ColorGroup, int[]>();
                foreach (ColorGroup g in Enum.GetValues(typeof(ColorGroup)))
                {
                    groups[g] = Enumerable.Range(0, spaces.Count).Where(i => spaces[i].IsProperty && spaces[i].group == g).ToArray();
                }
            }
            return groups.TryGetValue(group, out int[] members) ? members : Array.Empty<int>();
        }

        public IEnumerable<int> Properties => Enumerable.Range(0, spaces.Count).Where(i => spaces[i].IsProperty);

        public List<CardData> Deck(CardDeckKind kind)
        {
            return kind == CardDeckKind.Chance ? chance : communityChest;
        }

        /// <summary>Forgets the cached sets after the spaces were edited.</summary>
        public void Invalidate()
        {
            groups = null;
        }

        public BoardLayout Clone()
        {
            return new BoardLayout
            {
                title = title,
                edition = edition,
                spaces = spaces.Select(space => space.Clone()).ToList(),
                chance = chance.Select(card => card.Clone()).ToList(),
                communityChest = communityChest.Select(card => card.Clone()).ToList(),
                houses = houses,
                hotels = hotels
            };
        }

        /// <summary>Whether the layout can be played: a GO, a jail, the sets and decks in order.</summary>
        public bool Check(out string error)
        {
            if (spaces.Count < 12)
            {
                error = $"The board has {spaces.Count} spaces, at least 12 are needed";
                return false;
            }
            if (spaces[0].kind != SpaceKind.Go)
            {
                error = "The first space has to be GO";
                return false;
            }
            if (JailIndex < 0)
            {
                error = "The board has no jail";
                return false;
            }
            for (int i = 0; i < spaces.Count; i++)
            {
                SpaceData space = spaces[i];
                if (string.IsNullOrEmpty(space.name))
                {
                    error = $"Space {i} has no name";
                    return false;
                }
                if (space.IsProperty && space.price <= 0)
                {
                    error = $"{space.name} has no price";
                    return false;
                }
                if (space.kind == SpaceKind.Street && (space.rent == null || space.rent.Length < 6 || space.houseCost <= 0))
                {
                    error = $"{space.name} needs six rents and a house cost";
                    return false;
                }
            }
            if (chance.Count == 0 || communityChest.Count == 0)
            {
                error = "Both decks need cards";
                return false;
            }
            if (houses < 0 || hotels < 0)
            {
                error = "The bank cannot hold a negative number of buildings";
                return false;
            }
            error = null;
            return true;
        }
    }
}

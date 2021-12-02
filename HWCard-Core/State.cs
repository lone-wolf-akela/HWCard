namespace HWCard_Core
{
    public enum CardType
    {
        Fighter, Corvette, Frigate
    }
    public class GameStage
    {
        public enum Stages { DrawCard, DropCard, SelectATK, AskForAltATK, AskForEndGame, DEF };
        public Stages Stage { get; set; }
        public enum Players { Beginning, Player0, Player1 };
        public Players Player { get; set; }

        public GameStage()
        {
            Stage = Stages.DrawCard;
            Player = Players.Beginning;
        }
    }
    public class PlayerState
    {
        public List<Card> Deck { get; set; }
        public List<Card> Hand { get; set; }
        public bool HandVisible { get; set; }
        public List<Card> Graveyard { get; set; }
        public List<Card> OnBoard { get; set; }
        public Card? Active { get; set; }
        public PlayerState(int deck_start_size, Random rand)
        {
            var all_cards = CardsInfo.AllPossibleCards;
            Deck = new();
            for (int i = 0; i < deck_start_size; i++)
            {
                Deck.Add(all_cards[rand.Next(all_cards.Count)]);
            }
            Hand = new ();
            HandVisible = false;
            Graveyard = new();
            OnBoard = new();
            Active = null;
        }
    }
    public interface IAreaEffect
    {
        public int TurnsRemaining { get; set; }
        public string Name { get; }
        public string Description { get; }
    }
    public class MinelayerCannotDefenseThisTurn : IAreaEffect
    {
        public int TurnsRemaining { get { return 0; } set { /* do nothing*/} }
        public string Name => "扫雷";
        public string Description => "回合结束前，【布雷艇】不能进行防御。";
    }
    public class GameState
    {
        public GameStage Stage { get; set; }
        public List<IAreaEffect> AreaEffects { get; set; }
        public List<PlayerState> Players { get; set; }

        public GameState(int deck_start_size, Random rand)
        {
            Stage = new();
            AreaEffects = new();
            // gen decks
            Players = new()
            {
                new(deck_start_size, rand),
                new(deck_start_size, rand)
            };
        }
    }
    public interface AlternativeATK
    {
        public string Name { get; }
        public string Description { get; }
        public void Effect(GameState state, int player_idx);
    }
    public interface Card
    {
        public CardType Type { get; }
        public string Name { get; }
        public string Description { get; }
        public List<CardType> ATK_Destroy { get; }
        public List<CardType> DEF_NonDestroy { get; }
        public List<CardType> DEF_Destroy { get; }

        // below is optional property
        public bool IsInvulnerableInATK => false;
        public bool IsCloak => false;
        public bool IsAntiCloak => false;
        public List<AlternativeATK> AlternativeATKs => new();
    }
}
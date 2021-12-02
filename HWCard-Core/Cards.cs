namespace HWCard_Core
{
    public static class CardsInfo
    {
        public static List<Card> AllPossibleCards
        {
            get { return new() { new Scout(), new Interceptor(), new Azrael(), new Vulcan(), new Minelayer() }; }
        }
    }

    public class Scout : Card
    {
        public CardType Type => CardType.Fighter;
        public string Name => "侦察机";
        public string Description => @"进攻效果：无
防御效果：无
被动效果：该牌在进攻时无法被防御";
        public List<CardType> ATK_Destroy => new();
        public List<CardType> DEF_NonDestroy => new();
        public List<CardType> DEF_Destroy => new();
        public bool IsInvulnerableInATK => true;
    }
    public class Minesweeping : AlternativeATK
    {
        public string Name => "扫雷";
        public string Description => "使对方【布雷艇】本回合不能发挥防御效果";

        public void Effect(GameState state)
        {
            state.AreaEffects.Add(new MinelayerCannotDefenseThisTurn());
        }
    }
    public class Interceptor : Card
    {
        public CardType Type => CardType.Fighter;
        public string Name => "战斗机";
        public string Description => @"进攻效果：破坏对方随机一张【△】，或使对方【布雷艇】本回合不能发挥防御效果
防御效果：【△】类型的进攻无效
被动效果：无";
        public List<CardType> ATK_Destroy => new() { CardType.Fighter };
        public List<CardType> DEF_NonDestroy => new() { CardType.Fighter };
        public List<CardType> DEF_Destroy => new();
        public List<AlternativeATK> AlternativeATKs => new() { new Minesweeping() };
    }
    public class Azrael : Card
    {
        public CardType Type => CardType.Corvette;
        public string Name => "死神攻击艇";
        public string Description => @"进攻效果：破坏对方随机一张【♢】
防御效果：无
被动效果：对方没有反隐形单位时，进攻效果无法防御";
        public List<CardType> ATK_Destroy => new() { CardType.Frigate };
        public List<CardType> DEF_NonDestroy => new();
        public List<CardType> DEF_Destroy => new();
        public bool IsCloak => true;
    }
    public class Vulcan : Card
    {
        public CardType Type => CardType.Frigate;
        public string Name => "火神护卫舰";
        public string Description => @"进攻效果：破坏对方随机一张【△】
防御效果：【□】类型的进攻无效；进攻的【△】类型被破坏
被动效果：无";
        public List<CardType> ATK_Destroy => new() { CardType.Fighter };
        public List<CardType> DEF_NonDestroy => new() { CardType.Corvette};
        public List<CardType> DEF_Destroy => new() { CardType.Fighter };
    }
    public class Minelayer : Card
    {
        public CardType Type => CardType.Corvette;
        public string Name => "布雷艇";
        public string Description => @"进攻效果：无
防御效果：进攻的【♢】类型被破坏
被动效果：反隐形";
        public List<CardType> ATK_Destroy => new();
        public List<CardType> DEF_NonDestroy => new();
        public List<CardType> DEF_Destroy => new() { CardType.Frigate }; 
        public bool IsAntiCloak => true;
    }
}
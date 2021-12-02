namespace HWCard_Core
{
    public static class Moves
    {
        public static string DrawCard(GameState state, int player_idx, int card_num)
        {
            var player = state.Players[player_idx];
            if (!player.Deck.Any())
            {
                return $"玩家{player_idx}尝试抽卡，但他的卡堆已耗尽，无法继续抽取";
            }
            for (int i = 0; i < card_num; i++)
            {
                if (!player.Deck.Any())
                {
                    return $"玩家{player_idx}抽取了{i}张卡片，他的卡堆已耗尽，无法继续抽取";
                }
                Card card = player.Deck.First()!;
                player.Hand.Add(card);
                player.Deck.RemoveAt(0);
            }
            return $"玩家{player_idx}抽取了{card_num}张卡片";
        }
        public static string ActivateCard(GameState state, int player_idx, int card_idx)
        {
            var player = state.Players[player_idx];
            var card = player.Hand[card_idx];
            player.Hand.RemoveAt(card_idx);
            player.Active = card;

            return $"玩家{player_idx}使用了[{card.Name}]";
        }
        public static string DisableActiveCard(GameState state, int player_idx)
        {
            var player = state.Players[player_idx];
            var card = player.Active;
            if(card is null)
            {
                return "";
            }
            player.Active = null;
            player.OnBoard.Add(card);
            return $"玩家{player_idx}的[{card.Name}]的效果被无效化了";
        }
        public static string DropCard(GameState state, int player_idx, int card_idx)
        {
            var player = state.Players[player_idx];
            var card = player.Hand[card_idx];
            player.Hand.RemoveAt(card_idx);
            player.Graveyard.Add(card);

            return $"玩家{player_idx}将一张[{card.Name}]自爆了";
        }
        public static string AttackAndDefense(GameState state, Random rand, int attacker_idx, int defenser_idx, AlternativeATK? alt_atk)
        {
            var attacker = state.Players[attacker_idx];
            var defenser = state.Players[defenser_idx];
            Card attack_card = attacker.Active!;
            Card? defense_card = defenser.Active;

            defenser.HandVisible = true;

            attacker.Active = null;
            defenser.Active = null;

            List<CardType> DestoyedList = defense_card?.DEF_Destroy ?? new();
            if (DestoyedList.Contains(attack_card.Type))
            {
                attacker.Graveyard.Add(attack_card);
                defenser.OnBoard.Add(defense_card!);
                return $"玩家{attacker_idx}进攻的[{attack_card.Name}]被玩家{defenser_idx}的[{defense_card!.Name}]击毁了";
            }

            List<CardType> BlockedList = defense_card?.DEF_NonDestroy ?? new();
            if (BlockedList.Contains(attack_card.Type))
            {
                attacker.OnBoard.Add(attack_card);
                defenser.OnBoard.Add(defense_card!);
                return $"玩家{attacker_idx}的[{attack_card.Name}]的进攻被玩家{defenser_idx}的[{defense_card!.Name}]阻止了";
            }

            attacker.OnBoard.Add(attack_card);
            if(alt_atk is not null)
            {
                alt_atk.Effect(state, attacker_idx);
                return $"[{alt_atk.Name}]效果生效";
            }

            var AttackList = attack_card.ATK_Destroy ?? new();
            if (defense_card is not null)
            {
                if (AttackList.Contains(defense_card.Type))
                {
                    defenser.Graveyard.Add(defense_card);
                    return $"玩家{defenser_idx}用于防御的[{defense_card.Name}]被玩家{attacker_idx}的[{attack_card.Name}]击毁了";
                }
                defenser.OnBoard.Add(defense_card);
            }
            List<int> PossibleAttackTargetsIdx = new();
            for (int i = 0; i < defenser.Hand.Count; i++)
            {
                if (AttackList.Contains(defenser.Hand[i].Type))
                {
                    PossibleAttackTargetsIdx.Add(i);
                }
            }
            for (int i = 0; i < defenser.OnBoard.Count; i++)
            {
                if (AttackList.Contains(defenser.OnBoard[i].Type))
                {
                    PossibleAttackTargetsIdx.Add(-i - 1);
                }
            }
            if (PossibleAttackTargetsIdx.Count > 0)
            {
                int destroyed_idx = PossibleAttackTargetsIdx[rand.Next(PossibleAttackTargetsIdx.Count)];
                var destoyed_card = destroyed_idx >= 0 ? defenser.Hand[destroyed_idx] :
                    defenser.OnBoard[-destroyed_idx - 1];
                defenser.Graveyard.Add(destoyed_card);
                if (destroyed_idx >= 0)
                {
                    defenser.Hand.RemoveAt(destroyed_idx);
                }
                else
                {
                    defenser.OnBoard.RemoveAt(-destroyed_idx - 1);
                }
                return $"玩家{defenser_idx}舰队中的一只[{destoyed_card.Name}]被玩家{attacker_idx}的[{attack_card.Name}]击毁了";
            }
            return $"玩家{attacker_idx}的[{attack_card.Name}]向玩家{defenser_idx}发起了进攻，但没有造成什么伤害";
        }
        public static string EndTurn(GameState state, int attaker_idx, int defenser_idx)
        {
            List<string> msgs = new();

            {
                var player = state.Players[defenser_idx];
                player.Hand.AddRange(player.OnBoard);
                player.OnBoard.Clear();
            }
            foreach (var player in state.Players)
            {
                player.HandVisible = false;
            }

            for (int i = state.AreaEffects.Count - 1; i >= 0; i--)
            {
                var effect = state.AreaEffects[i];
                if (effect.TurnsRemaining <= 0)
                {
                    state.AreaEffects.RemoveAt(i);
                    msgs.Add($"场上的[{effect.Name}]效果已消失");
                }
            }

            msgs.Add("回合结束");
            return string.Join("\n", msgs);
        }
        public static bool NeedCheckWin(GameState state)
        {
            foreach (var player in state.Players)
            {
                if(player.Hand.Count + player.OnBoard.Count <=0)
                {
                    return true;
                }
            }
            return false;
        }
        public static string CheckWin(GameState state)
        {
            var player0 = state.Players[0];
            var player1 = state.Players[1];
            if(player0.Hand.Count > player1.Hand.Count)
            {
                return "玩家0胜利";
            }
            if (player0.Hand.Count < player1.Hand.Count)
            {
                return "玩家1胜利";
            }
            return "平局";
        }
    }
}
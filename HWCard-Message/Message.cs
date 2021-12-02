using HWCard_Core;

namespace HWCard_Message
{
    public interface IMessage
    {
    }
    namespace Message
    {
        public class Attack : IMessage
        {
            public int AttackCardIdx { get; set; }
        }
        public class AltAttack : IMessage
        {
            public int AltAttackIdx { get; set; }
        }
        public class Defense : IMessage
        {
            public int DefenseCardIdx { get; set; }
        }
        public class DropCard : IMessage
        {
            public int DropedCardIdx { get; set; }
        }
        public class EndTurn : IMessage
        {

        }
        public class AskForEndGame : IMessage
        {

        }
        public class AnswerForEndGame : IMessage
        {
            public bool Endgame { get; set; }
        }
        public class RequestMsg : IMessage
        {

        }
        public class LogUpdate : IMessage
        {
            public string Log { get; set; }
        }
        public class AssignPlayerID : IMessage
        {
            public int PlayerID { get; set; }
        }
        public class StateUpdate: IMessage
        {
            public GameState State { get; set; }
        }
    }
}
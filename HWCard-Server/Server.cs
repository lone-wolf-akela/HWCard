using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using Newtonsoft.Json;
using Lidgren.Network;

using HWCard_Core;
using HWCard_Message;
using Message = HWCard_Message.Message;
using System.Collections.Concurrent;

namespace HWCard_Server
{
    internal class InvalidOperationError : Exception
    {
        public InvalidOperationError(string msg) : base(msg) { }
    }
    internal class Server
    {
        readonly JsonSerializerSettings jsonSerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.All
        };
        readonly Random random = new();
        readonly GameState state;
        readonly int handsize;
        internal Server(int deck_size, int hand_size)
        {
            state = new(deck_size, random);
            handsize = hand_size;
        }
        void PublishState()
        {
            var message = server!.CreateMessage();
            Message.StateUpdate state_update = new() { State = state };
            message.Write(JsonConvert.SerializeObject(state_update, jsonSerializerSettings));
            server.SendToAll(message, NetDeliveryMethod.ReliableOrdered);
        }
        void PublishLog(string msg)
        {
            Console.WriteLine(msg);
            var message = server!.CreateMessage();
            Message.LogUpdate log_update = new() { Log = msg };
            message.Write(JsonConvert.SerializeObject(log_update, jsonSerializerSettings));
            server.SendToAll(message, NetDeliveryMethod.ReliableOrdered);
        }
        void PushMsg(int player_idx, IMessage msg)
        {
            var message = server!.CreateMessage();
            message.Write(JsonConvert.SerializeObject(msg, jsonSerializerSettings));
            server.SendMessage(message, map_idx_connection[player_idx], NetDeliveryMethod.ReliableOrdered);
        }
        IMessage PullFromPlayer(int player_idx)
        {
            {
                // Ask for answer
                Message.RequestMsg req = new();
                PushMsg(player_idx, req);
            }

            {
                // get the answer
                IMessage msg;
                while (!msg_pool[player_idx].TryDequeue(out msg))
                { }
                return msg;
            }
        }
        T ExpectMsg<T>(IMessage msg)
        {
            if (msg is T t)
            {
                return t;
            }
            else
            {
                throw new InvalidOperationError(JsonConvert.SerializeObject(msg, jsonSerializerSettings));
            }
        }
        internal void Run()
        {
            PublishState();
            PublishLog(Moves.DrawCard(state, 0, handsize - 1));
            PublishLog(Moves.DrawCard(state, 1, handsize - 1));
            PublishState();

            while (true)
            {
                state.Stage.Player = state.Stage.Player == GameStage.Players.Player0 ?
                    GameStage.Players.Player1 : GameStage.Players.Player0;
                int attacking_player_idx = state.Stage.Player == GameStage.Players.Player0 ? 0 : 1;
                int defensing_player_idx = state.Stage.Player == GameStage.Players.Player0 ? 1 : 0;
                /*************************/
                state.Stage.Stage = GameStage.Stages.DrawCard;
                PublishLog(Moves.DrawCard(state, attacking_player_idx, 1));
                PublishState();
                while (state.Players[attacking_player_idx].Hand.Count > handsize)
                {
                    state.Stage.Stage = GameStage.Stages.DropCard;
                    PublishState();
                    var msg_drop = ExpectMsg<Message.DropCard>(PullFromPlayer(attacking_player_idx));
                    if (msg_drop is null)
                    {
                        throw new InvalidOperationError(JsonConvert.SerializeObject(msg_drop, jsonSerializerSettings));
                    }
                    PublishLog(Moves.DropCard(state, attacking_player_idx, msg_drop.DropedCardIdx));
                }
                /*************************/
                while (true)
                {
                    state.Stage.Stage = GameStage.Stages.SelectATK;
                    PublishState();
                    var msg = PullFromPlayer(attacking_player_idx);
                    if (msg is Message.Attack msg_atk)
                    {
                        PublishLog(Moves.ActivateCard(state, attacking_player_idx, msg_atk.AttackCardIdx));
                        var attacker = state.Players[attacking_player_idx];
                        var defenser = state.Players[defensing_player_idx];

                        // dealing with AlternativeATK
                        var alt_atk_list = attacker.Active!.AlternativeATKs;
                        if (alt_atk_list.Any())
                        {
                            state.Stage.Stage = GameStage.Stages.AskForAltATK;
                            PublishState();
                            var msg_altatk = ExpectMsg<Message.AltAttack>(PullFromPlayer(attacking_player_idx));
                            if (msg_altatk.AltAttackIdx >= 0)
                            {
                                var altatk = attacker.Active!.AlternativeATKs[msg_altatk.AltAttackIdx];
                                altatk.Effect(state);
                                PublishLog($"激活了【{altatk.Name}】技能");
                                continue;
                            }
                        }

                        // dealing with cloak and invulnerable
                        bool skip_def = false;
                        if (attacker.Active!.IsInvulnerableInATK)
                        {
                            skip_def = true;
                        }
                        else
                        {
                            var anti_cloak_in_def_board = from card in defenser.OnBoard
                                                          where card.IsAntiCloak select card;
                            var anti_cloak_in_def_hand = from card in defenser.Hand
                                                         where card.IsAntiCloak select card;
                            if (!anti_cloak_in_def_board.Any() && !anti_cloak_in_def_hand.Any())
                            {
                                skip_def = true;
                            }
                        }
                        if (!skip_def)
                        {
                            state.Stage.Stage = GameStage.Stages.DEF;
                            PublishState();
                            var msg_def = ExpectMsg<Message.Defense>(PullFromPlayer(defensing_player_idx));
                            if (msg_def.DefenseCardIdx >= 0)
                            {
                                PublishLog(Moves.ActivateCard(state, defensing_player_idx, msg_def.DefenseCardIdx));
                            }
                        }
                        // check defense blocking condition
                        var minelayer_block = from effect in state.AreaEffects
                                              where effect is MinelayerCannotDefenseThisTurn
                                              select effect;
                        if (minelayer_block.Any() && defenser.Active is Minelayer)
                        {
                            PublishLog(Moves.DisableActiveCard(state, defensing_player_idx));
                            PublishState();
                        }
                        PublishLog(Moves.AttackAndDefense(state, random, attacking_player_idx, defensing_player_idx));
                        state.Stage.Stage = GameStage.Stages.SelectATK;
                        PublishState();
                    }
                    else if (msg is Message.EndTurn)
                    {
                        PublishLog(Moves.EndTurn(state));
                        if (Moves.NeedCheckWin(state))
                        {
                            PublishLog(Moves.CheckWin(state));
                            return;
                        }
                        break;
                    }
                    else if (msg is Message.AskForEndGame)
                    {
                        state.Stage.Stage = GameStage.Stages.AskForEndGame;
                        PublishState();
                        var msg_answer = ExpectMsg<Message.AnswerForEndGame>(PullFromPlayer(defensing_player_idx));
                        if (msg_answer.Endgame)
                        {
                            PublishLog(Moves.CheckWin(state));
                            return;
                        }
                    }
                    else
                    {
                        throw new InvalidOperationError(JsonConvert.SerializeObject(msg, jsonSerializerSettings));
                    }
                }
            }
        }
        private NetServer? server;
        private readonly Dictionary<NetConnection, int> map_connection_idx = new();
        private readonly Dictionary<int, NetConnection> map_idx_connection = new();
        private Thread? network_server_loop;
        internal void SetupAndConnect()
        {
            // use TcpListener to trigger Windows Firewall Prompt
            {
                var tcp_server = new TcpListener(IPAddress.Any, 0);
                tcp_server.Start();
                tcp_server.Stop();
            }

            var config = new NetPeerConfiguration("HWCard");
            server = new NetServer(config);
            server.Start();
            Console.WriteLine($"服务器已在端口 {server.Port} 开启");
            Console.WriteLine("===============================");
            Console.WriteLine("等待连接……");

            while (map_connection_idx.Count < 2)
            {
                NetIncomingMessage message = server.WaitMessage(1000 * 3600);
                if (message is not null)
                {
                    if (message.MessageType == NetIncomingMessageType.StatusChanged)
                    {
                        if(message.SenderConnection.Status == NetConnectionStatus.Connected)
                        {
                            var player_idx = map_connection_idx.Count;
                            Console.WriteLine($"已连接来自{message.SenderEndPoint}的玩家{player_idx}");
                            map_connection_idx.Add(message.SenderConnection, player_idx);
                            map_idx_connection.Add(player_idx, message.SenderConnection);
                            PushMsg(player_idx, new Message.AssignPlayerID() { PlayerID = player_idx });
                        }
                        if (message.SenderConnection.Status is
                            not NetConnectionStatus.Connected and
                            not NetConnectionStatus.InitiatedConnect and
                            not NetConnectionStatus.RespondedConnect
                            )
                        {
                            throw new InvalidOperationError($"Uunhandled connection status change: {message.SenderConnection.Status}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationError($"Invalid message({message}) received when waiting for connection");
                    }
                }
                else
                {
                    Console.WriteLine("Waiting connection timeout");
                    Environment.Exit(1);
                }
            }
            network_server_loop = new(server_running);
            network_server_loop.Start();
        }
        private List<ConcurrentQueue<IMessage>> msg_pool = new() { new(), new() }; 
        private void server_running()
        {
            NetIncomingMessage message;
            while (true)
            {
                message = server!.WaitMessage(100);
                if (message is not null)
                {
                    switch (message.MessageType)
                    {
                        case NetIncomingMessageType.Data:
                            {
                                var sender_idx = map_connection_idx[message.SenderConnection];
                                var json = message.ReadString();
                                var msg = JsonConvert.DeserializeObject(json, jsonSerializerSettings) as IMessage;
                                msg_pool[sender_idx].Enqueue(msg!);
                            }
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            // handle connection status messages
                            switch (message.SenderConnection.Status)
                            {
                                case NetConnectionStatus.Disconnected:
                                    var sender_idx = map_connection_idx[message.SenderConnection];
                                    Console.WriteLine($"Player {sender_idx} disconnected");
                                    Environment.Exit(0);
                                    break;
                                default:
                                    throw new InvalidOperationError($"unhandled connection status change: {message.SenderConnection.Status}");
                            }
                            break;
                        case NetIncomingMessageType.DebugMessage:
                            // handle debug messages
                            // (only received when compiled in DEBUG mode)
                            Console.WriteLine(message.ReadString());
                            break;
                        /* .. */
                        default:
                            throw new InvalidOperationError($"unhandled message with type: {message.MessageType}");
                    }
                }
                server.Recycle(message);
            }
        }
    }
}

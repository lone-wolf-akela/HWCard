using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Newtonsoft.Json;
using Lidgren.Network;

using HWCard_Core;
using HWCard_Message;
using Message = HWCard_Message.Message;

namespace HWCard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly JsonSerializerSettings jsonSerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public MainWindow()
        {
            InitializeComponent();
        }

        private int _player_id;
        private int PlayerID
        {
            get => _player_id;
            set
            {
                _player_id = value;
                txt_playerid.Text = $"玩家{_player_id}";
            }
        }
        private int PlayerThisTurn
        {
            get => State.Stage.Player == GameStage.Players.Player0 ? 0 :
                   State.Stage.Player == GameStage.Players.Player1 ? 1 : -1;
        }
        private bool MyTurn
        {
            get => PlayerThisTurn == PlayerID;
        }
        private int EnemyID
        {
            get => PlayerID == 0 ? 1 : 0;
        }
        private PlayerState MyState
        {
            get => State.Players[PlayerID];
        }
        private PlayerState EnemyState
        {
            get => State.Players[EnemyID];
        }
        private void DisableAllActionBtn()
        {
            btn_act.IsEnabled = false;
            btn_drop.IsEnabled = false;
            btn_endturn.IsEnabled = false;
            btn_reqend.IsEnabled = false;
            btn_nodef.IsEnabled = false;
        }
        private GameState _state;
        private GameState State
        {
            get => _state;
            set
            {
                _state = value;

                DisableAllActionBtn();

                txt_turn.Text = $"玩家{PlayerThisTurn}的回合";
                if (MyTurn)
                {
                    switch (_state.Stage.Stage)
                    {
                        case GameStage.Stages.DrawCard:
                            txt_todo.Text = "您正在抽卡……";
                            break;
                        case GameStage.Stages.DropCard:
                            txt_todo.Text = "请选择一张手牌，将其丢弃";
                            break;
                        case GameStage.Stages.SelectATK:
                            txt_todo.Text = "请选择使用一张手牌，或结束本回合";
                            break;
                        case GameStage.Stages.AskForAltATK:
                            txt_todo.Text = "请选择是否使用替代攻击效果";
                            break;
                        case GameStage.Stages.AskForEndGame:
                            txt_todo.Text = "正在等待对方玩家回复是否同意结束游戏……";
                            break;
                        case GameStage.Stages.DEF:
                            txt_todo.Text = "请等待对方玩家进行防御……";
                            break;
                    }
                }
                else
                {
                    switch (_state.Stage.Stage)
                    {
                        case GameStage.Stages.DrawCard:
                            txt_todo.Text = "对方玩家正在抽卡……";
                            break;
                        case GameStage.Stages.DropCard:
                            txt_todo.Text = "对方玩家正在选择弃牌……";
                            break;
                        case GameStage.Stages.SelectATK:
                            txt_todo.Text = "对方玩家正在选择是否攻击……";
                            break;
                        case GameStage.Stages.AskForAltATK:
                            txt_todo.Text = "对方玩家正在选择攻击效果……";
                            break;
                        case GameStage.Stages.AskForEndGame:
                            txt_todo.Text = "对方玩家想要结束本局游戏，是否同意？";
                            Message.AnswerForEndGame msg = new();
                            if (MessageBox.Show("对方玩家想要结束本局游戏，是否同意？", "选择", MessageBoxButton.YesNo, MessageBoxImage.Warning) 
                                == MessageBoxResult.Yes)
                            {
                                msg.Endgame = true;
                            }
                            else
                            {
                                msg.Endgame = false;
                            }
                            SendMsg(msg);
                            break;
                        case GameStage.Stages.DEF:
                            txt_todo.Text = "请选择一张手牌打出以进行防御，或直接放弃防御";
                            break;
                    }
                }
                txt_my_deck_n.Text = $"{MyState.Deck.Count}张";
                txt_your_deck_n.Text = $"{EnemyState.Deck.Count}张";
                txt_my_hand_n.Text = $"{MyState.Hand.Count}张";
                txt_your_hand_n.Text = $"{EnemyState.Hand.Count}张";

                lst_my_hand.Items.Clear();
                lst_your_hand.Items.Clear();

                foreach (var card in MyState.Hand)
                {
                    lst_my_hand.Items.Add(card.Name);
                }
                if (_state.Players[EnemyID].HandVisible)
                {
                    foreach (var card in EnemyState.Hand)
                    {
                        lst_your_hand.Items.Add(card.Name);
                    }
                }

                lst_my_used.Items.Clear();
                lst_your_used.Items.Clear();
                foreach (var card in MyState.OnBoard)
                {
                    lst_my_used.Items.Add(card.Name);
                }
                foreach (var card in EnemyState.OnBoard)
                {
                    lst_your_used.Items.Add(card.Name);
                }
                txt_my_active.Text = MyState.Active?.Name ?? "";
                txt_your_active.Text = EnemyState.Active?.Name ?? "";
            }
        }

        private NetClient client;
        private NetConnection conn;
        private void SendMsg(IMessage msg)
        {
            var message = client.CreateMessage();
            message.Write(JsonConvert.SerializeObject(msg, jsonSerializerSettings));
            client.SendMessage(message, conn, NetDeliveryMethod.ReliableOrdered);
        }
        private void log(string str)
        {
            txt_log.AppendText(str);
            txt_log.AppendText(Environment.NewLine);
            txt_log.CaretIndex = txt_log.Text.Length;
            txt_log.ScrollToEnd();
        }
        private async Task MainLoop()
        {
            NetIncomingMessage net_message;
            while (true)
            {
                net_message = await Task.Run(() => client.WaitMessage(1000));
                if (net_message is null)
                {
                    continue;
                }
                if (net_message.MessageType == NetIncomingMessageType.Data)
                {
                    var json_str = net_message.ReadString();
                    var msg = JsonConvert.DeserializeObject(json_str, jsonSerializerSettings) as IMessage;
                    if (msg is Message.AssignPlayerID assign)
                    {
                        PlayerID = assign.PlayerID;
                    }
                    else if (msg is Message.StateUpdate state_update)
                    {
                        State = state_update.State;
                    }
                    else if (msg is Message.LogUpdate log_update)
                    {
                        log(log_update.Log);
                    }
                    else if (msg is Message.RequestMsg)
                    {
                        if (MyTurn)
                        {
                            if(State.Stage.Stage == GameStage.Stages.SelectATK)
                            {
                                btn_act.IsEnabled = true;
                                btn_endturn.IsEnabled = true;
                                btn_reqend.IsEnabled = true;
                            }
                            else if(State.Stage.Stage == GameStage.Stages.DropCard)
                            {
                                btn_drop.IsEnabled = true;
                            }
                        }
                        else
                        {
                            if (State.Stage.Stage == GameStage.Stages.DEF)
                            {
                                btn_act.IsEnabled = true;
                                btn_nodef.IsEnabled = true;
                            }
                        }
                    }
                    else
                    {
                        log(msg!.GetType().Name);
                    }
                }
                client.Recycle(net_message);
            }
        }
        private async void btn_connect_Click(object sender, RoutedEventArgs e)
        {
            btn_connect.IsEnabled = false;

            var config = new NetPeerConfiguration("HWCard");
            client = new(config);
            client.Start();
            log("正在连接服务器");
            conn = client.Connect(txt_serverip.Text, int.Parse(txt_serverport.Text));

            // timeout after 10s
            bool connected = false;
            for (int i = 0; i < 100; i++)
            {
                await Task.Delay(100);
                if (conn.Peer.Connections.Count > 0)
                {
                    connected = true;
                    break;
                }
            }
            if (!connected)
            {
                log("无法连接到服务器");
                btn_connect.IsEnabled = true;
                return;
            }
            log("已连接到服务器");
            await MainLoop();
        }

        private void lst_my_hand_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selected_id = lst_my_hand.SelectedIndex;
            if (selected_id == -1)
            {
                return;
            }
            var card = MyState.Hand[selected_id];
            txt_desc.Text = card.Description;
        }

        private void lst_your_hand_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selected_id = lst_your_hand.SelectedIndex;
            if (selected_id == -1)
            {
                return;
            }
            var card = EnemyState.Hand[selected_id];
            txt_desc.Text = card.Description;
        }

        private void btn_act_Click(object sender, RoutedEventArgs e)
        {
            int selected_id = lst_my_hand.SelectedIndex;
            if (selected_id == -1)
            {
                return;
            }
            IMessage msg;
            if (MyTurn)
            {
                msg = new Message.Attack()
                {
                    AttackCardIdx = lst_my_hand.SelectedIndex
                };
            }
            else
            {
                msg = new Message.Defense()
                {
                    DefenseCardIdx = lst_my_hand.SelectedIndex
                };
            }
            SendMsg(msg);
        }

        private void btn_drop_Click(object sender, RoutedEventArgs e)
        {
            int selected_id = lst_my_hand.SelectedIndex;
            if (selected_id == -1)
            {
                return;
            }
            Message.DropCard msg = new()
            {
                DropedCardIdx = lst_my_hand.SelectedIndex
            };
            SendMsg(msg);
        }

        private void btn_reqend_Click(object sender, RoutedEventArgs e)
        {
            Message.AskForEndGame msg = new();
            SendMsg(msg);
        }

        private void btn_endturn_Click(object sender, RoutedEventArgs e)
        {
            Message.EndTurn msg = new();
            SendMsg(msg);
        }

        private void lst_my_used_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selected_id = lst_my_used.SelectedIndex;
            if (selected_id == -1)
            {
                return;
            }
            var card = MyState.OnBoard[selected_id];
            txt_desc.Text = card.Description;
        }

        private void lst_your_used_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selected_id = lst_your_used.SelectedIndex;
            if (selected_id == -1)
            {
                return;
            }
            var card = EnemyState.OnBoard[selected_id];
            txt_desc.Text = card.Description;
        }

        private void btn_nodef_Click(object sender, RoutedEventArgs e)
        {
            Message.Defense msg = new()
            {
                DefenseCardIdx = -1
            };
            SendMsg(msg);
        }
    }
}

using System;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;

namespace PingMaster_3._1
{
    public partial class Tracking : Form
    {
        bool is_eng, to_ping = true, to_clear = false;
        int cur_row = 0;
        string received_name, received_dns, received_ip;
        string check_connection, error;

        DataGridViewTextBoxColumn Col0 = new DataGridViewTextBoxColumn();
        DataGridViewTextBoxColumn Col1 = new DataGridViewTextBoxColumn();
        DataGridViewTextBoxColumn Col2 = new DataGridViewTextBoxColumn();
        DataGridViewTextBoxColumn Col3 = new DataGridViewTextBoxColumn();

        Ping ping = new Ping();
        PingReply reply;
        static AutoResetEvent waiter = new AutoResetEvent(false);

        public Tracking(bool loc_eng, string name, string ip)
        {
            received_name = name;
            //received_dns = dns;
            received_ip = ip;
            is_eng = loc_eng;

            InitializeComponent();
        }

        private void Preprocessing()
        {
            Translate();

            if (received_ip != "")
            {
                textBox1.Text = received_ip;
                label1.Text = received_name + " " + received_dns;
            }
            else
            {
                textBox1.Text = "127.0.0.1";
                label1.Text = "Loopback";
            }

            Col0.ReadOnly = true;

            Col1.ReadOnly = true;
            Col1.Width = 60;

            Col2.ReadOnly = true;
            Col2.Width = 195;

            Col3.ReadOnly = true;
            Col3.Width = 25;

            dataGridView1.Columns.Add(Col0);
            dataGridView1.Columns.Add(Col1);
            dataGridView1.Columns.Add(Col2);
            dataGridView1.Columns.Add(Col3);
        }

        private void Translate()
        {
            if (is_eng)
            {
                Text = "Tracking";
                label1.Text = "Name";
                label2.Text = "Type ip that will ping:";
                Col0.HeaderText = "Reply time";
                Col1.HeaderText = "Reply";
                Col2.HeaderText = "Status";
                button1.Text = "Start";

                check_connection = "No connection to the network." + Environment.NewLine + "Check cable connection or network/firewall settings.";
                error = "Input correct ip address.";
            }
            else
            {
                Text = "Слежение";
                label1.Text = "Имя";
                label2.Text = "Введите пингуемый ip адрес:";
                Col0.HeaderText = "Время ответа";
                Col1.HeaderText = "Ответ";
                Col2.HeaderText = "Статус";
                button1.Text = "Старт";

                check_connection = "Нет подключения к сети" + Environment.NewLine + "Проверьте подключение сетевого кабеля или настройки сети/фаерволла";
                error = "Введите правильный ip адрес.";
            }

            Col3.HeaderText = "";
        }

        private void Ping_cl()
        {
            if (textBox1.Text != "" && textBox1.Text != "0.0.0.0")
                ping.SendAsync(textBox1.Text, 1000, waiter);
        }

        private void Tracking_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            to_clear = true;

            ping.SendAsyncCancel();

            FormClosing -= new FormClosingEventHandler(Tracking_FormClosing);
            Close();
        }

        private void Display_reply()
        {
            if (to_clear)
            {
                cur_row = 0;
                dataGridView1.Rows.Clear();
            }
            else
            {
                dataGridView1[0, cur_row].Value = DateTime.Now.ToString().Substring(11) + "." + DateTime.Now.Millisecond.ToString();

                if (reply.Status == IPStatus.Success)
                {
                    if (reply.RoundtripTime > 999)
                    {
                        if (reply.RoundtripTime.ToString().Substring(1)[0] == '0' && reply.RoundtripTime.ToString().Substring(1)[1] == '0')
                            dataGridView1[1, cur_row].Value = reply.RoundtripTime.ToString().Substring(0, 1) + " s " + reply.RoundtripTime.ToString().Substring(3) + " ms";
                        else if (reply.RoundtripTime.ToString().Substring(1)[0] == '0')
                            dataGridView1[1, cur_row].Value = reply.RoundtripTime.ToString().Substring(0, 1) + " s " + reply.RoundtripTime.ToString().Substring(2) + " ms";
                        else
                            dataGridView1[1, cur_row].Value = reply.RoundtripTime.ToString().Substring(0, 1) + " s " + reply.RoundtripTime.ToString().Substring(1) + " ms";
                    }
                    else if (reply.RoundtripTime == 0)
                        dataGridView1[1, cur_row].Value = "<1 ms";
                    else
                        dataGridView1[1, cur_row].Value = reply.RoundtripTime.ToString() + " ms";
                    dataGridView1[3, cur_row].Style.BackColor = Color.GreenYellow;
                }
                else
                {
                    dataGridView1[1, cur_row].Value = "---";
                    dataGridView1[2, cur_row].Value = reply.Status;
                    dataGridView1[3, cur_row].Style.BackColor = Color.Red;
                }

                if (cur_row == 0)
                    dataGridView1[0, cur_row].Selected = true;
                else
                    dataGridView1[0, cur_row - 1].Selected = true;

                cur_row++;

                if (button1.Text == "Стоп" || button1.Text == "Stop")
                    Ping_cl();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            ping.SendAsyncCancel();

            if (textBox1.Text == "127.0.0.1")
                label1.Text = "Loopback";
            else if (textBox1.Text == "8.8.8.8")
                label1.Text = "Google";
            else if (textBox1.Text == "4.2.2.2" || textBox1.Text == "77.88.21.11" || textBox1.Text == "5.255.255.50")
                label1.Text = "Яндекс";
            else
                label1.Text = textBox1.Text;

            switch (is_eng)
            {
                case true:
                    button1.Text = "Start";
                    break;
                case false:
                    button1.Text = "Старт";
                    break;
            }

            to_clear = true;

            if (!to_ping)
            {
                cur_row = 0;
                dataGridView1.Rows.Clear();
            }
        }

        private void Tracking_Load(object sender, EventArgs e)
        {
            Preprocessing();

            ping.PingCompleted += new PingCompletedEventHandler(Received_reply);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Старт" || button1.Text == "Start")
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    int num = 0;

                    if (textBox1.TextLength < 7)
                        MessageBox.Show(error);
                    else
                    {
                        bool norm = true;
                        for (int i = 0, j = 0, dot = 0; i < textBox1.Text.Length; i++)
                        {
                            if (textBox1.Text[i] == '0' || textBox1.Text[i] == '1' || textBox1.Text[i] == '2' || textBox1.Text[i] == '3' || textBox1.Text[i] == '4' || textBox1.Text[i] == '5' || textBox1.Text[i] == '6' || textBox1.Text[i] == '7' || textBox1.Text[i] == '8' || textBox1.Text[i] == '9')
                            {
                                if (j == 0)
                                    num = textBox1.Text[i];
                                else if (j > 0 && j < 3)
                                    num = num * 10 + textBox1.Text[i];
                                else
                                {
                                    norm = false;
                                    MessageBox.Show(error);
                                }

                                j++;
                            }
                            else if (textBox1.Text[i] == '.' && dot < 3 && (num >= 0 || num < 255))
                            {
                                if (j > 3)
                                {
                                    norm = false;
                                    MessageBox.Show(error);
                                }

                                num = 0;
                                j = 0;
                                dot++;
                            }
                            else
                            {
                                norm = false;
                                MessageBox.Show(error);
                            }
                        }

                        if (norm)
                        {
                            switch (is_eng)
                            {
                                case true:
                                    button1.Text = "Stop";
                                    break;
                                case false:
                                    button1.Text = "Стоп";
                                    break;
                            }

                            //to_ping = true;
                            to_clear = false;
                            if (to_ping)
                                Ping_cl();
                        }
                    }
                }
                else
                    MessageBox.Show(check_connection);
            }
            else
            {
                //ping.SendAsyncCancel();

                switch (is_eng)
                {
                    case true:
                        button1.Text = "Start";
                        break;
                    case false:
                        button1.Text = "Старт";
                        break;
                }

                to_ping = false;
            }
        }

        private void Received_reply(object sender, PingCompletedEventArgs e)
        {
            if (e.Cancelled)
                ((AutoResetEvent)e.UserState).Set();

            if (e.Error != null)
                ((AutoResetEvent)e.UserState).Set();

            // Let the main thread resume.
            ((AutoResetEvent)e.UserState).Set();

            reply = e.Reply;

            if (!to_clear)
            {
                to_ping = true;
                dataGridView1.Rows.Add();
                Display_reply();
            }
        }
    }
}

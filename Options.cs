using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PingMaster_3._1
{
    public partial class Options : Form
    {
        bool is_eng;

        public bool DNS { get; private set; }
        public bool IP { get; private set; }
        public bool RT { get; private set; }

        public int Group { get; private set; }
        public int Period { get; private set; }
        public int Timeout { get; private set; }
        public int Packets { get; private set; }

        public Options(bool loc_eng, bool dns, bool ip, bool rt, int group, int period, int timeout, int packets)
        {
            is_eng = loc_eng;
            DNS = dns;
            IP = ip;
            RT = rt;

            Group = group;
            Period = period;
            Timeout = timeout;
            Packets = packets;

            InitializeComponent();
        }

        private void Preprocessing()
        {
            checkBox1.Checked = DNS;
            checkBox2.Checked = IP;
            checkBox3.Checked = RT;

            numericUpDown1.Value = Period;
            numericUpDown2.Value = Timeout;
            numericUpDown3.Value = Packets;
        }

        private void Translate()
        {
            if (is_eng)
            {
                Text = "Settings of " + (Group + 1) + " clients group";
                checkBox1.Text = "Show DNS names";
                checkBox2.Text = "Show IP addresses";
                checkBox3.Text = "Show response time";
                label1.Text = "Autoping period:";
                label2.Text = "Timeout:";
                label3.Text = "Packets count:";
                label4.Text = "min";
                label5.Text = "sec";
                button1.Text = "Apply";
            }
            else
            {
                Text = "Настройки " + (Group + 1) + " группы клиентов";
                checkBox1.Text = "Показывать DNS имена";
                checkBox2.Text = "Показывать IP адреса";
                checkBox3.Text = "Показывать время ответов";
                label1.Text = "Период автопинга:";
                label2.Text = "Время ожидания:";
                label3.Text = "Кол-во пакетов:";
                label4.Text = "мин";
                label5.Text = "сек";
                button1.Text = "Применить";
            }
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            Preprocessing();
            Translate();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            DNS = checkBox1.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            IP = checkBox2.Checked;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            RT = checkBox3.Checked;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            Period = (int)numericUpDown1.Value;
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            Timeout = (int)numericUpDown2.Value;
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            Packets = (int)numericUpDown3.Value;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}

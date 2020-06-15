using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PingMaster_3._1
{
    public partial class Form1 : Form
    {
        bool is_english = false, // проверка на использование английской версии программы
               is_tracking = false, // проверка открытого окна слежения (для того, чтобы не создавалось более 1 окна слежения)
               to_close = false,
               is_ping_all = false, is_started;

        bool[] ping_completed = new bool[8];

        bool[,] states = new bool[8, 4]; // настройки (да/нет) групп клиентов, принимаемых из файла: [группа (1-8), настройки (автопинговать/показывать dns/показывать ip/показывать время ответа)]

        int groups_num = 0; // количество групп для отображения, задействованных при старте программы

        int[,] g_settings = new int[8, 6]; // настройки групп в виде: [группа (1-8), настройки (кол-во клиентов/текущий клиент/текущий период автопинга/текущий таймаут/кол-во запросов/текущее кол-во запросов)]

        string pinging, check_connection, fill_clients_list; // текст некоторых элементов, зависящий от выбранного языка

        string[] al, // сырой список абонентов из файла
            g_names = new string[8];

        string[,][] g_lists = new string[8, 11][]; // трёхмерный список клиентов в виде: [группа (1-8), аргументы (имя/dns/ip/время 1 опроса/ответ 1 опроса/время 2 опроса/.../ответ 4 опроса)] [клиент]

        //
        //
        //

        static Ping ping_g1 = new Ping(),
            ping_g2 = new Ping(),
            ping_g3 = new Ping(),
            ping_g4 = new Ping(),
            ping_g5 = new Ping(),
            ping_g6 = new Ping(),
            ping_g7 = new Ping(),
            ping_g8 = new Ping();

        static AutoResetEvent waiter1 = new AutoResetEvent(false),
            waiter2 = new AutoResetEvent(false),
            waiter3 = new AutoResetEvent(false),
            waiter4 = new AutoResetEvent(false),
            waiter5 = new AutoResetEvent(false),
            waiter6 = new AutoResetEvent(false),
            waiter7 = new AutoResetEvent(false),
            waiter8 = new AutoResetEvent(false);

        PingReply reply_g1, reply_g2, reply_g3, reply_g4, reply_g5, reply_g6, reply_g7, reply_g8;

        Options settings;

        Tracking tracking;

        //
        //
        //

        #region Константы

        private const int C_sec = 1000, C_min = 60000;

        private readonly string[] StandartConfigList = new string[]
            { "Language: rus", "Autoping all: yes", "",
            "Group 1 name: 1. Сетевое коммутационное оборудование", "Group 2 name: 2. Сетевые абоненты АС МКС", "Group 3 name: 3. Сетевые абоненты РС МКС", "Group 4 name: 4. Служебные системы РС МКС", "",
            "Group 1 autoping: no", "Group 1 show ip: no", "Group 1 show response time: yes", "Group 1 autoping timer (min): 1", "Group 1 timeout (sec): 3", "Group 1 packets count: 1", "",
            "Group 2 autoping: no", "Group 2 show ip: no", "Group 2 show response time: yes", "Group 2 autoping timer (min): 1", "Group 2 timeout (sec): 3", "Group 2 packets count: 1", "",
            "Group 3 autoping: no", "Group 3 show ip: no", "Group 3 show response time: yes", "Group 3 autoping timer (min): 1", "Group 3 timeout (sec): 3", "Group 3 packets count: 1", "",
            "Group 4 autoping: no", "Group 4 show ip: no", "Group 4 show response time: yes", "Group 4 autoping timer (min): 1", "Group 4 timeout (sec): 3", "Group 4 packets count: 1", "",
            "Group 5 autoping: no", "Group 5 show ip: no", "Group 5 show response time: yes", "Group 5 autoping timer (min): 1", "Group 5 timeout (sec): 3", "Group 5 packets count: 1", "",
            "Group 6 autoping: no", "Group 6 show ip: no", "Group 6 show response time: yes", "Group 6 autoping timer (min): 1", "Group 6 timeout (sec): 3", "Group 6 packets count: 1", "",
            "Group 7 autoping: no", "Group 7 show ip: no", "Group 7 show response time: yes", "Group 7 autoping timer (min): 1", "Group 7 timeout (sec): 3", "Group 7 packets count: 1", "",
            "Group 8 autoping: no", "Group 8 show ip: no", "Group 8 show response time: yes", "Group 8 autoping timer (min): 1", "Group 8 timeout (sec): 3", "Group 8 packets count: 1" };

        private readonly string[] StandartClientList = new string[]
            { "Loopback/127.0.0.1", "БРИ-1/10.1.1.254", "БРИ-2/10.1.2.254", "БРИ-3/192.168.60.254", "SM BelAir WAP/192.168.68.73", "АСП/10.1.2.250", "",
            "USL ER-SWB-J1, J2/192.168.67.250", "USL ER-SWA/192.168.60.253", "ISS Server 1/192.168.60.51", "LS1/192.168.60.53", "Lab printer/192.168.60.82", "",
            "RSE1/10.1.1.3", "RSE2/10.1.1.2", "RSK1/10.1.1.4", "RSK2/10.1.1.5", "RSS1/10.1.2.1", "RSS2/10.1.1.1", "RSE-Med/10.1.1.7", "Mediaserver AGAT/10.1.1.80", "SM Printer/192.168.60.81", "СКП-УП(Умная полка)/10.1.1.49", "",
            "FS1/10.1.1.100", "ТВМ1-Н/10.1.3.1", "БПИ-НЧ (TRPU)/192.168.249.1", "БЗУ/10.1.11.5", "MDM (ШСС)/10.1.3.50" };

        #endregion Константы

        //
        //
        //

        #region Методы

        public Form1()
        {
            InitializeComponent();
        }

        private void PreProcessing()
        {
            is_started = true; // программа стартовала

            if (!File.Exists("Clients.txt"))
            { // если файл со списком клиентов не существует, то...
                FileStream f = File.Create("Clients.txt"); // создаём его
                f.Close();
                File.WriteAllLines("Clients.txt", StandartClientList); // и заполняем стандартным списком
            }

            al = File.ReadAllLines("Clients.txt"); // читаем список клиентов

            Counting();
            CheckConfig();
            CheckLog();

            File.AppendAllText("Logs//" + DateTime.Now.Date.ToString().Substring(0, 10) + ".log", "Программа запущена " + DateTime.Now.Date.ToString().Substring(0, 11) + " в " + DateTime.Now.ToString().Substring(11) + "." + DateTime.Now.Millisecond.ToString() + Environment.NewLine);
            File.AppendAllText("Logs//" + DateTime.Now.Date.ToString().Substring(0, 10) + ".log", Environment.NewLine);

            groupBox1.Text = g_names[0];
            groupBox2.Text = g_names[1];
            groupBox3.Text = g_names[2];
            groupBox4.Text = g_names[3];
            groupBox5.Text = g_names[4];
            groupBox6.Text = g_names[5];
            groupBox7.Text = g_names[6];
            groupBox8.Text = g_names[7];

            FillClientsList();
            EnableElements();
            SortGrids();
            SortColumns();
            Translate();
            CopyText();
            CopyElements();

            ping_g1.PingCompleted += new PingCompletedEventHandler(Received_reply_g1);
            ping_g2.PingCompleted += new PingCompletedEventHandler(Received_reply_g2);
            ping_g3.PingCompleted += new PingCompletedEventHandler(Received_reply_g3);
            ping_g4.PingCompleted += new PingCompletedEventHandler(Received_reply_g4);
            ping_g5.PingCompleted += new PingCompletedEventHandler(Received_reply_g5);
            ping_g6.PingCompleted += new PingCompletedEventHandler(Received_reply_g6);
            ping_g7.PingCompleted += new PingCompletedEventHandler(Received_reply_g7);
            ping_g8.PingCompleted += new PingCompletedEventHandler(Received_reply_g8);

            checkBox1.Checked = states[0, 0];
            checkBox2.Checked = states[1, 0];
            checkBox3.Checked = states[2, 0];
            checkBox4.Checked = states[3, 0];
            checkBox5.Checked = states[4, 0];
            checkBox6.Checked = states[5, 0];
            checkBox7.Checked = states[6, 0];
            checkBox8.Checked = states[7, 0];

            if (is_ping_all)
                PingAll();
        }

        private void Counting()
        {
            if (al.Count() > 0)
            { // если количество клиентов больше нуля, то...
                groups_num++; // считаем, что существует хотя бы 1 группа

                for (int s = 0; s < al.Count(); s++)
                { // в каждой строке списка клиентов...
                    if (al[s] == "")
                        groups_num++; // при пустой строке наращиваем группу
                    else
                        g_settings[groups_num - 1, 0]++; // наращиваем количество клиентов
                }
            }
            else
                MessageBox.Show(fill_clients_list);

            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 11; j++)
                    g_lists[i, j] = new string[g_settings[i, 0]]; // создаём продвинутый список клиентов

            for (int i = 0; i < 8; i++)
                ping_completed[i] = true; // помечаем все группы (даже не существующие) как пропингованные
        }

        private void CheckConfig()
        {
            if (File.Exists("Config.ini"))
            { // если файл конфигураций существует, то...
                string[] config = File.ReadAllLines("Config.ini"); // считываем его в список

                for (int i = 0; i < config.Count(); i++)
                { // в каждой строке списка...
                    if (config[i] == "Language: eng")
                        is_english = true;
                    else if (config[i] == "Language: rus")
                        is_english = false; // проверяем язык (таким образом можно в любом порядке вписывать конфигурации в файл)

                    if (config[i] == "Autoping all: yes" || config[i] == "Autoping all: y")
                        is_ping_all = true;
                    else if (config[i] == "Autoping all: no" || config[i] == "Autoping all: n")
                        is_ping_all = false; // проверяем пинговать ли всех при запуске или нет

                    for (int g = 0; g < groups_num; g++)
                    { // для каждой группы...
                        if (config[i].Length >= 13 && config[i].Substring(0, 13) == "Group " + (g + 1) + " name:")
                            g_names[g] = config[i].Substring(14); // читаем название группы

                        if (config[i] == "Group " + (g + 1) + " autoping: yes" || config[i] == "Group " + (g + 1) + " autoping: y")
                            states[g, 0] = true;
                        else if (config[i] == "Group " + (g + 1) + " autoping: no" || config[i] == "Group " + (g + 1) + " autoping: n")
                            states[g, 0] = false; // проверяем включать ли галочку автопинга группы

                        if (config[i] == "Group " + (g + 1) + " show dns: yes" || config[i] == "Group " + (g + 1) + " show dns: y")
                            states[g, 1] = true;
                        else if (config[i] == "Group " + (g + 1) + " show dns: no" || config[i] == "Group " + (g + 1) + " show dns: n")
                            states[g, 1] = false; // проверяем показывать ли dns имена клиентов группы

                        if (config[i] == "Group " + (g + 1) + " show ip: yes" || config[i] == "Group " + (g + 1) + " show ip: y")
                            states[g, 2] = true;
                        else if (config[i] == "Group " + (g + 1) + " show ip: no" || config[i] == "Group " + (g + 1) + " show ip: n")
                            states[g, 2] = false; // проверяем показывать ли ip адреса клиентов группы

                        if (config[i] == "Group " + (g + 1) + " show response time: yes" || config[i] == "Group " + (g + 1) + " show response time: y")
                            states[g, 3] = true;
                        else if (config[i] == "Group " + (g + 1) + " show response time: no" || config[i] == "Group " + (g + 1) + " show response time: n")
                            states[g, 3] = false; // проверяем показывать ли время ответов клиентов группы

                        if (config[i].Length >= 29 && config[i].Substring(0, 29) == "Group " + (g + 1) + " autoping timer (min):" && int.TryParse(config[i].Substring(30), out _))
                            g_settings[g, 2] = int.Parse(config[i].Substring(30)); // считываем период автопинга группы

                        if (config[i].Length >= 22 && config[i].Substring(0, 22) == "Group " + (g + 1) + " timeout (sec):" && int.TryParse(config[i].Substring(23), out _))
                            g_settings[g, 3] = int.Parse(config[i].Substring(23)); // считываем время для таймаутов ответов группы

                        if (config[i].Length >= 22 && config[i].Substring(0, 22) == "Group " + (g + 1) + " packets count:" && int.TryParse(config[i].Substring(23), out _))
                            g_settings[g, 4] = int.Parse(config[i].Substring(23)); // считываем количество ping запросов группы
                    }
                }
            }
            else
            { // иначе...
                SaveINI();
                CheckConfig();
            }
        }

        private void CheckLog()
        {
            if (!Directory.Exists("Logs\\"))
                Directory.CreateDirectory("Logs\\");

            if (!File.Exists("Logs\\" + DateTime.Now.Date.ToString().Substring(0, 10) + ".log"))
            {
                FileStream f = File.Create("Logs\\" + DateTime.Now.Date.ToString().Substring(0, 10) + ".log");
                f.Close();
            }
        }

        private void FillClientsList()
        {
            for (int s = 0, cl = 0, group = 0; s < al.Count(); s++)
            {
                if (al[s] == "")
                {
                    group++;
                    cl = 0;
                }
                else
                {
                    for (int c = 0, flag = 0; c < al[s].Length; c++)
                    {
                        if (al[s][c] == '/') // Правило разбиения строки на компоненты (Имя/DNS/IP), так как временно dns вычленяется, то флаг прибавляется на 2 значения сразу
                            flag += 2;
                        else
                            g_lists[group, flag][cl] += al[s][c];
                    }
                    cl++;
                }
            }
        }

        private void EnableElements()
        {
            if (groups_num > 1)
                groupBox2.Visible = true;

            if (groups_num > 2)
                groupBox3.Visible = true;

            if (groups_num > 3)
                groupBox4.Visible = true;

            if (groups_num > 4)
                groupBox5.Visible = true;

            if (groups_num > 5)
                groupBox6.Visible = true;

            if (groups_num > 6)
                groupBox7.Visible = true;

            if (groups_num > 7)
                groupBox8.Visible = true;

            if (g_settings[0, 0] >= 15)
                dataGridView1.Rows.Add(g_settings[0, 0]);
            else
                dataGridView1.Rows.Add(15);

            if (g_settings[1, 0] >= 15)
                dataGridView2.Rows.Add(g_settings[1, 0]);
            else
                dataGridView2.Rows.Add(15);

            if (g_settings[2, 0] >= 15)
                dataGridView3.Rows.Add(g_settings[2, 0]);
            else
                dataGridView3.Rows.Add(15);

            if (g_settings[3, 0] >= 15)
                dataGridView4.Rows.Add(g_settings[3, 0]);
            else
                dataGridView4.Rows.Add(15);

            if (g_settings[4, 0] >= 15)
                dataGridView5.Rows.Add(g_settings[4, 0]);
            else
                dataGridView5.Rows.Add(15);

            if (g_settings[5, 0] >= 15)
                dataGridView6.Rows.Add(g_settings[5, 0]);
            else
                dataGridView6.Rows.Add(15);

            if (g_settings[6, 0] >= 15)
                dataGridView7.Rows.Add(g_settings[6, 0]);
            else
                dataGridView7.Rows.Add(15);

            if (g_settings[7, 0] >= 15)
                dataGridView8.Rows.Add(g_settings[7, 0]);
            else
                dataGridView8.Rows.Add(15);
        }

        private void SortGrids()
        {
            FillGrid(dataGridView1, 0);

            if (groups_num > 1)
                FillGrid(dataGridView2, 1);

            if (groups_num > 2)
                FillGrid(dataGridView3, 2);

            if (groups_num > 3)
                FillGrid(dataGridView4, 3);

            if (groups_num > 4)
                FillGrid(dataGridView5, 4);

            if (groups_num > 5)
                FillGrid(dataGridView6, 5);

            if (groups_num > 6)
                FillGrid(dataGridView7, 6);

            if (groups_num > 7)
                FillGrid(dataGridView8, 7);
        }

        private void FillGrid(DataGridView grid, int group)
        {
            for (int cl = 0; cl < g_settings[group, 0]; cl++)
                for (int col = 0; col < 3; col++)
                    grid[col, cl].Value = g_lists[group, col][cl];
        }

        private void SortColumns()
        {
            SwitchColumns(0, Column1b, Column1c, Column1d);
            SwitchColumns(1, Column2b, Column2c, Column2d);
            SwitchColumns(2, Column3b, Column3c, Column3d);
            SwitchColumns(3, Column4b, Column4c, Column4d);
            SwitchColumns(4, Column5b, Column5c, Column5d);
            SwitchColumns(5, Column6b, Column6c, Column6d);
            SwitchColumns(6, Column7b, Column7c, Column7d);
            SwitchColumns(7, Column8b, Column8c, Column8d);
        }

        private void SwitchColumns(int group, DataGridViewTextBoxColumn col1, DataGridViewColumn col2, DataGridViewColumn col3)
        {
            col1.Visible = states[group, 1];
            col2.Visible = states[group, 2];
            col3.Visible = states[group, 3];
        }

        private void Translate()
        {
            if (is_english)
            {
                toolStripButton1.Text = "File";
                Open_iniTSMitem.Text = "Open .INI file";
                Save_iniTSMitem.Text = "Save .INI file";
                Open_logTSMitem.Text = "Open log file";
                Open_clientsTSMitem.Text = "Open clients list";
                button0.Text = "Ping all";
                toolStripButton3.Text = "Tracking";
                toolStripButton4.Text = "Settings";
                LanguageTSMitem.Text = "Language";
                Lang_rusTSMitem.Text = "Russian";
                Lang_engTSMitem.Text = "English";
                toolStripButton5.Text = "Help";
                User_guideTSMitem.Text = "User's guide";
                AboutTSMitem.Text = "About";

                Column1a.HeaderText = "Name";
                Column1d.HeaderText = "Time";
                Column1e.HeaderText = "Status";

                checkBox1.Text = "Autoping 1st group";
                checkBox2.Text = "Autoping 2nd group";
                checkBox3.Text = "Autoping 3rd group";
                checkBox4.Text = "Autoping 4th group";

                button1.Text = "Settings";
                button2.Text = "Settings";
                button3.Text = "Settings";
                button4.Text = "Settings";

                button11.Text = "Ping 1st group";
                button12.Text = "Ping 2nd group";
                button13.Text = "Ping 3rd group";
                button14.Text = "Ping 4th group";

                pinging = "Pinging...";
                check_connection = "No connection to the network." + Environment.NewLine + "Check cable connection or network/firewall settings.";
                fill_clients_list = "Fill clients list.";
            }
            else
            {
                toolStripButton1.Text = "Файл";
                Open_iniTSMitem.Text = "Открыть .INI файл";
                Save_iniTSMitem.Text = "Сохранить .INI файл";
                Open_logTSMitem.Text = "Открыть лог файл";
                Open_clientsTSMitem.Text = "Открыть список клиентов";
                button0.Text = "Опрос всех абонентов";
                toolStripButton3.Text = "Слежение";
                toolStripButton4.Text = "Настройки";
                LanguageTSMitem.Text = "Язык";
                Lang_rusTSMitem.Text = "Русский";
                Lang_engTSMitem.Text = "Английский";
                toolStripButton5.Text = "Помощь";
                User_guideTSMitem.Text = "Руководство пользователя";
                AboutTSMitem.Text = "О программе";

                Column1a.HeaderText = "Имя";
                Column1d.HeaderText = "Время";
                Column1e.HeaderText = "Статус";

                checkBox1.Text = "Автоматический опрос 1 группы";
                checkBox2.Text = "Автоматический опрос 2 группы";
                checkBox3.Text = "Автоматический опрос 3 группы";
                checkBox4.Text = "Автоматический опрос 4 группы";

                button1.Text = "Настройки";
                button2.Text = "Настройки";
                button3.Text = "Настройки";
                button4.Text = "Настройки";

                button11.Text = "Опрос 1 группы";
                button12.Text = "Опрос 2 группы";
                button13.Text = "Опрос 3 группы";
                button14.Text = "Опрос 4 группы";

                pinging = "Опрос...";
                check_connection = "Нет подключения к сети" + Environment.NewLine + "Проверьте подключение сетевого кабеля или настройки сети/фаерволла";
                fill_clients_list = "Заполните список клиентов.";
            }
        }

        private void CopyText()
        {
            Column2a.HeaderText = Column1a.HeaderText;
            Column3a.HeaderText = Column1a.HeaderText;
            Column4a.HeaderText = Column1a.HeaderText;
            Column5a.HeaderText = Column1a.HeaderText;
            Column6a.HeaderText = Column1a.HeaderText;
            Column7a.HeaderText = Column1a.HeaderText;
            Column8a.HeaderText = Column1a.HeaderText;

            Column2b.HeaderText = Column1b.HeaderText;
            Column3b.HeaderText = Column1b.HeaderText;
            Column4b.HeaderText = Column1b.HeaderText;
            Column5b.HeaderText = Column1b.HeaderText;
            Column6b.HeaderText = Column1b.HeaderText;
            Column7b.HeaderText = Column1b.HeaderText;
            Column8b.HeaderText = Column1b.HeaderText;

            Column2c.HeaderText = Column1c.HeaderText;
            Column3c.HeaderText = Column1c.HeaderText;
            Column4c.HeaderText = Column1c.HeaderText;
            Column5c.HeaderText = Column1c.HeaderText;
            Column6c.HeaderText = Column1c.HeaderText;
            Column7c.HeaderText = Column1c.HeaderText;
            Column8c.HeaderText = Column1c.HeaderText;

            Column2d.HeaderText = Column1d.HeaderText;
            Column3d.HeaderText = Column1d.HeaderText;
            Column4d.HeaderText = Column1d.HeaderText;
            Column5d.HeaderText = Column1d.HeaderText;
            Column6d.HeaderText = Column1d.HeaderText;
            Column7d.HeaderText = Column1d.HeaderText;
            Column8d.HeaderText = Column1d.HeaderText;

            Column2e.HeaderText = Column1e.HeaderText;
            Column3e.HeaderText = Column1e.HeaderText;
            Column4e.HeaderText = Column1e.HeaderText;
            Column5e.HeaderText = Column1e.HeaderText;
            Column6e.HeaderText = Column1e.HeaderText;
            Column7e.HeaderText = Column1e.HeaderText;
            Column8e.HeaderText = Column1e.HeaderText;
        }

        private void CopyElements()
        {
            groupBox2.Location = new Point(groupBox1.Location.X, groupBox1.Location.Y);
            groupBox3.Location = new Point(groupBox1.Location.X, groupBox1.Location.Y);
            groupBox4.Location = new Point(groupBox1.Location.X, groupBox1.Location.Y);
            groupBox5.Location = new Point(groupBox1.Location.X, groupBox1.Location.Y);
            groupBox6.Location = new Point(groupBox1.Location.X, groupBox1.Location.Y);
            groupBox7.Location = new Point(groupBox1.Location.X, groupBox1.Location.Y);
            groupBox8.Location = new Point(groupBox1.Location.X, groupBox1.Location.Y);

            groupBox2.Size = new Size(groupBox1.Width, groupBox1.Height);
            groupBox3.Size = new Size(groupBox1.Width, groupBox1.Height);
            groupBox4.Size = new Size(groupBox1.Width, groupBox1.Height);
            groupBox5.Size = new Size(groupBox1.Width, groupBox1.Height);
            groupBox6.Size = new Size(groupBox1.Width, groupBox1.Height);
            groupBox7.Size = new Size(groupBox1.Width, groupBox1.Height);
            groupBox8.Size = new Size(groupBox1.Width, groupBox1.Height);

            checkBox2.Location = new Point(checkBox1.Location.X, checkBox1.Location.Y);
            checkBox3.Location = new Point(checkBox1.Location.X, checkBox1.Location.Y);
            checkBox4.Location = new Point(checkBox1.Location.X, checkBox1.Location.Y);
            checkBox5.Location = new Point(checkBox1.Location.X, checkBox1.Location.Y);
            checkBox6.Location = new Point(checkBox1.Location.X, checkBox1.Location.Y);
            checkBox7.Location = new Point(checkBox1.Location.X, checkBox1.Location.Y);
            checkBox8.Location = new Point(checkBox1.Location.X, checkBox1.Location.Y);

            button2.Location = new Point(button1.Location.X, button1.Location.Y);
            button3.Location = new Point(button1.Location.X, button1.Location.Y);
            button4.Location = new Point(button1.Location.X, button1.Location.Y);
            button5.Location = new Point(button1.Location.X, button1.Location.Y);
            button6.Location = new Point(button1.Location.X, button1.Location.Y);
            button7.Location = new Point(button1.Location.X, button1.Location.Y);
            button8.Location = new Point(button1.Location.X, button1.Location.Y);

            button12.Location = new Point(button11.Location.X, button11.Location.Y);
            button13.Location = new Point(button11.Location.X, button11.Location.Y);
            button14.Location = new Point(button11.Location.X, button11.Location.Y);
            button15.Location = new Point(button11.Location.X, button11.Location.Y);
            button16.Location = new Point(button11.Location.X, button11.Location.Y);
            button17.Location = new Point(button11.Location.X, button11.Location.Y);
            button18.Location = new Point(button11.Location.X, button11.Location.Y);

            dataGridView2.Location = new Point(dataGridView1.Location.X, dataGridView1.Location.Y);
            dataGridView3.Location = new Point(dataGridView1.Location.X, dataGridView1.Location.Y);
            dataGridView4.Location = new Point(dataGridView1.Location.X, dataGridView1.Location.Y);
            dataGridView5.Location = new Point(dataGridView1.Location.X, dataGridView1.Location.Y);
            dataGridView6.Location = new Point(dataGridView1.Location.X, dataGridView1.Location.Y);
            dataGridView7.Location = new Point(dataGridView1.Location.X, dataGridView1.Location.Y);
            dataGridView8.Location = new Point(dataGridView1.Location.X, dataGridView1.Location.Y);
        }

        private void PingAll()
        {
            CheckLog();

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                is_ping_all = true;
                button0.Enabled = false;

                if (ping_completed[0] && ping_completed[1] && ping_completed[2] && ping_completed[3] && ping_completed[4] && ping_completed[5] && ping_completed[6] && ping_completed[7])
                    for (int i = 0; i < groups_num; i++)
                        ping_completed[i] = false;

                ClearGrid(dataGridView1, g_settings[0, 0]);
                ClearGrid(dataGridView2, g_settings[1, 0]);
                ClearGrid(dataGridView3, g_settings[2, 0]);
                ClearGrid(dataGridView4, g_settings[3, 0]);
                ClearGrid(dataGridView5, g_settings[4, 0]);
                ClearGrid(dataGridView6, g_settings[5, 0]);
                ClearGrid(dataGridView7, g_settings[6, 0]);
                ClearGrid(dataGridView8, g_settings[7, 0]);

                if (is_started)
                {
                    if (!checkBox1.Checked)
                        SortPing(1);

                    if (!checkBox2.Checked)
                        SortPing(2);

                    if (!checkBox3.Checked)
                        SortPing(3);

                    if (!checkBox4.Checked)
                        SortPing(4);

                    if (!checkBox5.Checked)
                        SortPing(5);

                    if (!checkBox6.Checked)
                        SortPing(6);

                    if (!checkBox7.Checked)
                        SortPing(7);

                    if (!checkBox8.Checked)
                        SortPing(8);
                }
                else
                {
                    for (int i = 1; i <= groups_num; i++)
                        SortPing(i);
                }
            }
            else
            {
                button0.Enabled = true;
                MessageBox.Show(check_connection);
            }

            is_started = false;
            is_ping_all = false;
        }

        private void SortPing(int group)
        {
            switch (group)
            {
                case 1:
                    PingGroup(group, dataGridView1, button11, checkBox1, ping_g1, g_settings[group - 1, 3] * C_sec, waiter1);
                    break;
                case 2:
                    PingGroup(group, dataGridView2, button12, checkBox2, ping_g2, g_settings[group - 1, 3] * C_sec, waiter2);
                    break;
                case 3:
                    PingGroup(group, dataGridView3, button13, checkBox3, ping_g3, g_settings[group - 1, 3] * C_sec, waiter3);
                    break;
                case 4:
                    PingGroup(group, dataGridView4, button14, checkBox4, ping_g4, g_settings[group - 1, 3] * C_sec, waiter4);
                    break;
                case 5:
                    PingGroup(group, dataGridView5, button15, checkBox5, ping_g5, g_settings[group - 1, 3] * C_sec, waiter5);
                    break;
                case 6:
                    PingGroup(group, dataGridView6, button16, checkBox6, ping_g6, g_settings[group - 1, 3] * C_sec, waiter6);
                    break;
                case 7:
                    PingGroup(group, dataGridView7, button17, checkBox7, ping_g7, g_settings[group - 1, 3] * C_sec, waiter7);
                    break;
                case 8:
                    PingGroup(group, dataGridView8, button18, checkBox8, ping_g8, g_settings[group - 1, 3] * C_sec, waiter8);
                    break;
            }
        }

        private void PingGroup(int current_group, DataGridView grid, Button button, CheckBox check, Ping ping, int timeout, AutoResetEvent waiter)
        {
            current_group--;

            if (g_settings[current_group, 1] < g_settings[current_group, 0])
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    if (grid[0, 0].Value.ToString() != "")
                    {
                        button.Enabled = false;
                        check.Enabled = false;

                        grid[0, g_settings[current_group, 1]].Selected = true;
                        grid[4, g_settings[current_group, 1]].Value = pinging;
                        grid[4, g_settings[current_group, 1]].Style.BackColor = Color.Cyan;
                        grid[4, g_settings[current_group, 1]].Style.SelectionForeColor = Color.White;
                        grid[4, g_settings[current_group, 1]].Style.SelectionBackColor = Color.DarkCyan;

                        PingCl(ping, grid[2, g_settings[current_group, 1]].Value.ToString(), timeout, waiter);
                    }
                    else
                    {
                        button0.Enabled = true;
                        MessageBox.Show("Список клиентов данной группы пуст");
                    }
                }
                else if (!is_ping_all)
                {
                    if (checkBox1.Checked)
                        checkBox1.Checked = false;

                    if (checkBox2.Checked)
                        checkBox2.Checked = false;

                    if (checkBox3.Checked)
                        checkBox3.Checked = false;

                    if (checkBox4.Checked)
                        checkBox4.Checked = false;

                    if (checkBox5.Checked)
                        checkBox5.Checked = false;

                    if (checkBox6.Checked)
                        checkBox6.Checked = false;

                    if (checkBox7.Checked)
                        checkBox7.Checked = false;

                    if (checkBox8.Checked)
                        checkBox8.Checked = false;

                    g_settings[current_group, 1] = 0;
                    button.Enabled = true;
                    check.Enabled = true;
                    ping_completed[current_group] = true;

                    for (int i = 0; i < groups_num; i++)
                        ping_completed[i] = false;

                    button0.Enabled = true;
                    MessageBox.Show(check_connection);
                }
                else
                    MessageBox.Show(check_connection);
            }
            else
            {
                //button0.Enabled = true;
                CheckLog();
            }
        }

        private static void PingCl(Ping ping, string address, int timeout, AutoResetEvent waiter)
        {
            ping.SendAsync(address, timeout, waiter);
        }

        private void SortReply(int group)
        {
            if (to_close)
            {
                ClearGrid(dataGridView1, g_settings[0, 0]);
                ClearGrid(dataGridView2, g_settings[1, 0]);
                ClearGrid(dataGridView3, g_settings[2, 0]);
                ClearGrid(dataGridView4, g_settings[3, 0]);
                ClearGrid(dataGridView5, g_settings[4, 0]);
                ClearGrid(dataGridView6, g_settings[5, 0]);
                ClearGrid(dataGridView7, g_settings[6, 0]);
                ClearGrid(dataGridView8, g_settings[7, 0]);
            }
            else
            {
                switch (group)
                {
                    case 1:
                        DisplayReply(group, dataGridView1, reply_g1, button11, checkBox1);
                        break;
                    case 2:
                        DisplayReply(group, dataGridView2, reply_g2, button12, checkBox2);
                        break;
                    case 3:
                        DisplayReply(group, dataGridView3, reply_g3, button13, checkBox3);
                        break;
                    case 4:
                        DisplayReply(group, dataGridView4, reply_g4, button14, checkBox4);
                        break;
                    case 5:
                        DisplayReply(group, dataGridView5, reply_g5, button15, checkBox5);
                        break;
                    case 6:
                        DisplayReply(group, dataGridView6, reply_g6, button16, checkBox6);
                        break;
                    case 7:
                        DisplayReply(group, dataGridView7, reply_g7, button17, checkBox7);
                        break;
                    case 8:
                        DisplayReply(group, dataGridView8, reply_g8, button18, checkBox8);
                        break;
                }
            }
        }

        private void DisplayReply(int current_group, DataGridView grid, PingReply reply, Button button, CheckBox check)
        {
            current_group--;

            grid[3, g_settings[current_group, 1]].Value = DateTime.Now.ToString().Substring(11) + "." + DateTime.Now.Millisecond.ToString();


            if (reply != null)
            {
                grid[4, g_settings[current_group, 1]].Value = reply.Status;

                if (reply.Status == IPStatus.Success)
                {
                    if (reply.RoundtripTime <= 0)
                        grid[4, g_settings[current_group, 1]].Value += " " + "<1 ms";
                    else
                        grid[4, g_settings[current_group, 1]].Value += " " + reply.RoundtripTime.ToString() + " ms";

                    grid[4, g_settings[current_group, 1]].Style.BackColor = Color.GreenYellow;
                    grid[4, g_settings[current_group, 1]].Style.SelectionBackColor = Color.DarkGreen;
                }
                else
                {
                    grid[4, g_settings[current_group, 1]].Style.BackColor = Color.FromArgb(255, 63, 63);
                    grid[4, g_settings[current_group, 1]].Style.SelectionBackColor = Color.DarkRed;
                }
            }
            else
            {
                grid[4, g_settings[current_group, 1]].Value = "Пакет утерян";
                grid[4, g_settings[current_group, 1]].Style.BackColor = Color.FromArgb(255, 63, 63);
                grid[4, g_settings[current_group, 1]].Style.SelectionBackColor = Color.DarkRed;
            }

            if (g_settings[current_group, 5] == g_settings[current_group, 4] - 1)
            {
                g_settings[current_group, 1]++;
                g_settings[current_group, 5] = 0;
            }
            else
                g_settings[current_group, 5]++;

            if (g_settings[current_group, 1] < g_settings[current_group, 0])
                SortPing(current_group + 1);
            else
            {
                g_settings[current_group, 1] = 0;
                button.Enabled = true;
                check.Enabled = true;
                ping_completed[current_group] = true;

                if (ping_completed[0] && ping_completed[1] && ping_completed[2] && ping_completed[3] && ping_completed[4] && ping_completed[5] && ping_completed[6] && ping_completed[7])
                    button0.Enabled = true;
            }
        }

        private void CheckChange(DataGridView grid, System.Windows.Forms.Timer timer, CheckBox check, int currrent_group, int count)
        {
            if (timer.Enabled)
                timer.Stop();

            if (check.Checked)
            {
                timer.Interval = g_settings[currrent_group - 1, 2] * C_min;
                timer.Start();

                for (int i = 0; i < 8; i++)
                    g_settings[i, 1] = 0;

                button0.Enabled = false;
                ping_completed[currrent_group - 1] = false;

                ClearGrid(grid, count);
                SortPing(currrent_group);
            }
            else
            {
                if (!checkBox1.Checked && !checkBox2.Checked && !checkBox3.Checked && !checkBox4.Checked && !checkBox5.Checked && !checkBox6.Checked && !checkBox7.Checked && !checkBox8.Checked)
                    button0.Enabled = true;
            }
        }

        private void ClearGrid(DataGridView grid, int count)
        {
            for (int i = 0; i < count; i++)
            {
                grid[3, i].Value = "";
                grid[4, i].Value = "";

                grid[4, i].Style.BackColor = Color.White;
                grid[4, i].Style.SelectionForeColor = Color.Black;
                grid[4, i].Style.SelectionBackColor = Color.LightGray;
            }
        }

        private void SortStyle() // form adds height and width to client size: x16, y39
        {
            switch (groups_num)
            {
                case 1:
                    ResizeStyle1();
                    break;
                case 2:
                    ResizeStyle2();
                    break;
                case 3:
                    ResizeStyle3();
                    break;
                case 4:
                    ResizeStyle4();
                    break;
                case 5:
                    ResizeStyle5();
                    break;
                case 6:
                    ResizeStyle6();
                    break;
                case 7:
                    ResizeStyle7();
                    break;
                case 8:
                    ResizeStyle8();
                    break;
            }
        }
        // ------------------------
        private void ResizeStyle1()
        {

        }
        // ------------------------
        private void ResizeStyle2()
        {

        }
        // ------------------------
        private void ResizeStyle3()
        {

        }
        // ------------------------
        private void ResizeStyle4()
        {
            groupBox1.Size = new Size((ClientSize.Width - 30) / 2, (ClientSize.Height - 96) / 2);
            groupBox2.Size = new Size(groupBox1.Size.Width, groupBox1.Size.Height);
            groupBox3.Size = new Size(groupBox1.Size.Width, groupBox1.Size.Height);
            groupBox4.Size = new Size(groupBox1.Size.Width, groupBox1.Size.Height);

            //groupBox1.Location = new Point(10, toolStrip1.Size.Height + 35);
            groupBox2.Location = new Point(groupBox1.Width + 20, groupBox1.Location.Y);
            groupBox3.Location = new Point(groupBox1.Location.X, groupBox1.Height + toolStrip1.Size.Height + 60);
            groupBox4.Location = new Point(groupBox1.Width + 20, groupBox1.Height + toolStrip1.Size.Height + 60);

            dataGridView1.Size = new Size(groupBox1.Size.Width - 10, groupBox1.Size.Height - 80);
            dataGridView2.Size = new Size(dataGridView1.Size.Width, dataGridView1.Size.Height);
            dataGridView3.Size = new Size(dataGridView1.Size.Width, dataGridView1.Size.Height);
            dataGridView4.Size = new Size(dataGridView1.Size.Width, dataGridView1.Size.Height);

            //checkBox1.Location = new Point(groupBox1.Size.Width - 125, 20);
            //checkBox2.Location = new Point(groupBox2.Size.Width - 125, 20);
            //checkBox3.Location = new Point(groupBox3.Size.Width - 125, 20);
            //checkBox4.Location = new Point(groupBox4.Size.Width - 125, 20);

            button1.Location = new Point(groupBox1.Size.Width - 105, button1.Location.Y);
            button2.Location = new Point(button1.Location.X, button1.Location.Y);
            button3.Location = new Point(button1.Location.X, button1.Location.Y);
            button4.Location = new Point(button1.Location.X, button1.Location.Y);

            Column1e.Width = dataGridView1.Width - Column1a.Width - 20;
            Column2e.Width = Column1e.Width;
            Column3e.Width = Column1e.Width;
            Column4e.Width = Column1e.Width;

            if (Column1b.Visible)
            {
                //Column1b.Width = (dataGridView1.Width - 205) / 2;
                //Column2b.Width = (dataGridView1.Width - 230) / 2;
                //Column3b.Width = (dataGridView1.Width - 230) / 2;
                //Column4b.Width = (dataGridView1.Width - 230) / 2;

                Column1e.Width -= Column1b.Width;
            }

            if (Column1c.Visible)
                Column1e.Width -= Column1c.Width;

            if (Column1d.Visible)
                Column1e.Width -= Column1d.Width;



            if (Column2b.Visible)
                Column2e.Width -= Column2b.Width;

            if (Column2c.Visible)
                Column2e.Width -= Column2c.Width;

            if (Column2d.Visible)
                Column2e.Width -= Column2d.Width;



            if (Column3b.Visible)
                Column3e.Width -= Column3b.Width;

            if (Column3c.Visible)
                Column3e.Width -= Column3c.Width;

            if (Column3d.Visible)
                Column3e.Width -= Column3d.Width;



            if (Column4b.Visible)
                Column4e.Width -= Column4b.Width;

            if (Column4c.Visible)
                Column4e.Width -= Column4c.Width;

            if (Column4d.Visible)
                Column4e.Width -= Column4d.Width;

            //label1.Text = Size.Width + "." + Size.Height;
        }
        // ------------------------
        private void ResizeStyle5()
        {

        }
        // ------------------------
        private void ResizeStyle6()
        {

        }
        // ------------------------
        private void ResizeStyle7()
        {

        }
        // ------------------------
        private void ResizeStyle8()
        {

        }

        private void ShowTracking()
        {
            if (!is_tracking)
            {
                tracking = new Tracking(is_english, "", "");
                tracking.FormClosed += new FormClosedEventHandler(Tracking_Closed);
                toolStripButton3.Enabled = false;
                is_tracking = true;
                tracking.Show();
            }
        }

        private void ShowTracking(DataGridView grid, int group)
        {
            int selected_row = grid.SelectedCells[0].RowIndex;

            if (selected_row < g_settings[group, 0])
            {
                string s_name = grid[0, selected_row].Value.ToString();
                //string s_dns = grid[1, selected_row].Value.ToString();
                string s_ip = grid[2, selected_row].Value.ToString();

                if (!is_tracking)
                {
                    tracking = new Tracking(is_english, s_name, s_ip);
                    tracking.FormClosed += new FormClosedEventHandler(Tracking_Closed);
                    toolStripButton3.Enabled = false;
                    is_tracking = true;
                    tracking.Show();
                }
            }
        }

        private void ShowSettings(int group)
        {
            group--;
            settings = new Options(is_english, states[group, 1], states[group, 2], states[group, 3], group, g_settings[group, 2], g_settings[group, 3], g_settings[group, 4]);
            settings.FormClosed += new FormClosedEventHandler(Settings_Closed);
            settings.Show();
        }

        private void SaveINI()
        {
            if (!File.Exists("Config.ini"))
            {
                FileStream f = File.Create("Config.ini");
                f.Close();
                File.WriteAllLines("Config.ini", StandartConfigList);
            }
            else
            {
                FileStream f = File.Create("Config.ini");
                f.Close();

                if (is_english)
                    File.AppendAllText("Config.ini", "Language: eng" + Environment.NewLine);
                else
                    File.AppendAllText("Config.ini", "Language: rus" + Environment.NewLine);

                File.AppendAllText("Config.ini", "Autoping all: yes" + Environment.NewLine);

                for (int g = 0; g < groups_num; g++)
                {
                    File.AppendAllText("Config.ini", Environment.NewLine);
                    File.AppendAllText("Config.ini", "Group " + (g + 1) + " name: " + g_names[g] + Environment.NewLine);

                    //if (states[g, 0])
                    switch (g)
                    {
                        case 0:
                            if (checkBox1.Checked)
                                File.AppendAllText("Config.ini", "Group " + (g + 1) + " autoping: y" + Environment.NewLine);
                            else
                                File.AppendAllText("Config.ini", "Group " + (g + 1) + " autoping: n" + Environment.NewLine);
                            break;
                        case 1:
                            if (checkBox2.Checked)
                                File.AppendAllText("Config.ini", "Group " + (g + 1) + " autoping: y" + Environment.NewLine);
                            else
                                File.AppendAllText("Config.ini", "Group " + (g + 1) + " autoping: n" + Environment.NewLine);
                            break;
                        case 2:
                            if (checkBox3.Checked)
                                File.AppendAllText("Config.ini", "Group " + (g + 1) + " autoping: y" + Environment.NewLine);
                            else
                                File.AppendAllText("Config.ini", "Group " + (g + 1) + " autoping: n" + Environment.NewLine);
                            break;
                        case 3:
                            if (checkBox4.Checked)
                                File.AppendAllText("Config.ini", "Group " + (g + 1) + " autoping: y" + Environment.NewLine);
                            else
                                File.AppendAllText("Config.ini", "Group " + (g + 1) + " autoping: n" + Environment.NewLine);
                            break;
                        case 4:
                            if (checkBox5.Checked)
                                File.AppendAllText("Config.ini", "Group " + (g + 1) + " autoping: y" + Environment.NewLine);
                            else
                                File.AppendAllText("Config.ini", "Group " + (g + 1) + " autoping: n" + Environment.NewLine);
                            break;
                        case 5:
                            if (checkBox6.Checked)
                                File.AppendAllText("Config.ini", "Group " + (g + 1) + " autoping: y" + Environment.NewLine);
                            else
                                File.AppendAllText("Config.ini", "Group " + (g + 1) + " autoping: n" + Environment.NewLine);
                            break;
                        case 6:
                            if (checkBox7.Checked)
                                File.AppendAllText("Config.ini", "Group " + (g + 1) + " autoping: y" + Environment.NewLine);
                            else
                                File.AppendAllText("Config.ini", "Group " + (g + 1) + " autoping: n" + Environment.NewLine);
                            break;
                        case 7:
                            if (checkBox8.Checked)
                                File.AppendAllText("Config.ini", "Group " + (g + 1) + " autoping: y" + Environment.NewLine);
                            else
                                File.AppendAllText("Config.ini", "Group " + (g + 1) + " autoping: n" + Environment.NewLine);
                            break;
                    }

                    if (states[g, 2])
                        File.AppendAllText("Config.ini", "Group " + (g + 1) + " show ip: y" + Environment.NewLine);
                    else
                        File.AppendAllText("Config.ini", "Group " + (g + 1) + " show ip: n" + Environment.NewLine);

                    if (states[g, 3])
                        File.AppendAllText("Config.ini", "Group " + (g + 1) + " show response time: y" + Environment.NewLine);
                    else
                        File.AppendAllText("Config.ini", "Group " + (g + 1) + " show response time: n" + Environment.NewLine);

                    File.AppendAllText("Config.ini", "Group " + (g + 1) + " autoping timer (min): " + g_settings[g, 2] + Environment.NewLine);

                    File.AppendAllText("Config.ini", "Group " + (g + 1) + " timeout (sec): " + g_settings[g, 3] + Environment.NewLine);

                    File.AppendAllText("Config.ini", "Group " + (g + 1) + " packets count: " + g_settings[g, 4] + Environment.NewLine);
                }
            }
        }

        #endregion Методы

        //
        //
        //

        #region События

        private void Form1_Load(object sender, EventArgs e)
        {
            PreProcessing();

            Size = new Size(916, 739);
            SortStyle();

            //Application.DoEvents();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            button0.Enabled = false;
            ping_completed[0] = false;

            ClearGrid(dataGridView1, g_settings[0, 0]);
            SortPing(1);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            button0.Enabled = false;
            ping_completed[1] = false;

            ClearGrid(dataGridView2, g_settings[1, 0]);
            SortPing(2);
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            button0.Enabled = false;
            ping_completed[2] = false;

            ClearGrid(dataGridView3, g_settings[2, 0]);
            SortPing(3);
        }

        private void timer4_Tick(object sender, EventArgs e)
        {
            button0.Enabled = false;
            ping_completed[3] = false;

            ClearGrid(dataGridView4, g_settings[3, 0]);
            SortPing(4);
        }

        private void timer5_Tick(object sender, EventArgs e)
        {
            button0.Enabled = false;
            ping_completed[4] = false;

            ClearGrid(dataGridView5, g_settings[4, 0]);
            SortPing(5);
        }

        private void timer6_Tick(object sender, EventArgs e)
        {
            button0.Enabled = false;
            ping_completed[5] = false;

            ClearGrid(dataGridView6, g_settings[5, 0]);
            SortPing(6);
        }

        private void timer7_Tick(object sender, EventArgs e)
        {
            button0.Enabled = false;
            ping_completed[6] = false;

            ClearGrid(dataGridView7, g_settings[6, 0]);
            SortPing(7);
        }

        private void timer8_Tick(object sender, EventArgs e)
        {
            button0.Enabled = false;
            ping_completed[7] = false;

            ClearGrid(dataGridView8, g_settings[7, 0]);
            SortPing(8);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            CheckChange(dataGridView1, Timer1, checkBox1, 1, g_settings[0, 0]);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            CheckChange(dataGridView2, Timer2, checkBox2, 2, g_settings[1, 0]);
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            CheckChange(dataGridView3, Timer3, checkBox3, 3, g_settings[2, 0]);
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            CheckChange(dataGridView4, Timer4, checkBox4, 4, g_settings[3, 0]);
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            CheckChange(dataGridView5, Timer5, checkBox5, 5, g_settings[4, 0]);
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            CheckChange(dataGridView6, Timer6, checkBox6, 6, g_settings[5, 0]);
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            CheckChange(dataGridView7, Timer7, checkBox7, 7, g_settings[6, 0]);
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            CheckChange(dataGridView8, Timer8, checkBox8, 8, g_settings[7, 0]);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ShowSettings(1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ShowSettings(2);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ShowSettings(3);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ShowSettings(4);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ShowSettings(5);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            ShowSettings(6);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            ShowSettings(7);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            ShowSettings(8);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (Timer1.Enabled)
                Timer1.Stop();

            button0.Enabled = false;
            ping_completed[0] = false;

            ClearGrid(dataGridView1, g_settings[0, 0]);
            SortPing(1);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (Timer2.Enabled)
                Timer2.Stop();

            button0.Enabled = false;
            ping_completed[1] = false;

            ClearGrid(dataGridView2, g_settings[1, 0]);
            SortPing(2);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (Timer3.Enabled)
                Timer3.Stop();

            button0.Enabled = false;
            ping_completed[2] = false;

            ClearGrid(dataGridView3, g_settings[2, 0]);
            SortPing(3);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (Timer4.Enabled)
                Timer4.Stop();

            button0.Enabled = false;
            ping_completed[3] = false;

            ClearGrid(dataGridView4, g_settings[3, 0]);
            SortPing(4);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (Timer5.Enabled)
                Timer5.Stop();

            button0.Enabled = false;
            ping_completed[4] = false;

            ClearGrid(dataGridView5, g_settings[4, 0]);
            SortPing(5);
        }

        private void button16_Click(object sender, EventArgs e)
        {
            if (Timer6.Enabled)
                Timer6.Stop();

            button0.Enabled = false;
            ping_completed[5] = false;

            ClearGrid(dataGridView6, g_settings[5, 0]);
            SortPing(6);
        }

        private void button17_Click(object sender, EventArgs e)
        {
            if (Timer7.Enabled)
                Timer7.Stop();

            button0.Enabled = false;
            ping_completed[6] = false;

            ClearGrid(dataGridView7, g_settings[6, 0]);
            SortPing(7);
        }

        private void button0_Click(object sender, EventArgs e)
        {
            PingAll();
        }

        private void button18_Click(object sender, EventArgs e)
        {
            if (Timer8.Enabled)
                Timer8.Stop();

            button0.Enabled = false;
            ping_completed[7] = false;

            ClearGrid(dataGridView8, g_settings[7, 0]);
            SortPing(8);
        }

        private void Save_iniTSMitem_Click(object sender, EventArgs e)
        {
            SaveINI();
        }

        private void dataGridView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowTracking(dataGridView1, 0);
        }

        private void dataGridView2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowTracking(dataGridView2, 1);
        }

        private void dataGridView3_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowTracking(dataGridView3, 2);
        }

        private void dataGridView4_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowTracking(dataGridView4, 3);
        }

        private void dataGridView5_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowTracking(dataGridView5, 4);
        }

        private void dataGridView6_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowTracking(dataGridView6, 5);
        }

        private void dataGridView7_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowTracking(dataGridView7, 6);
        }

        private void dataGridView8_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowTracking(dataGridView8, 7);
        }

        private void Open_iniTSMitem_Click(object sender, EventArgs e)
        {
            Process.Start("C:\\Windows\\System32\\notepad.exe", "Config.ini");
        }

        private void Open_logTSMitem_Click(object sender, EventArgs e)
        {
            Process.Start("C:\\Windows\\System32\\notepad.exe", "Logs//" + DateTime.Now.Date.ToString().Substring(0, 10) + ".log");
        }

        private void Open_clientsTSMitem_Click(object sender, EventArgs e)
        {
            Process.Start("C:\\Windows\\System32\\notepad.exe", "Clients.txt");
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            ShowTracking();
        }

        private void Tracking_Closed(object sender, FormClosedEventArgs e)
        {
            toolStripButton3.Enabled = true;
            is_tracking = false;
        }

        private void Settings_Closed(object sender, FormClosedEventArgs e)
        {
            states[settings.Group, 1] = settings.DNS;
            states[settings.Group, 2] = settings.IP;
            states[settings.Group, 3] = settings.RT;

            g_settings[settings.Group, 2] = settings.Period;
            g_settings[settings.Group, 3] = settings.Timeout;
            g_settings[settings.Group, 4] = settings.Packets;

            SortColumns();
            SortStyle();
        }

        private void Lang_rusTSMitem_Click(object sender, EventArgs e)
        {
            is_english = false;
            Translate();
            CopyText();
        }

        private void Lang_engTSMitem_Click(object sender, EventArgs e)
        {
            is_english = true;
            Translate();
            CopyText();
        }

        private void Switch_dnsTSMitem_Click(object sender, EventArgs e)
        {
            if (Column1b.Visible)
            {
                Column1b.Visible = false;
                Column2b.Visible = false;
                Column3b.Visible = false;
                Column4b.Visible = false;

                MinimumSize = new Size(656, 519);
            }
            else
            {
                Column1b.Visible = true;
                Column2b.Visible = true;
                Column3b.Visible = true;
                Column4b.Visible = true;

                MinimumSize = new Size(896, 519);
            }
            SortStyle();
        }

        private void Received_reply_g1(object sender, PingCompletedEventArgs e)
        {
            if (e.Cancelled)
                ((AutoResetEvent)e.UserState).Set();

            if (e.Error != null)
                ((AutoResetEvent)e.UserState).Set();

            // Let the main thread resume.
            ((AutoResetEvent)e.UserState).Set();

            reply_g1 = e.Reply;
            reply_g1 = e.Reply;
            reply_g1 = e.Reply;
            reply_g1 = e.Reply;
            reply_g1 = e.Reply;
            reply_g1 = e.Reply;
            reply_g1 = e.Reply;

            if (!to_close)
                SortReply(1);
        }

        private void Received_reply_g2(object sender, PingCompletedEventArgs e)
        {
            if (e.Cancelled)
                ((AutoResetEvent)e.UserState).Set();

            if (e.Error != null)
                ((AutoResetEvent)e.UserState).Set();

            // Let the main thread resume.
            ((AutoResetEvent)e.UserState).Set();

            reply_g2 = e.Reply;

            if (!to_close)
                SortReply(2);
        }

        private void Received_reply_g3(object sender, PingCompletedEventArgs e)
        {
            if (e.Cancelled)
                ((AutoResetEvent)e.UserState).Set();

            if (e.Error != null)
                ((AutoResetEvent)e.UserState).Set();

            // Let the main thread resume.
            ((AutoResetEvent)e.UserState).Set();

            reply_g3 = e.Reply;

            if (!to_close)
                SortReply(3);
        }

        private void Received_reply_g4(object sender, PingCompletedEventArgs e)
        {
            if (e.Cancelled)
                ((AutoResetEvent)e.UserState).Set();

            if (e.Error != null)
                ((AutoResetEvent)e.UserState).Set();

            // Let the main thread resume.
            ((AutoResetEvent)e.UserState).Set();

            reply_g4 = e.Reply;

            if (!to_close)
                SortReply(4);
        }

        private void Received_reply_g5(object sender, PingCompletedEventArgs e)
        {
            if (e.Cancelled)
                ((AutoResetEvent)e.UserState).Set();

            if (e.Error != null)
                ((AutoResetEvent)e.UserState).Set();

            // Let the main thread resume.
            ((AutoResetEvent)e.UserState).Set();

            reply_g5 = e.Reply;

            if (!to_close)
                SortReply(5);
        }

        private void Received_reply_g6(object sender, PingCompletedEventArgs e)
        {
            if (e.Cancelled)
                ((AutoResetEvent)e.UserState).Set();

            if (e.Error != null)
                ((AutoResetEvent)e.UserState).Set();

            // Let the main thread resume.
            ((AutoResetEvent)e.UserState).Set();

            reply_g6 = e.Reply;

            if (!to_close)
                SortReply(6);
        }

        private void Received_reply_g7(object sender, PingCompletedEventArgs e)
        {
            if (e.Cancelled)
                ((AutoResetEvent)e.UserState).Set();

            if (e.Error != null)
                ((AutoResetEvent)e.UserState).Set();

            // Let the main thread resume.
            ((AutoResetEvent)e.UserState).Set();

            reply_g7 = e.Reply;

            if (!to_close)
                SortReply(7);
        }

        private void Received_reply_g8(object sender, PingCompletedEventArgs e)
        {
            if (e.Cancelled)
                ((AutoResetEvent)e.UserState).Set();

            if (e.Error != null)
                ((AutoResetEvent)e.UserState).Set();

            // Let the main thread resume.
            ((AutoResetEvent)e.UserState).Set();

            reply_g8 = e.Reply;

            if (!to_close)
                SortReply(8);
        }

        private void Form_ChangedSize(object sender, EventArgs e)
        {
            SortStyle();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            to_close = true;

            File.AppendAllText("Logs//" + DateTime.Now.Date.ToString().Substring(0, 10) + ".log", "Программа закрыта " + DateTime.Now.Date.ToString().Substring(0, 11) + " в " + DateTime.Now.ToString().Substring(11) + "." + DateTime.Now.Millisecond.ToString() + Environment.NewLine);
            File.AppendAllText("Logs//" + DateTime.Now.Date.ToString().Substring(0, 10) + ".log", Environment.NewLine);

            ping_g1.SendAsyncCancel();
            ping_g2.SendAsyncCancel();
            ping_g3.SendAsyncCancel();
            ping_g4.SendAsyncCancel();
            ping_g5.SendAsyncCancel();
            ping_g6.SendAsyncCancel();
            ping_g7.SendAsyncCancel();
            ping_g8.SendAsyncCancel();

            FormClosing -= new FormClosingEventHandler(Form1_FormClosing);
            Close();
            Dispose();
        }
        #endregion События
    }
}

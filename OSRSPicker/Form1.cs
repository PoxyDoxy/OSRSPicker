using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Collections;
using System.Threading;
using HtmlAgilityPack;
using System.IO;

namespace OSRSPicker
{
    public partial class Form1 : Form
    {
        public static class Variables
        {
            // Scrape Data from oldschool.runescape.com/g=oldscape/slu
            // This Class Stores Global Variables.
            public static int world_amount = 0;
            public static int progress_counter = 0;
            public static int trip_count = 0;
            public static int scanspeed;
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Close();
            Application.Exit();
        }

        // Implements the manual sorting of items by columns. 
        class ListViewItemComparer : IComparer
        {
            public ListViewItemComparer()
            {

            }

            // Compare the two rows.
            public int Compare(object x, object y)
            {
                /* 
                   We need to access the same item multiple times, so
                   save a local reference to reduce typecasting over and
                   over again
                */
                ListViewItem FirstItem = (ListViewItem)x;
                ListViewItem SecondItem = (ListViewItem)y;

                /* 
                 *  Compare the two columns of each item, combined to make 
                 *  a single item for comparing.
                 *  SubItems[0] is the first column.
                 *  SubItems[1] is the second column, and so on.
                */
                int resultthing;

                // Sort by First Column (Always Works)
                resultthing = String.Compare(FirstItem.SubItems[0].Text, SecondItem.SubItems[0].Text);

                // Try Sorting by Second Column (Latency), does not always work.
                try { resultthing = String.Compare(FirstItem.SubItems[1].Text, SecondItem.SubItems[1].Text); }
                catch {};
                
                return resultthing;
            }
        }

        private void SetStatus(String status)
        {
            // Call this to set the status label.
            // This Exists so that you don't have to Call Invoke during the Async Thread.
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate() { SetStatus(status); }));
            }
            else
            {
                status_label.Text = status;
            }
        }

        private void pinger_DoWork(object sender, DoWorkEventArgs e)
        {
            // Main Method, called by pressing either Start Button.
            int speed = Convert.ToInt32(e.Argument);

            // Get OSRS World List from their main page.
            SetStatus("Fetching World List");
            WebClient webClient = new WebClient();
            string page = webClient.DownloadString("http://oldschool.runescape.com/g=oldscape/slu");
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(page);

            // Find the main Table that contains the World Data.
            SetStatus("Extracting World Info");
            foreach (HtmlNode row in doc.DocumentNode.SelectNodes("//tbody[@class='server-list__body']"))
            {
                // Count all the rows to find out how many worlds exist.
                HtmlNodeCollection rows = row.SelectNodes("tr");
                Variables.world_amount = rows.Count;
                SetProgressBarMax();

                if (speed == 0)
                {
                    SetStatus("Preparing to Scan");
                    Variables.scanspeed = 0;
                }
                else if (speed == 1)
                {
                    SetStatus("Preparing to Slow Scan");
                    Variables.scanspeed = 1;
                }

                // Begin rotating through each world, extracting the values.
                for (int i = 0; i < rows.Count; ++i)
                {
                    // World
                    String current_world = rows[i].SelectNodes("td")[0].InnerText;
                    current_world = current_world.Replace("Old School", "");
                    int current_world_number = Convert.ToInt32(current_world);

                    // Population
                    String current_players = rows[i].SelectNodes("td")[1].InnerText;
                    current_players = current_players.Replace("players", "");
                    int current_players_number = Convert.ToInt32(current_players);

                    // Country
                    String current_location = rows[i].SelectNodes("td")[2].InnerText;

                    // Members
                    String current_type = rows[i].SelectNodes("td")[3].InnerText;

                    // Activity
                    String current_activity = rows[i].SelectNodes("td")[4].InnerText;
                    if (current_activity == "-")
                    {
                        current_activity = "";
                    };

                    // Domain Name
                    string domainname = "oldschool" + current_world_number + ".runescape.com";

                    // Start Ping using Async threads
                    var thread = new Thread(
                    () =>
                    {
                        // Push all the details to the main Ping Method.
                        RunPing(current_world_number, current_players_number, current_type, current_location, current_activity, domainname);
                    });
                    // GOTTA GO FAST
                    thread.Start();

                    // Sleep for small amount to prevent CPU spiking and also general fuckery.
                    if (speed == 0)
                    {
                        // Normal Speed -> 75ms wait
                        Thread.Sleep(75);
                    }
                    else if (speed == 1)
                    {
                        // Slow Scan -> 375ms wait
                        Thread.Sleep(375);
                    }
                    // Perhaps an even SLOWER scan?
                }
            }            
        }

        private void RunPing(int current_world_number, int current_players_number, string current_type, string current_location, string current_activity, string domainname)
        {
            // Run Ping
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            // Use the default Ttl value which is 128, 
            // but change the fragmentation behavior.
            options.DontFragment = true;

            // Create a buffer of 32 bytes of data to be transmitted. 
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 3000;

            // FIND IP
            string ipaddress;
            IPHostEntry hostEntry;
            hostEntry = Dns.GetHostEntry(domainname);
            if (hostEntry.AddressList.Length > 0)
            {
                var ip = hostEntry.AddressList[0];
                ipaddress = Convert.ToString(ip);
            }
            else
            {
                ipaddress = Convert.ToString("offline");
            }

            string latency;
            latency = "loading";

            // Send ICMP Packet
            PingReply reply1 = pingSender.Send(ipaddress, timeout, buffer, options);

            if (reply1.Status == IPStatus.TimedOut)
            {
                latency = "offline";
            }
            if (reply1.Status == IPStatus.Success)
            {
                latency = Convert.ToString(reply1.RoundtripTime) + "ms";
            }

            // Save to List
            // World, Latency, Population, Members, Location, Type, Domain, IP
            string[] row1 = { latency, Convert.ToString(current_players_number), current_type, current_location, current_activity, domainname, ipaddress };

            Variables.progress_counter++;

            // Push finished results to the list.
            AddToList(current_world_number + 300, row1);

            // Call the cleanup method to tick over all the meters / bars / values.
            // Because this is all called very fast, I've placed it in a Thread Que.
            ThreadPool.QueueUserWorkItem(FinishPinger);
        }

        private void FinishPinger(Object stateInfo)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate() { FinishPinger(stateInfo); }));
            }
            else
            {
                // Save Variable at the time, just incase It's changed half way through.
                int local_copy_ofvar = Variables.progress_counter;

                // Sort List
                this.mainlist.ListViewItemSorter = new ListViewItemComparer();

                // Update the progress bar, only if it's below the current value
                if (progressBar1.Value < local_copy_ofvar)
                {
                    progressBar1.Value = local_copy_ofvar;
                }

                // If Normal Speed
                if (Variables.scanspeed == 0)
                {
                    // Set Status Label
                    SetStatus("Scanning (" + local_copy_ofvar + "/" + Variables.world_amount + ")");
                    // Set Form Title
                    this.Text = "OSRS Picker - Scanning (" + local_copy_ofvar + "/" + Convert.ToString(Variables.world_amount) + ")";
                }
                // If Slow Speed
                else if (Variables.scanspeed == 1)
                {
                    // Set Status Label
                    SetStatus("Slow Scanning (" + local_copy_ofvar + "/" + Variables.world_amount + ")");
                    // Set Form Title
                    this.Text = "OSRS Picker - SlowScanning (" + local_copy_ofvar + "/" + Convert.ToString(Variables.world_amount) + ")";
                }

                // If the values match (100/100, or in other words 100%)
                // All threads are done.
                if (local_copy_ofvar == Variables.world_amount)
                {
                    // Neaten up the columns so they fit
                    foreach (ColumnHeader ch in this.mainlist.Columns)
                    {
                        ch.Width = -2;
                    }

                    // Set Status Label
                    SetStatus("Finished (" + local_copy_ofvar + "/" + Variables.world_amount + ")");
                    // Set Form Title
                    this.Text = "OSRS Picker - Finished (" + local_copy_ofvar + "/" + Convert.ToString(Variables.world_amount) + ")";

                    // Reset buttons to be clicked again.
                    button1.Text = "Scan Worlds"; 
                    button1.Enabled = true;
                    button3.Text = "Slow Scan";
                    button3.Enabled = true;

                    // Progress Bar set to be reset.
                    progressBar1.Value = 0;
                }
            }
        }

        private void SetProgressBarMax()
        {
            // Does what it says...
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate() { SetProgressBarMax(); }));
            }
            else
            {
                progressBar1.Maximum = Variables.world_amount;
            }
        }

        private void AddToList(int worldnum, string[] data)
        {
            // Adds the results to the listview.
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate() { AddToList(worldnum, data); }));
            }
            else
            {
                mainlist.Items.Add(Convert.ToString(worldnum)).SubItems.AddRange(data);
                mainlist.Update();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Load about form.
            Form aboutform = new About();
            aboutform.ShowDialog();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            // Normal Scan
            GoTime(0);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Slow Scan
            GoTime(1);
        }

        private void GoTime(int speed)
        {
            // It's go time.
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate() { GoTime(speed); }));
            }
            else
            {
                Variables.progress_counter = 0;
                mainlist.Items.Clear();
                //mainlist.Clear();
                this.mainlist.Sorting = SortOrder.None;

                if (speed == 0)
                {
                    // Normal Scan
                    button1.Enabled = false;
                    button3.Enabled = false;
                    button1.Text = "Scanning...";
                }
                else if (speed == 1)
                {
                    // Fast Scan
                    button1.Enabled = false;
                    button3.Enabled = false;
                    button3.Text = "Scanning...";
                }

                // Run the Main Background worker with the chosen Speed (NormalScan or SlowScan)
                // This Async Worker free's up the GUI.
                pinger.RunWorkerAsync(speed);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Warn users that HTMLAgilityPack is required to run the program.
            // Else, when you press scan, it won't be able to scape the results from the website.
            // And no results will show up.
            string NameOfAgilityFile = "HtmlAgilityPack.dll";
            if (!(File.Exists(NameOfAgilityFile)))
            {
                MessageBox.Show("Error: 'HtmlAgilityPack.dll' seems to be missing, please copy it over when moving OSRS Picker.");
                status_label.Text = "'HtmlAgilityPack.dll' is missing.";
                button1.Enabled = false;
                button3.Enabled = false;
            }
        }

    }
}

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
using System.Text.RegularExpressions;

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
            // Storing Form States for nonblocking use during async operation
            public static string Members;
            public static string Event;
            public static string Location;
            public static string Sort;
            public static bool CheckMaxLatency;
            public static decimal MaxLatency;
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

            
            public int Compare(object x, object y)
            {
                // Compare the two rows.
                ListViewItem FirstItem = (ListViewItem)x;
                ListViewItem SecondItem = (ListViewItem)y;

                // Sorting by Selected Method (comboSort.text stored in Variables.Sort)
                int resultthing;

                // Sort by World
                if (Variables.Sort == "Sort by World") {
                    try { return Convert.ToInt32(FirstItem.SubItems[0].Text).CompareTo(Convert.ToInt32(SecondItem.SubItems[0].Text)); } catch { return 0; }
                }

                // Sort by Latency
                if (Variables.Sort == "Sort by Latency") { 
                    try {
                        if (FirstItem.SubItems[1].Text == "offline" & SecondItem.SubItems[1].Text == "offline") { return 0; }
                        if (FirstItem.SubItems[1].Text == "offline") { return 1; }
                        if (SecondItem.SubItems[1].Text == "offline") { return -1; }

                        int tempvar1 = Convert.ToInt32(FirstItem.SubItems[1].Text.Replace("ms", ""));
                        int tempvar2 = Convert.ToInt32(SecondItem.SubItems[1].Text.Replace("ms", ""));
                        resultthing = tempvar1.CompareTo(tempvar2);
                        return resultthing;
                    } catch {
                        return 0;
                    }
                }

                // Sort by Population
                if (Variables.Sort == "Sort by Population")
                {
                    try { return Convert.ToInt32(FirstItem.SubItems[2].Text).CompareTo(Convert.ToInt32(SecondItem.SubItems[2].Text)); } catch { return 0; }
                }

                // Sort by Free/Members
                if (Variables.Sort == "Sort by Free/Members")
                {
                    try { return String.Compare(FirstItem.SubItems[3].Text, SecondItem.SubItems[3].Text); } catch { return 0; }
                }

                // Sort by Location
                if (Variables.Sort == "Sort by Location")
                {
                    try { return String.Compare(FirstItem.SubItems[4].Text, SecondItem.SubItems[4].Text); } catch { return 0; }
                }

                // Sort by Type
                if (Variables.Sort == "Sort by Type")
                {
                    try { return String.Compare(FirstItem.SubItems[5].Text, SecondItem.SubItems[5].Text); } catch { return 0; }
                }

                // Sort by Domain
                if (Variables.Sort == "Sort by Domain")
                {
                    try { return Convert.ToInt32(Regex.Replace(FirstItem.SubItems[6].Text, "[^0-9]", "")).CompareTo(Convert.ToInt32(Regex.Replace(SecondItem.SubItems[6].Text, "[^0-9]", ""))); } catch { return 0; }
                }

                // Sort by IP
                if (Variables.Sort == "Sort by IP")
                {
                    try {
                        string[] ip1 = FirstItem.SubItems[7].Text.Split('.');
                        string[] ip2 = SecondItem.SubItems[7].Text.Split('.');
                        long resultip1 = (Convert.ToInt64(ip1[0]) * 255 * 255 * 255) + (Convert.ToInt64(ip1[1]) * 255 * 255 ) + (Convert.ToInt64(ip1[2]) * 255) + (Convert.ToInt64(ip1[3]));
                        long resultip2 = (Convert.ToInt64(ip2[0]) * 255 * 255 * 255) + (Convert.ToInt64(ip2[1]) * 255 * 255) + (Convert.ToInt64(ip2[2]) * 255) + (Convert.ToInt64(ip2[3]));
                        return Convert.ToInt64(resultip1).CompareTo(Convert.ToInt64(resultip2)); } catch { return 0; }
                }
                return 0;
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
            string page = "";
            try 
            { 
                page = webClient.DownloadString("http://oldschool.runescape.com/g=oldscape/slu"); 
            }
            catch
            { 
                SetStatus("Unable to fetch world list.");
                MessageBox.Show("Error: Unable to fetch world list.\nIs the OSRS website down?\nCheck oldschool.runescape.com");
                EnableStartButtons();
                return;
            }
            
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

                    // Get rid of anything that isn't a number
                    current_world = Regex.Replace(current_world, "[^0-9]", "");

                    int current_world_number = 0;
                    try {
                        current_world_number = Convert.ToInt32(current_world);
                    } catch {
                        // Skip the world because it derped
                        continue;
                    }

                    // Population
                    String current_players = "";
                    current_players = rows[i].SelectNodes("td")[1].InnerText;
                    current_players = current_players.Replace("players", "");
                    int current_players_number;
                    try
                    {
                        current_players_number = Convert.ToInt32(current_players);
                    }
                    catch 
                    {
                        current_players_number = 0;
                    }

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

                    bool allG = true;

                    if (Variables.Members != "Free+Members") { if (Variables.Members != current_type) { allG = false; } } // Check Free/Members
                    if (Variables.Event != "Event/NoEvent") { if (current_activity.Length == 0 & Variables.Event == "Event") { allG = false; } } // Check Event
                    if (Variables.Event != "Event/NoEvent") { if (current_activity.Length != 0 & Variables.Event == "No Event") { allG = false; } } // Check NoEvent
                    if (Variables.Location != "All Locations") { if (Variables.Location != current_location) { allG = false; } } // Check Location

                    if (allG)
                    {
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
                    } else {
                        Variables.progress_counter++;

                        // Call the cleanup method to tick over all the meters / bars / values.
                        // Because this is all called very fast, I've placed it in a Thread Que.
                        // This must be called, even if there are no pings made (because of filtering)
                        ThreadPool.QueueUserWorkItem(FinishPinger);
                    }
                }
            }
        }

        private void EnableStartButtons()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate() { EnableStartButtons(); }));
            }
            else
            {
                // Enable Buttons
                button1.Text = "Scan Worlds";
                button1.Enabled = true;
                button3.Text = "Slow Scan";
                button3.Enabled = true;

                // Enable Form Inputs
                comboMembers.Enabled = true;
                comboEvent.Enabled = true;
                comboLocation.Enabled = true;
                comboSort.Enabled = true;
                checkBox1.Enabled = true;
                if (checkBox1.Checked) { numericUpDown1.Enabled = true; label2.Enabled = true; } else { numericUpDown1.Enabled = false; label2.Enabled = false; }
                
            }

        }

        private void RunPing(int current_world_number, int current_players_number, string current_type, string current_location, string current_activity, string domainname)
        {
            // Find IP First
            string ipaddress;
            IPHostEntry hostEntry;
            try
            {
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
            }
            catch 
            {
                MessageBox.Show("Error: Internet is down.\nUnable to query DNS.\rOr hostname simply does not exist.");
                EnableStartButtons();
                return;
            }

            string latency;
            latency = "loading";

            // Measure Latency
            try
            {
                // Try the new TCP connect method
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    try
                    {
                        // Start Timing
                        var watch = System.Diagnostics.Stopwatch.StartNew();

                        // Open Connection
                        socket.Connect(ipaddress, 43594);

                        // Stop Timing
                        watch.Stop();

                        // Calculate Time Difference
                        var elapsedMs = watch.ElapsedMilliseconds;

                        latency = Convert.ToString(elapsedMs) + "ms";
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.ConnectionRefused)
                        {
                            // Port is closed
                            latency = "offline";
                        }
                    }
                }
            }
            catch {
                // Fail Over to the old ICMP method
                try
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

                    // Old ICMP Ping Method
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
                }
                catch {
                    latency = "offline";
                }
            }

            bool allG = true;

            // Check Minimum Latency
            if (latency.Contains("ms"))
            {
                if (Variables.CheckMaxLatency)
                {
                    if (Convert.ToDecimal(latency.Replace("ms", "")) > Variables.MaxLatency)
                    {
                        allG = false;
                    }
                }
            }

            Variables.progress_counter++;

            if (allG)
            {
                // Save to List
                // World, Latency, Population, Members, Location, Type, Domain, IP
                string[] row1 = { latency, Convert.ToString(current_players_number), current_type, current_location, current_activity, domainname, ipaddress };

                // Push finished results to the list.
                AddToList(current_world_number + 300, row1);
            }
            

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
                    EnableStartButtons();

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
            // Disable Form Inputs
            comboMembers.Enabled = false;
            comboEvent.Enabled = false;
            comboLocation.Enabled = false;
            comboSort.Enabled = false;
            checkBox1.Enabled = false;
            numericUpDown1.Enabled = false;

            // Store Form State for nonblocking use during async operation
            Variables.Members = comboMembers.Text;
            Variables.Event = comboEvent.Text;
            Variables.Location = comboLocation.Text;
            Variables.Sort = comboSort.Text;
            Variables.CheckMaxLatency = checkBox1.Checked;
            Variables.MaxLatency = numericUpDown1.Value;



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
            comboMembers.SelectedIndex = 0;
            comboEvent.SelectedIndex = 0;
            comboLocation.SelectedIndex = 0;
            comboSort.SelectedIndex = 1;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked) { numericUpDown1.Enabled = true; label2.Enabled = true; } else { numericUpDown1.Enabled = false; label2.Enabled = false; }
        }
    }
}

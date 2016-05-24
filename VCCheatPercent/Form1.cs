using Earlbot.BLL;
using GTAOnMissionChanger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using VCSplitInfo;

namespace VCCheatPercent
{
    public partial class Form1 : Form
    {
        /* issues:
        * While a cheat is being entered, the game might sometimes pick up a letter and execute it (for example: if the cheat contains 'f', you might sometimes exit your vehicle as well).
        * If you are pressing buttons while a cheat is being entered, the cheat might sometimes not be executed (because you pressing the button will 'invalidate' the cheat).
            - Even though the things written above can still happen, it's very unlikely, since most characters (other than the last one) are being entered through memory, and not being typed.
             
        * Some cheats might crash the game during certain missions (for example: using 'bigbang' during the final part of 'In the Beginning').
         */


        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);


        IRC irc = new IRC();
        Memory memory = new Memory();
        IRC.IncomingMessage formattedMessage;

        Thread cheatThread;
        Thread mainThread;

        DateTime delayedTimer = DateTime.Now.AddMinutes(3); // Start with a delay of 3 minutes. This way the streamer has time to get past the beginning cutscene which might prevent crashing.
        DateTime cooldownTimer = DateTime.Now;

        private bool isActivated = false;
        private bool cooldown = false;
        private bool connected = false;
        private bool wasActive = false; // stores whether 'chkIsActive' was active before it got changed by the code

        private string rawMessage = "";
        private string channel = "";
        private string currentChannel = "";


        List<Cheat> cheatsList = new List<Cheat>();
        Process processVC = Process.GetProcessesByName("gta-vc").FirstOrDefault();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            AddVCCheats();
        }

        /// <summary>
        /// Handles IRC connection
        /// </summary>
        public void MainThread()
        {

            string error = irc.Connect();
            if (error != null)
            {
                Log(error);
                this.Invoke((MethodInvoker)delegate
                {
                    btnConnect.Enabled = true;
                });
            }
            else {

                irc.JoinChannel(txtChannelToJoin.Text.ToLower());
                irc.SendRawMessage("CAP REQ :twitch.tv/membership");

                Log("Started!");

                while (true)
                {
                    Thread.Sleep(20);
                    if (connected)
                    {
                        rawMessage = irc.readRawMessage();

                        if (rawMessage.ToLower().Contains("Error".ToLower()))
                        {
                            Log(rawMessage);
                            Log("Aborted!");
                            connected = false;
                        }
                        // Log(rawMessage);
                        formattedMessage = irc.ReadMessage(rawMessage);

                        #region PingPong_And_UserStatus

                        if (rawMessage.StartsWith("PING"))
                        {
                            irc.SendRawMessage("PONG tmi.twitch.tv\r\n"); // returns a PONG so connection doesn't get closed

                            Log("PING > PONG");

                        }
                        else if (rawMessage.Contains("JOIN #" + channel.ToLower()))
                        {
                            Log(formattedMessage.username + " has joined the channel.");
                        }
                        else if (rawMessage.Contains("PART #" + channel.ToLower()))
                        {
                            Log(formattedMessage.username + " has left the channel.");
                        }
                        else if (rawMessage.StartsWith((":jtv MODE #") + channel.ToLower()))
                        {

                            string[] splitter = rawMessage.Split(' ');

                            if (splitter[3] == "+o")
                            {
                                Log(formattedMessage.username + " received rank +O.");
                            }
                            else if (splitter[3] == "-o")
                            {
                                Log(formattedMessage.username + " received rank -O.");
                            }

                        }

                        #endregion

                        // Keeps scroll bar at bottom (mostly)
                        int visible = 0;
                        BeginInvoke(new Action(() => visible = lstLog.ClientSize.Height / lstLog.ItemHeight));
                        BeginInvoke(new Action(() => lstLog.TopIndex = Math.Max(lstLog.Items.Count - visible + 1, 0)));

                    }
                }
            }
        }


        /// <summary>
        /// Checks to see if latest message was a cheat, and tries to execute it.
        /// </summary>
        public void CheatThread()
        {


            while (true)
            {
                processVC = Process.GetProcessesByName("gta-vc").FirstOrDefault();
                this.Invoke((MethodInvoker)delegate
                {
                    if (GameVersionDetector.isProcessActive(processVC))
                    {
                        chkIsActive.Enabled = true;
                        if (wasActive == true)
                        {
                            chkIsActive.Checked = true;
                            wasActive = false;
                        }
                    }
                    else if (!GameVersionDetector.isProcessActive(processVC))
                    {
                        if (chkIsActive.Checked)
                        {
                            wasActive = true;
                        }
                        chkIsActive.Enabled = false;
                        chkIsActive.Checked = false;
                    }
                });

                isActivated = chkIsActive.Checked;
                cooldown = chkEnableCooldown.Checked;

                if (isActivated && cooldownTimer <= DateTime.Now)
                {
                    if (formattedMessage.message.StartsWith("!"))
                    {
                        if (ApplicationHasFocus(processVC)) // is active window
                        {
                            foreach (var cheat in cheatsList)
                            {
                                if (formattedMessage.message.ToLower().TrimStart('!') == cheat.name.ToLower())
                                {
                                    /* This might not be necessary anymore, as it's very unlikely the user will be interrupted by it.
                                    I'm keeping it here just in case it turns out to be important anyways. (it would need some changes too, to prevent false positives). */
                                    /*  this.Invoke((MethodInvoker)delegate
                                      {
                                          while (IsAnyKeyPressed())
                                          {
                                              Log("Cheat '" + cheat.ToString().ToLower() + "' could not be entered because buttons are being pressed.");
                                              Thread.Sleep(100);
                                          }
                                      });*/

                                    if (cheat.delayCheat && delayedTimer > DateTime.Now)
                                    {
                                        Log(formattedMessage.username + " tried to enter '" + cheat.name.ToString().ToLower() + "', but needs to wait " + Math.Round((delayedTimer - DateTime.Now).TotalMinutes, 0).ToString() + " more minutes.");
                                    }
                                    else
                                    {
                                        if (cheat.delayCheat)
                                        {
                                            delayedTimer = DateTime.Now.AddMinutes(5);
                                        }

                                        if (cooldown)
                                        {
                                            cooldownTimer = DateTime.Now.AddSeconds(15);
                                        }

                                        string finalLetter = cheat.name.Substring(cheat.name.Length - 1, 1);
                                        string trimmedText = cheat.name.Remove(cheat.name.Length - 1);
                                        string reversedCheat = Reverse(trimmedText.ToUpper());

                                        // Characters should be typed in a reversed way.
                                        if (Memory.writeToMemory(processVC, 0xA10942 - GameVersionDetector.getGameOffset(processVC), Encoding.ASCII.GetBytes(reversedCheat)))
                                        {
                                            // If the cheat was written successfully it will 'type' the final letter
                                            // Cheats are being checked on key press, that's why the final letter HAS to be typed (pressed) for it to work.
                                            SendKeys.SendWait(finalLetter.ToUpper());
                                        }

                                        Log("Cheat '" + cheat.name.ToString().ToLower() + "' was entered by " + formattedMessage.username + ".");

                                    }
                                    formattedMessage.message = ""; // reset formatted message everytime, otherwise it will keep trying the same command until someone else types.
                                }

                            }
                        }
                        else
                        {
                            Log("Cheat '" + formattedMessage.message.TrimStart('!').ToString().ToLower() + "' could not be entered because process is not the active window.");
                            formattedMessage.message = ""; // reset formatted message everytime, otherwise it will keep trying the same command until someone else types.
                        }

                    }
                    else if (formattedMessage.username != null)
                    {
                        // if the typed message isn't a cheat/command
                        formattedMessage.message = ""; // reset formatted message everytime, otherwise it will keep trying the same command until someone else types.

                    }

                }
                else if (cooldown)
                {
                    int remaining = Convert.ToInt32(Math.Round((cooldownTimer - DateTime.Now).TotalSeconds * 1000, 0));
                    Thread.Sleep(remaining);
                }

                Thread.Sleep(100);

            }
        }

        public string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }


        private string StringToHex(string hexstring)
        {
            var sb = new StringBuilder();
            foreach (char t in hexstring)
                sb.Append(Convert.ToInt32(t).ToString("x") + " ");
            return sb.ToString();
        }

        static byte[] GetBytes(string str)
        {

            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            btnConnect.Enabled = false;
            channel = txtChannelToJoin.Text.ToLower();
            currentChannel = txtChannelToJoin.Text;

            mainThread = new Thread(new ThreadStart(MainThread));
            mainThread.Start();

            cheatThread = new Thread(new ThreadStart(CheatThread));
            cheatThread.Start();

            connected = true;

        }

        public void Log(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => lstLog.Items.Add("[" + time + "] " + message)));
            }

        }

        public bool IsAnyKeyPressed()
        {
            var allPossibleKeys = Enum.GetValues(typeof(Key));
            bool results = false;
            foreach (var currentKey in allPossibleKeys)
            {
                Key key = (Key)currentKey;
                if (key != Key.None)
                    if (Keyboard.IsKeyDown((Key)currentKey)) { results = true; break; }
            }
            return results;
        }


        /// <summary>
        /// Returns true if the current application has focus, false otherwise.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public bool ApplicationHasFocus(Process process)
        {
            var activatedHandle = GetForegroundWindow();
            if (activatedHandle == IntPtr.Zero)
            {
                return false;       // No window is currently activated
            }

            var procId = process.Id;
            int activeProcId;
            GetWindowThreadProcessId(activatedHandle, out activeProcId);

            return activeProcId == procId;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                Environment.Exit(Environment.ExitCode);
            }
            catch (Exception)
            { Application.Exit(); }

        }

        private void AddVCCheats()
        {
            cheatsList.Add(new Cheat("miamitraffic", false));
            cheatsList.Add(new Cheat("thugstools", false));
            cheatsList.Add(new Cheat("nuttertools", false));
            cheatsList.Add(new Cheat("professionaltools", false));
            cheatsList.Add(new Cheat("iwantitpaintedblack", false));
            cheatsList.Add(new Cheat("ahairdressercar", false));
            cheatsList.Add(new Cheat("aspirine", false));
            cheatsList.Add(new Cheat("preciousprotection", false));
            cheatsList.Add(new Cheat("icanttakeitanymore", true));
            cheatsList.Add(new Cheat("youwonttakemealive", false));
            cheatsList.Add(new Cheat("leavemealone", false));
            cheatsList.Add(new Cheat("deepfriedmarsbars", false));
            cheatsList.Add(new Cheat("programmer", false));
            cheatsList.Add(new Cheat("certaindeath", false));
            cheatsList.Add(new Cheat("stilllikedressingup", false));
            cheatsList.Add(new Cheat("cheatshavebeencracked", false));
            cheatsList.Add(new Cheat("looklikelance", false));
            cheatsList.Add(new Cheat("mysonisalawyer", false));
            cheatsList.Add(new Cheat("ilooklikehilary", false));
            cheatsList.Add(new Cheat("idonthavethemoneysonny", false));
            cheatsList.Add(new Cheat("iwantbigtits", false));
            cheatsList.Add(new Cheat("rockandrollman", false));
            cheatsList.Add(new Cheat("weloveourdick", false));
            cheatsList.Add(new Cheat("onearmedbandit", false));
            cheatsList.Add(new Cheat("foxylittlethings", false));
            cheatsList.Add(new Cheat("panzer", false));
            cheatsList.Add(new Cheat("travelinstyle", false));
            cheatsList.Add(new Cheat("gettherequickly", false));
            cheatsList.Add(new Cheat("getthereveryfastindeed", false));
            cheatsList.Add(new Cheat("getthereamazinglyfast", false));
            cheatsList.Add(new Cheat("thelastride", false));
            cheatsList.Add(new Cheat("rockandrollcar", false));
            cheatsList.Add(new Cheat("rubbishcar", false));
            cheatsList.Add(new Cheat("gettherefast", false));
            cheatsList.Add(new Cheat("betterthanwalking", false));
            cheatsList.Add(new Cheat("wheelsareallineed", false));
            cheatsList.Add(new Cheat("bigbang", true));
            cheatsList.Add(new Cheat("seaways", false));
            cheatsList.Add(new Cheat("comeflywithme", false));
            cheatsList.Add(new Cheat("hopingirl", false));
            cheatsList.Add(new Cheat("loadsoflittlethings", false));
            cheatsList.Add(new Cheat("airship", false));
            cheatsList.Add(new Cheat("gripiseverything", false));
            cheatsList.Add(new Cheat("alovelyday", false));
            cheatsList.Add(new Cheat("apleasantday", false));
            cheatsList.Add(new Cheat("abitdrieg", false));
            cheatsList.Add(new Cheat("catsanddogs", false));
            cheatsList.Add(new Cheat("cantseeathing", false));
            cheatsList.Add(new Cheat("lifeispassingmeby", false));
            cheatsList.Add(new Cheat("onspeed", false));
            cheatsList.Add(new Cheat("booooooring", false));
            cheatsList.Add(new Cheat("fightfightfight", false));
            cheatsList.Add(new Cheat("nobodylikesme", false));
            cheatsList.Add(new Cheat("chasestat", false));
            cheatsList.Add(new Cheat("ourgodgivenrighttobeararms", false));
            cheatsList.Add(new Cheat("greenlight", false));
            cheatsList.Add(new Cheat("fannymagnet", false));
            cheatsList.Add(new Cheat("chickswithguns", false));
        }

        private void chkIsActive_CheckedChanged(object sender, EventArgs e)
        {
            isActivated = this.chkIsActive.Checked;
        }

        private void chkEnableCooldown_CheckedChanged(object sender, EventArgs e)
        {
            cooldown = this.chkEnableCooldown.Checked;
        }
    }
}
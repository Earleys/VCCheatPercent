using Earlbot;
using Earlbot.BLL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VCCheatPercent
{
    public class IRC
    {
        public struct IncomingMessage
        {
            public string username;
            public string message;
        }

        private TcpClient tcpClient;
        private StreamReader inputStream;
        private StreamWriter outputStream;
        IncomingMessage formattedMessage;

        DateTime lastMessage;

        private string server = "irc.twitch.tv"; // irc.twitch.tv - 199.9.253.119
        private int port = 6667;
        private string password = "";
        public string username = "";

        public string Connect()
        {
            Configuration c = FileHandler.GetConfiguration();
            server = c.Ip;
            port = c.Port;
            password = c.Password;
            username = c.Username;

            if (c.Error != null)
            {
                return c.Error;
            }
            else {
                tcpClient = new TcpClient(server, port);
                inputStream = new StreamReader(tcpClient.GetStream());
                outputStream = new StreamWriter(tcpClient.GetStream());

                outputStream.WriteLine("PASS " + password);
                outputStream.WriteLine("NICK " + username.ToLower());
                outputStream.WriteLine("USER " + username.ToLower() + " 8 * :" + username);
                outputStream.Flush();
                return null;
            }
        }

        public void JoinChannel(string channel)
        {
                outputStream.WriteLine("JOIN #" + channel.ToLower());
                outputStream.Flush();
        }


        public void SendRawMessage(string message)
        {
            outputStream.WriteLine(message);
            outputStream.Flush();
        }

        /// <summary>
        /// Sends a chat message with a delay of 1 message every second.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        public void SendChatMessage(string channel, string message)
        {
            double timeSpan = (DateTime.UtcNow - lastMessage).Seconds;
            int timeSpanInt = Convert.ToInt32(timeSpan * 1000);
            if (timeSpanInt < 1000)
            {
                Thread.Sleep(1000 - timeSpanInt);
            }
            lastMessage = DateTime.UtcNow;

            SendRawMessage(":" + username + "!" + username + "@" + username + ".tmi.twitch.tv PRIVMSG #" + channel + " :" + message);
        }

        /// <summary>
        /// Sends a chat message immediately.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        public void SendChatMessageUrgent(string channel, string message)
        {
            SendRawMessage(":" + username + "!" + username + "@" + username + ".tmi.twitch.tv PRIVMSG #" + channel + " :" + message);
        }


        public IncomingMessage ReadMessage(string input_message)
        {
            formattedMessage.username = "";
            formattedMessage.message = "";
            string message = input_message.Remove(0, 1);
            int nicknameStartingPoint = message.IndexOf('!', 1);
            int nicknameEndingPoint = message.IndexOf('@', 1);
            int messageStartingPoint = message.IndexOf(':', 1) + 1;
            if (nicknameStartingPoint > 0 && nicknameEndingPoint > 0)
            {
                formattedMessage.username = message.Substring(nicknameStartingPoint + 1, nicknameEndingPoint - 1 - nicknameStartingPoint).Trim();
            }
            if (messageStartingPoint > 0)
            {
                formattedMessage.message = message.Substring(messageStartingPoint);
            }
            return formattedMessage;
        }

        public string readRawMessage()
        {
            string message = inputStream.ReadLine();
            return message;
        }

    }
}

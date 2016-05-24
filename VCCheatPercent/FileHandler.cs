using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Earlbot.BLL;
using System.IO;

namespace Earlbot
{
    public static class FileHandler
    {
        public static Configuration GetConfiguration()
        {
            List<string> temp = new List<string>();

            Configuration config = new Configuration();
            string invalidFields = "";

            try
            {
                string path = Path.Combine(Environment.CurrentDirectory, "config.txt");
                using (StreamReader reader = new StreamReader(path))
                {
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!line.Contains("#") && !line.Contains(" "))
                        {
                            temp.Add(line);
                        }
                    }
                }
                config.Error = null;
                for (int i = 0; i < temp.Count; i++)
                {
                    string currentLine = temp.ElementAt(i);
                    string[] splitter = currentLine.Split('=');

                    if (splitter[1] == "")
                    {
                        invalidFields += splitter[0] + ", ";
                        continue;
                    }
                    switch (splitter[0])
                    {

                        case "SERVER": config.Ip = splitter[1]; break;
                        case "PORT": config.Port = Convert.ToInt32(splitter[1]); break;
                        case "PASSWORD": config.Password = splitter[1]; break;
                        case "USERNAME": config.Username = splitter[1]; break;


                        default:
                            //config.Error = "Invalid value in config.txt!";
                            break;
                    }
                }

                if (invalidFields != "")
                {
                    config.Error = "Please check the following fields in your config file: " + invalidFields + "";
                }

            }
            catch (IOException ioe) {
                Console.WriteLine(ioe.Message);
                if (WriteConfigFile())
                {
                    config.Error = "Config file could not be found. A new file named 'config.txt' has been created. Please fill it in before trying to connect!";
                }
                else
                {
                    config.Error = "Config file could not be found. Unable to create a new config file. Please redownload this tool!";
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                config.Error = "Invalid value in config.txt!";
            }

            return config;
        }

        public static bool WriteConfigFile()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter("config.txt"))
                {
                    writer.Write(VCCheatPercent.Properties.Resources.DefaultConfigFile);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

        }

        
    }
}

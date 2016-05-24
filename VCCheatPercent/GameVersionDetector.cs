using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCSplitInfo;

namespace GTAOnMissionChanger
{
    public class GameVersionDetector
    {

        public static int getGameOffset(Process process)
        {
            // this is based on Lighnat0r's GameVersionCheck script ( https://github.com/Lighnat0r/Files/blob/master/Lib/GameVersionCheck.ahk )
            try
            {
                if (isProcessActive(process))
                {
                    if (process.ProcessName == "gta-vc")
                    {
                        byte value = (byte)Memory.getMemoryResult(process, 0x00608578, 4);

                        if (value == 0x5D)
                        {
                            return 0; ; //1.0
                        }
                        else if (value == 0x81)
                        {
                            return 0x81; // 1.1, untested
                        }
                        else if (value == 0x5B)
                        {
                            return 0x0FF8;  // steam
                        }
                        else if (value == 0x44)
                        {
                            return 0x2FF0; // japanese, 'normal' offset is -0x2FF8
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return 0;
        }


        public static bool isProcessActive(Process process)
        {
            if (process == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }


    }
}

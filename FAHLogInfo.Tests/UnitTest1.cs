using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace FAHLogInfo.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            const string time_regx = @"(?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2}):";       // 23:12:03:
            const string fsws_regx = @"WU(?<wu_slot>\d{2}):FS(?<f_slot>\d{2}):";                 // WU01:FS01:
            const string fahcore_regx = @"(?<fahcore>\S{4}):";                                  // 0x21:
            const string std_prepend = time_regx + fsws_regx + fahcore_regx;
            // (@"22:52:29:WU01:FS01:0x21:Project: 11407 (Run 3, Clone 9, Gen 217)"
            // Project: 11407 (Run 3, Clone 9, Gen 217)
            const string new_wu = @"Project: (?<project>\d+) \(Run (?<run>\d+), Clone (?<clone>\d+), Gen (?<gen>\d+)\)";
            //*********************** Log Started 2016-09-19T16:24:38Z ***********************
            const string log_start = @"[*]+ Log Started (?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})T";
        
            //*******************************Date: 2016 - 09 - 19 *******************************
            const string new_date = @"[*]+Date: (?<year>\d{4}) - (?<month>\d{2}) - (?<day>\d{2}) ";

            // Server responded WORK_ACK (400)
            const string server_responded = @"Server responded (?<response>\S+)";    // Server responded WORK_ACK (400)
                                                                                     //Final credit estimate, 50346.00 points
            const string cred = @"Final credit estimate, (?<credit>[0-9]+.[0-9]+) points";  //Final credit estimate, 50346.00 points


            Match m;

            string txt = @"22:52:29:WU01:FS01:0x21:Project: 11407 (Run 3, Clone 9, Gen 217)";
            string pattern = @"Project: (?<project>\d+) \(Run (?<run>\d+), Clone (?<clone>\d+), Gen (?<gen>\d+)\)";
            m = Regex.Match(txt, pattern);

            Console.WriteLine("m.suc = {0} Line = {1}", m.Success, txt);
            if (m.Success)
            {
                Console.WriteLine("hour = {0} f_slot = {1} fahcore = {2}", m.Groups["hour"].Value, m.Groups["f_slot"].Value, m.Groups["fahcore"].Value);
                Console.WriteLine();
            }


        }
    }
}

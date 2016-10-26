using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;


namespace FAHLogInfo
{
    public class FahLogParser
    {

        public List<FoldingSlot> FoldingSlots = new List<FoldingSlot>();
        public List<WorkUnitInfo> WorkUnits = new List<WorkUnitInfo>();
        public DateTime CurrentTime = new DateTime();
        public List<LineParser> LineParsers = new List<LineParser>();
        public List<string> UnmatchedLines = new List<string>();
        public int LineNumber = 0;
        public string CpuName;
        public string CurentFilename;


        public FahLogParser()
        {
            //               LineParsers.Add(new ParseDateChange());
            LineParsers.Add(new ParseEnableFoldingSlot());
            LineParsers.Add(new ParseNewWUProject());
            LineParsers.Add(new ParseProgress());
            LineParsers.Add(new ParseUnitID());
            LineParsers.Add(new ParseLogStarted());
            LineParsers.Add(new ParseFinalCreditEstimate());
            LineParsers.Add(new ParseBadResults());
            LineParsers.Add(new ParseCPUName());

        }

        abstract public class LineParser
        {
            // Define some common string used by nearly all implementations
            internal const string time_regx = @"(?<time>\d{2}:\d{2}:\d{2}):";               // 23:12:03:
            internal const string fsws_regx = @"WU(?<wu_slot>\d{2}):FS(?<f_slot>\d{2}):";                 // WU01:FS01:
            internal const string fahcore_regx = @"(?<fahcore>\S{4}):";                                  // 0x21:
            internal const string std_prepend = time_regx + fsws_regx + fahcore_regx;

            abstract public bool ParseLine(FahLogParser flp, string Line);
        }

        public class ParseLogStarted : LineParser
        {
            //02:11:20:WU04:FS01:0x21:*********************** Log Started 2016-03-14T02:11:20Z ***********************
            const string log_start = @"[*]+ Log Started (?<date>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})Z [*]+";

            override public bool ParseLine(FahLogParser flp, string Line)
            {
                Match m = Regex.Match(Line, std_prepend + log_start);
                if (m.Success)
                {
                    DateTime dt;
                    dt = DateTime.ParseExact(m.Groups["date"].Value, "yyyy-MM-ddTHH:mm:ss", null);
                    flp.CurrentTime = dt;
                }
                return m.Success;
            }
        }


        public class ParseEnableFoldingSlot : LineParser
        {
            // 16:24:38:Enabled folding slot 01: READY gpu:0:GM206 [GeForce GTX 950]
            // 23:46:28:Enabled folding slot 01: PAUSED gpu:1:GP104 [GeForce GTX 1070] (by user)
            const string new_slot = @"Enabled folding slot (?<f_slot>\d{2}): (READY|PAUSED) gpu:(?<cuda_slot>\d):(?<gpu_id>\S{5}) \[(?<desc>.+)\]+";

            override public bool ParseLine(FahLogParser flp, string Line)
            {
                Match m = Regex.Match(Line, time_regx + new_slot);
                if (m.Success)
                {

                    flp.NewSlot(int.Parse(m.Groups["f_slot"].Value), m.Groups["desc"].Value, m.Groups["cuda_slot"].Value);
                }
                return m.Success;
            }
        }

        public class ParseNewWUProject : LineParser
        {
            // New Project - Creates new WU for FS
            // 22:52:29:WU01:FS01:0x21:Project: 11407(Run 3, Clone 9, Gen 217)
            // Unfortunately this line occurs before the UnitID line, so can't possitively identify the unit prior.
            const string new_wu = @"Project: (?<project>\d+) \(Run (?<run>\d+), Clone (?<clone>\d+), Gen (?<gen>\d+)\)";

            override public bool ParseLine(FahLogParser flp, string Line)
            {
                Match m = Regex.Match(Line, std_prepend + new_wu);
                if (m.Success)
                {
                    flp.UpdateProject(int.Parse(m.Groups["wu_slot"].Value), int.Parse(m.Groups["f_slot"].Value), int.Parse(m.Groups["project"].Value),
                     int.Parse(m.Groups["run"].Value), int.Parse(m.Groups["clone"].Value), int.Parse(m.Groups["gen"].Value), m.Groups["fahcore"].Value);
                }
                return m.Success;
            }
        }

        public class ParseUnitID : LineParser
        {
            // 03:53:18:WU01:FS00:0x21:Unit: 0x0000000cab436c9056b4f806ba24f05e
            const string UnitID = @"Unit: 0x(?<UnitID>[0-9a-fA-F]+)";

            override public bool ParseLine(FahLogParser flp, string Line)
            {
                Match m = Regex.Match(Line, std_prepend + UnitID);
                if (m.Success)
                {
                    flp.UpdateUnitID(int.Parse(m.Groups["wu_slot"].Value), int.Parse(m.Groups["f_slot"].Value), m.Groups["UnitID"].Value);
                }
                return m.Success;
            }
        }

        public class ParseProgress : LineParser
        {
            // Parse progress status update.
            // 04:05:21:WU00:FS01:0x21:Completed 1450000 out of 5000000 steps (29%)
            const string progress = @"Completed (?<steps>\d+) out of (?<max_steps>\d+) steps.+";

            override public bool ParseLine(FahLogParser flp, string Line)
            {
                Match m = Regex.Match(Line, std_prepend + progress);
                if (m.Success)
                {
                    flp.UpdateProgress(m.Groups["time"].Value, int.Parse(m.Groups["wu_slot"].Value), int.Parse(m.Groups["f_slot"].Value), int.Parse(m.Groups["steps"].Value),
                     int.Parse(m.Groups["max_steps"].Value));
                }
                return m.Success;
            }
        }


        public class ParseFinalCreditEstimate : LineParser
        {
            //Final credit estimate, 50346.00 points
            const string cred = @"Final credit estimate, (?<credit>[0-9]+).[0-9]+ points";
            // Check end of project - only take the integer portion

            override public bool ParseLine(FahLogParser flp, string Line)
            {
                Match m = Regex.Match(Line, time_regx + fsws_regx + cred);
                if (m.Success)
                {
                    flp.EndProject(int.Parse(m.Groups["wu_slot"].Value), int.Parse(m.Groups["f_slot"].Value), m.Groups["credit"].Value);
                }
                return m.Success;
            }
        }

        public class ParseBadResults : LineParser
        {
            //02:58:29:WARNING:WU02:FS00:Server did not like results, dumping
            const string dumping = @"Server did not like results, dumping";
            override public bool ParseLine(FahLogParser flp, string Line)
            {
                Match m = Regex.Match(Line, time_regx + "WARNING:" + fsws_regx + dumping);
                if (m.Success)
                {
                    Console.WriteLine("LN:{0}:BADWU(): Server did not like results - wu_slot {1} - f_slot {2}.", flp.LineNumber, m.Groups["wu_slot"].Value, m.Groups["f_slot"].Value);
                    flp.EndProject(int.Parse(m.Groups["wu_slot"].Value), int.Parse(m.Groups["f_slot"].Value), "-1");  //TODO: cheesy for now
                }
                return m.Success;
            }
        }

        public class ParseCPUName : LineParser
        {
            //18:56:43:          CPU: Intel(R) Core(TM) i5-6500 CPU @ 3.20GHz
            const string cpu_desc_regex = @"          CPU: (?<cpu_desc>.+)";
            override public bool ParseLine(FahLogParser flp, string Line)
            {
                Match m = Regex.Match(Line, time_regx + cpu_desc_regex);
                if (m.Success)
                {
                    Console.WriteLine("LN:{0}:cpu_desc = {1}", flp.LineNumber, m.Groups["cpu_desc"].Value);
                    flp.CpuName = m.Groups["cpu_desc"].Value;
                }
                return m.Success;
            }
        }


        public void NewSlot(int foldingSlot, string desc, string cudaSlot)
        {
            Console.WriteLine("LN:{2}:NewSlot(): f_slot = {0} cuda_slot {3} desc ={1}", foldingSlot, desc, LineNumber, cudaSlot);

            FoldingSlot fs = FoldingSlots.Find(x => x.FoldingSlotNumber == foldingSlot);

            // If work unit slot exists, remove it and log an error.
            if (fs != null)
            {
                if (fs.GpuDescription.Equals(desc))  // if the descriptions are equal, then don't remove the slot, assuming we're continuing.
                    return;

                Console.WriteLine("LN:{0}:NewSlot(): Folding Slot exists-Removing f_slot = {1} new_desc = {2} old_desc = {3}", LineNumber, foldingSlot, desc, fs.GpuDescription);
                FoldingSlots.Remove(FoldingSlots.Find(x => x.FoldingSlotNumber == foldingSlot));
            }

            FoldingSlots.Add(new FoldingSlot() { FoldingSlotNumber = foldingSlot, GpuDescription = desc, CudaSlot = cudaSlot });

        }

        public void UpdateProject(int workUnitSlot, int foldingSlot, int project, int run, int clone, int gen, string fahcore)
        {
            // If fully parsing file, the folding slot should already exist.
            // If it's not there, just make one.
            if (FoldingSlots.Exists(x => x.FoldingSlotNumber == foldingSlot) == false)
            {
                Console.WriteLine("LN:{1}:UpdateProject(): Folding Slot doesn't exist so, creating one f_slot = {0} ***************", foldingSlot, LineNumber);
                FoldingSlots.Add(new FoldingSlot() { FoldingSlotNumber = foldingSlot, GpuDescription = string.Format("UpdateProject() Created with WU p:{0}.", project) });
            }

            // Add the WU to folding slot.
            FoldingSlots.Find(x => x.FoldingSlotNumber == foldingSlot).NewWU(this, workUnitSlot, project, run, clone, gen, fahcore);
        }

        public void UpdateProgress(string time, int workUnitSlot, int foldingSlot, int steps, int maxSteps)
        {
            // If fully parsing file, the folding slot should already exist.
            // If it's not there, just make one.
            if (FoldingSlots.Exists(x => x.FoldingSlotNumber == foldingSlot) == false)
            {
                FoldingSlots.Add(new FoldingSlot() { FoldingSlotNumber = foldingSlot, GpuDescription = "Unknown. Created by UpdateProgress" });
                Console.WriteLine("LN:{1}:UpdateProgress(): Folding Slot should already exist but creating one f_slot = {0} ***************", foldingSlot, LineNumber);
            }

            // Add the WU to folding slot.
            FoldingSlots.Find(x => x.FoldingSlotNumber == foldingSlot).UpdateProgress(this, time, workUnitSlot, steps, maxSteps);
        }


        public void UpdateUnitID(int workUnitSlot, int foldingSlot, string unitID)
        {
            // If fully parsing file, the folding slot should already exist.
            // If it's not there, just make one.
            if (FoldingSlots.Exists(x => x.FoldingSlotNumber == foldingSlot) == false)
            {
                Console.WriteLine("LN:{1}:UpdateUnitID(): Folding Slot should already exist but creating one f_slot = {0}", foldingSlot, LineNumber);
                FoldingSlots.Add(new FoldingSlot() { FoldingSlotNumber = foldingSlot, GpuDescription = "UpdateUnitID() - Created Folding Slot" });
            }

            // Add the WU to folding slot.
            FoldingSlots.Find(x => x.FoldingSlotNumber == foldingSlot).UpdateUnitID(this, workUnitSlot, unitID);
        }

        public void EndProject(int workUnitSlot, int foldingSlot, string credit)
        {
            // Add the WU to folding slot.
            FoldingSlot fs = FoldingSlots.Find(x => x.FoldingSlotNumber == foldingSlot);
            if (fs != null)
            {
                WorkUnitInfo wui = fs.EndWU(this, workUnitSlot, int.Parse(credit));

                if (wui != null)
                {
                    // Update the folding slot information
                    wui.FoldingSlot = foldingSlot;
                    wui.GpuDescription = fs.GpuDescription;
                    wui.CudaSlot = fs.CudaSlot;

                    WorkUnits.Add(wui);  // Save the completed wu.
                }
            }
            else
            {
                Console.WriteLine("LN:{0}:EndProject(): Can't find folding slot {1} - wu = {2} credit = {3}", LineNumber, workUnitSlot, foldingSlot, credit);
                Console.WriteLine("LN:{0}:EndProject(): This can happen early in the log file if FAH is trying to send a WU that wasn't processed at all in the log file.", LineNumber);
            }
        }

        public void EndParse()
        {
            // Closes and logs all WU's that are partially complete.

            int fs_end = FoldingSlots.Count();

            for (int j = 0; j < fs_end; j++)
            //                foreach (FoldingSlot fs in folding_slots)
            {
                int wu_end = FoldingSlots[j].WorkUnitSlots.Count();
                var fs = FoldingSlots[j];

                // Can't use foreach because we're removing work_unit_slots as we go along.
                for (int i = 0; i < wu_end; i++)
                {
                    Console.WriteLine("LN:{0}EndParse(): f_slot {1} wu_slot{2}", LineNumber, fs.FoldingSlotNumber, fs.WorkUnitSlots[0].WorkUnitSlotNumber);
                    EndProject(fs.FoldingSlotNumber, fs.WorkUnitSlots[0].WorkUnitSlotNumber, "0");
                }
            }
        }

        public bool ParseTextLine(string Line)
        {
            LineNumber++;
            foreach (LineParser Parser in LineParsers)
            {
                bool b = (Parser.ParseLine(this,Line));
                // If it line is parsed, then return true.
                if (b) return b;
            }
            // None of the parsers matched it, so it add to the unmatched list.
            UnmatchedLines.Add(Line);
            return false;
        }

        public void ShowResults(bool goodWus, bool badWus, StreamWriter outputStream)
        {
            // Write out the header line with all the property names
            outputStream.WriteLine("WUStatus,{0}", WorkUnits[0].ToStringPropertyNames());

            foreach (WorkUnitInfo wu in WorkUnits)
            {
                if (wu.Credit < 0 && badWus)
                    outputStream.WriteLine("BADWU,{0} ", wu);
                if (wu.Credit > 0 && goodWus)
                    outputStream.WriteLine("GOODWU,{0} ", wu);
                if (wu.Credit == 0 && goodWus)
                    outputStream.WriteLine("PARTWU,{0} ", wu);
            }
        }

        public void ShowUnmatchedLines()
        {
            Console.WriteLine("Unmatched lines:");
            foreach (string s in UnmatchedLines)
            {
                Console.WriteLine(s);
            }
        }
    }
}

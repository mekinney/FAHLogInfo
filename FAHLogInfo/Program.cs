using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;



//  Objective: Parse through all FAH Log files in logs directory and print the following:
//  WU info (Project: 11407 (Run 3, Clone 9, Gen 217)
//  GPU: [GeForce GTX 950]
//  WU Slot: WU01
//  Folding Slot: FS01
//  FahCore - 0x21
//  Start Time: 
//  Avg TPF (later
//  Send Time: 02:12:27:WU01:FS01:Sending unit results: id:01 state:SEND error:NO_ERROR project:11407 run:3 clone:9 gen:217 core:0x21 unit:0x0000010c8ca304f25686b25d248e6c18
//  Final Credit estimate: 02:12:40:WU01:FS01:Final credit estimate, 50346.00 points
//
//

namespace FAHLogInfo
{
    class Program
    {

        public static string current_filename;  // Super Hack
        public class FahLogParser
        {

            public static List<FoldingSlot> foldingSlots = new List<FoldingSlot>();
            public static List<WorkUnitInfo> workUnits = new List<WorkUnitInfo>();
            public static DateTime currentTime = new DateTime();
            public List<LineParser> LineParsers = new List<LineParser>();
            public List<String> UnmatchedLines = new List<String>();
            public static int LineNumber = 0;
            public static string machine_name_processed_log = System.Environment.MachineName;
            public static string cpu_from_log;


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
            // ============================================== Work Unit Information we collect =============================
            // This is the parsed output of work units to report on.
            public class WorkUnitInfo
            {
                public int project { get; set; }
                public int run { get; set; }
                public int clone { get; set; }
                public int gen { get; set; }
                public string gpuDescription { get; set; }
                public string cudaSlot { get; set; }
                public int foldingSlot { get; set; }
                public String core { get; set; }
                public DateTime start { get; set; }
                public DateTime end { get; set; }
                public TimeSpan tpf { get; set; }
                public DateTime frameTime { get; set; }
                public TimeSpan totalComputeTime { get; set; }
                public long elapsedTimePpd { get; set; }
                public long computeTimePpd { get; set; }
                public int frames { get; set; }
                public int lastStep { get; set; }
                public int credit { get; set; }
                public String UnitID { get; set; }
                public int startLine { get; set; }
                public int endLine { get; set; }
                public string logFilename { get; set; }
                public string cpuFromLog { get; set; }

                /*                public override string ToString()
                                {
                                    return string.Format("Project: {0} (run {1}, Clone {2}, gen {3}) pts = {4} tpf {5} gpu = {6} unitID = {7}",
                                        project, run, clone, gen, credit, tpf, gpu_description, UnitID);
                                }
                */
                public override string ToString()
                {
                    // https://chrisbenard.net/2009/07/23/using-reflection-to-dynamically-generate-tostring-output/
                    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy;
                    System.Reflection.PropertyInfo[] infos = this.GetType().GetProperties(flags);

                    StringBuilder sb = new StringBuilder();

                    string typeName = this.GetType().Name;
                    sb.AppendFormat("{0}", typeName);

                    //                    sb.AppendLine(string.Empty.PadRight(typeName.Length + 5, '='));

                    foreach (var info in infos)
                    {
                        object value = info.GetValue(this, null);
                        String ElementTypeName = info.GetType().Name;

                        // This is a hack because Excel doesn't like the format d.hh:mm:ss by default.
                        if (info.Name.Equals("totalComputeTime"))
                        {
                            var ts = (TimeSpan)value;

                            String s = string.Format("{0}:{1}:{2}", (int)Math.Floor(ts.TotalHours), ts.Minutes, ts.Seconds);
                            sb.AppendFormat(",{0}", s);
                        }
                        else
                        {
                            sb.AppendFormat(",{0}", value != null ? value : "null");
                        }
                    }
                    //                    sb.AppendFormat("{0}", Environment.NewLine);

                    return sb.ToString();
                }


                public String ToStringPropertyNames()
                {
                    // https://chrisbenard.net/2009/07/23/using-reflection-to-dynamically-generate-tostring-output/
                    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy;
                    System.Reflection.PropertyInfo[] infos = this.GetType().GetProperties(flags);

                    StringBuilder sb = new StringBuilder();

                    string typeName = this.GetType().Name;
                    sb.AppendFormat("{0}", typeName);

                    foreach (var info in infos)
                    {
                        object value = info.GetValue(this, null);
                        sb.AppendFormat(",{0}", info.Name);
                    }

                    return sb.ToString();
                }

            }


            abstract public class LineParser
            {
                // Define some common string used by nearly all implementations
                internal const string time_regx = @"(?<time>\d{2}:\d{2}:\d{2}):";               // 23:12:03:
                internal const string fsws_regx = @"WU(?<wu_slot>\d{2}):FS(?<f_slot>\d{2}):";                 // WU01:FS01:
                internal const string fahcore_regx = @"(?<fahcore>\S{4}):";                                  // 0x21:
                internal const string std_prepend = time_regx + fsws_regx + fahcore_regx;

                abstract public bool ParseLine(String Line);
            }

            public class ParseLogStarted : LineParser
            {
                //02:11:20:WU04:FS01:0x21:*********************** Log Started 2016-03-14T02:11:20Z ***********************
                const string log_start = @"[*]+ Log Started (?<date>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})Z [*]+";

                override public bool ParseLine(String Line)
                {
                    Match m = Regex.Match(Line, std_prepend + log_start);
                    if (m.Success)
                    {
                        DateTime dt;
                        dt = DateTime.ParseExact(m.Groups["date"].Value, "yyyy-MM-ddTHH:mm:ss", null);
                        currentTime = dt;
                    }
                    return m.Success;
                }
            }


            public class ParseEnableFoldingSlot : LineParser
            {
                // 16:24:38:Enabled folding slot 01: READY gpu:0:GM206 [GeForce GTX 950]
                // 23:46:28:Enabled folding slot 01: PAUSED gpu:1:GP104 [GeForce GTX 1070] (by user)
                //                const string new_slot = @"Enabled folding slot (?<f_slot>\d{2}): [READY|PAUSED] gpu:\n:GM\d{3} (?<desc>.+)";
                const string new_slot = @"Enabled folding slot (?<f_slot>\d{2}): (READY|PAUSED) gpu:(?<cuda_slot>\d):(?<gpu_id>\S{5}) \[(?<desc>.+)\]+";

                override public bool ParseLine(String Line)
                {

                    Match m = Regex.Match(Line, time_regx + new_slot);
                    if (m.Success)
                    {
                        
                        NewSlot(int.Parse(m.Groups["f_slot"].Value), m.Groups["desc"].Value, m.Groups["cuda_slot"].Value);
                    }
                    return m.Success;
                }
            }

            public class ParseNewWUProject : LineParser
            {
                // New Project - Creates new WU for FS
                // 22:52:29:WU01:FS01:0x21:Project: 11407(Run 3, Clone 9, Gen 217)
                // Unfortuneatly this line occurs before the UnitID line, so can't possitively identify the unit prior.
                const string new_wu = @"Project: (?<project>\d+) \(Run (?<run>\d+), Clone (?<clone>\d+), Gen (?<gen>\d+)\)";

                override public bool ParseLine(String Line)
                {
                    Match m = Regex.Match(Line, std_prepend + new_wu);
                    if (m.Success)
                    {
                        UpdateProject(int.Parse(m.Groups["wu_slot"].Value), int.Parse(m.Groups["f_slot"].Value), int.Parse(m.Groups["project"].Value),
                         int.Parse(m.Groups["run"].Value), int.Parse(m.Groups["clone"].Value), int.Parse(m.Groups["gen"].Value), m.Groups["fahcore"].Value);
                    }
                    return m.Success;
                }
            }

            public class ParseUnitID : LineParser
            {
                // 03:53:18:WU01:FS00:0x21:Unit: 0x0000000cab436c9056b4f806ba24f05e
                const string UnitID = @"Unit: 0x(?<UnitID>[0-9a-fA-F]+)";

                override public bool ParseLine(String Line)
                {
                    Match m = Regex.Match(Line, std_prepend + UnitID);
                    if (m.Success)
                    {
                        UpdateUnitID(int.Parse(m.Groups["wu_slot"].Value), int.Parse(m.Groups["f_slot"].Value), m.Groups["UnitID"].Value);
                    }
                    return m.Success;
                }
            }

            public class ParseProgress : LineParser
            {
                // Parse progress status update.
                // 04:05:21:WU00:FS01:0x21:Completed 1450000 out of 5000000 steps (29%)
                const string progress = @"Completed (?<steps>\d+) out of (?<max_steps>\d+) steps.+";

                override public bool ParseLine(String Line)
                {
                    Match m = Regex.Match(Line, std_prepend + progress);
                    if (m.Success)
                    {
                        UpdateProgress(m.Groups["time"].Value, int.Parse(m.Groups["wu_slot"].Value), int.Parse(m.Groups["f_slot"].Value), int.Parse(m.Groups["steps"].Value),
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

                override public bool ParseLine(String Line)
                {
                    Match m = Regex.Match(Line, time_regx + fsws_regx + cred);
                    if (m.Success)
                    {
                        EndProject(int.Parse(m.Groups["wu_slot"].Value), int.Parse(m.Groups["f_slot"].Value), m.Groups["credit"].Value);
                    }
                    return m.Success;
                }
            }

            public class ParseBadResults : LineParser
            {
                //02:58:29:WARNING:WU02:FS00:Server did not like results, dumping
                const string dumping = @"Server did not like results, dumping";
                override public bool ParseLine(String Line)
                {
                    Match m = Regex.Match(Line, time_regx + "WARNING:" + fsws_regx + dumping);
                    if (m.Success)
                    {
                        Console.WriteLine("LN:{0}:BADWU(): Server did not like results - wu_slot {1} - f_slot {2}.", LineNumber, m.Groups["wu_slot"].Value, m.Groups["f_slot"].Value);
                        EndProject(int.Parse(m.Groups["wu_slot"].Value), int.Parse(m.Groups["f_slot"].Value), "-1");  //TODO: cheesy for now
                    }
                    return m.Success;
                }
            }

            public class ParseCPUName : LineParser
            {
                //18:56:43:          CPU: Intel(R) Core(TM) i5-6500 CPU @ 3.20GHz
                const string cpu_desc_regex = @"          CPU: (?<cpu_desc>.+)";
                override public bool ParseLine(String Line)
                {
                    Match m = Regex.Match(Line, time_regx + cpu_desc_regex);
                    if (m.Success)
                    {
                        Console.WriteLine("LN:{0}:cpu_desc = {1}", LineNumber, m.Groups["cpu_desc"].Value);
                        cpu_from_log = m.Groups["cpu_desc"].Value;
                    }
                    return m.Success;
                }
            }

            public class WorkUnitSlot
            {
                public int workUnitSlotNumber { get; set; }

                public WorkUnitInfo wu = new WorkUnitInfo();

            }
            public class FoldingSlot
            {
                public int foldingSlotNumber;
                public string gpuDescription { get; set; }
                public string cudaSlot { get; set; }
                public List<WorkUnitSlot> workUnitSlots = new List<WorkUnitSlot>();

                public void NewWU(int wuSlot, int project, int run, int clone, int gen, String fahcore)
                {

                    WorkUnitSlot wus = workUnitSlots.Find(x => x.workUnitSlotNumber == wuSlot);
                    // If work unit slot exists, remove it and log an error.
                    // Very rarely the client ends a wu, but don't give a failed or Final credit.
                    if (wus != null)
                    {

                        //                        Console.WriteLine("LN:{0}:NewWU(): Work Unit Slot exists. wu_slot = {1} f_slot = {2} **********************", LineNumber, wu_slot, f_slot_number);

                        if ((wus.wu.project == project) && (wus.wu.run == run) && (wus.wu.clone == clone) && (wus.wu.gen == gen))
                        {
                            //                            Console.WriteLine("LN:{0}:NewWU(): Work Unit Slot exists, but matches. Leaving. wu_slot = {1} {2} {3} {4} {5} f_slot = {6}", LineNumber, wu_slot, project, run, clone, gen, f_slot_number);
                            return;
                        }

                        Console.WriteLine("LN:{0}:NewWU(): Work Unit Slot exists and doesn't match existing - Removing.wu_slot = {1} {2} {3} {4} {5} f_slot = {6}", LineNumber, wuSlot, project, run, clone, gen, foldingSlotNumber);
                        Console.WriteLine("LN:{0}:NewWU(): Work Unit Slot exists and doesn't match existing - Existing info    = {1} {2} {3} {4} {5} f_slot = {6}", LineNumber, wuSlot, wus.wu.project, wus.wu.run, wus.wu.clone, wus.wu.gen, foldingSlotNumber);

                        workUnitSlots.Remove(wus);

                    }
                    workUnitSlots.Add(new WorkUnitSlot()
                    {
                        workUnitSlotNumber = wuSlot,
                        wu = { project = project, run = run, clone = clone, gen = gen, start = currentTime, core = fahcore, startLine = LineNumber, logFilename = current_filename }
                    });
                }

                public void UpdateProgress(String time, int workUnitSlot, int step, int maxSteps)
                {

                    // Calculate seconds that have passed
                    // Add to total WU time
                    // Update Total Frames                        
                    // Update TPF

                    WorkUnitSlot wus = workUnitSlots.Find(x => x.workUnitSlotNumber == workUnitSlot);
                    // If work unit slot exists, remove it and log an error.
                    if (wus == null)
                    {
                        Console.WriteLine("LN:{0}:UpdateProgress(): Work Unit Slot doesn't exist wu_slot = {1}", LineNumber, workUnitSlot);
                        return;
                    }

                    // Convert time to time structure incorporating date
                    // Cheesy but quick to write
                    String DateString = currentTime.ToString("yyyy-MM-dd") + "T" + time;

                    DateTime dt;
                    dt = DateTime.ParseExact(DateString, "yyyy-MM-ddTHH:mm:ss", null);
                    //                    Console.Write("LN:{0}:UpdateProgress(): DateString = {1}", LineNumber, DateString);

                    if (dt < currentTime)
                    {
                        dt = dt.AddDays(1);
                    }
                    currentTime = dt;

                    if (wus.wu.lastStep == step)
                    {  // Restarting WU.
                        wus.wu.frameTime = dt;
                        return;
                    }

                    wus.wu.lastStep = step;

                    if (wus.wu.frames == 0)
                    {
                        // Restarting or first update 
                        wus.wu.totalComputeTime = TimeSpan.Zero;
                        wus.wu.frames = 1;
                        wus.wu.frameTime = dt;
                        return;

                    }

                    if ((wus.wu.frameTime == null) || (wus.wu.frameTime == DateTime.MinValue))
                    {
                        Console.WriteLine("- SHOULD NEVER HAPPEN!!!!!");
                        // Starting the WU so update the frame time
                        wus.wu.totalComputeTime = TimeSpan.Zero;
                        wus.wu.frames = 1;
                        wus.wu.frameTime = dt;
                        return;

                    }

                    TimeSpan tpf = dt - wus.wu.frameTime;
                    wus.wu.totalComputeTime += tpf;
                    wus.wu.frames++;
                    double d = wus.wu.totalComputeTime.TotalSeconds;
                    wus.wu.frameTime = dt;

                }

                public void UpdateUnitID(int workUnitSlot, String unitID)
                {
                    WorkUnitSlot wus = workUnitSlots.Find(x => x.workUnitSlotNumber == workUnitSlot);
                    // Work Unit Slot should always exist.
                    if (wus != null)
                    {
                        if (wus.wu.UnitID != null)
                        {
                            if (!wus.wu.UnitID.Equals(unitID))
                            {
                                Console.WriteLine("LN:{0}:UpdateUnitID(): Unit ID's don't match. wu_slot = {1}, New UnitID = {2} Old UnitID = {3}", LineNumber, workUnitSlot, unitID, wus.wu.UnitID);
                            }
                        }
                        wus.wu.UnitID = unitID;
                    }
                    else
                    {
                        Console.WriteLine("LN:{0}:UpdateUnitID(): Work Unit Slot doesn't exist. wu_slot = {1} UnitID = {2}", LineNumber, workUnitSlot, unitID);
                    }
                }

                public WorkUnitInfo EndWU(int workUnitSlot, int credit)
                {
                    WorkUnitSlot wu = workUnitSlots.Find(x => x.workUnitSlotNumber == workUnitSlot);

                    if (wu != null)
                    {
                        wu.wu.credit = credit;
                        wu.wu.end = currentTime;
                        wu.wu.endLine = LineNumber;
                        wu.wu.cpuFromLog = cpu_from_log;

                        if (wu.wu.frames > 1)
                            wu.wu.tpf = TimeSpan.FromTicks(wu.wu.totalComputeTime.Ticks / (wu.wu.frames - 1));

                        // Calculate PPD for each WU

                        TimeSpan oneDay = new TimeSpan(1, 0, 0, 0, 0);
                        TimeSpan delta = new TimeSpan();
                        delta = (wu.wu.end - wu.wu.start);
                        double ratio = delta.TotalSeconds / oneDay.TotalSeconds;

                        wu.wu.elapsedTimePpd = (long)(wu.wu.credit / ratio);

                        ratio = wu.wu.totalComputeTime.TotalSeconds / oneDay.TotalSeconds;
                        wu.wu.computeTimePpd = (long)(wu.wu.credit / ratio);

                        workUnitSlots.Remove(wu);
                        return wu.wu;
                    }
                    else
                    {
                        Console.WriteLine("LN:{0}:EndWU(): Can not find WU. wu_slot = {1} credit = {2}", LineNumber, workUnitSlot, credit);
                        return null;
                    }
                }

            }


            public static void NewSlot(int foldingSlot, String desc, string cudaSlot)
            {
                Console.WriteLine("LN:{2}:NewSlot(): f_slot = {0} cuda_slot {3} desc ={1}", foldingSlot, desc, LineNumber, cudaSlot);

                FoldingSlot fs = foldingSlots.Find(x => x.foldingSlotNumber == foldingSlot);

                // If work unit slot exists, remove it and log an error.
                if (fs != null)
                {
                    if (fs.gpuDescription.Equals(desc))  // if the descriptions are equal, then don't remove the slot, assuming we're continuing.
                        return;

                    Console.WriteLine("LN:{0}:NewSlot(): Folding Slot exists-Removing f_slot = {1} new_desc = {2} old_desc = {3}", LineNumber, foldingSlot, desc, fs.gpuDescription);
                    foldingSlots.Remove(foldingSlots.Find(x => x.foldingSlotNumber == foldingSlot));

                }

                foldingSlots.Add(new FoldingSlot() { foldingSlotNumber = foldingSlot, gpuDescription = desc, cudaSlot = cudaSlot });

            }

            public static void UpdateProject(int workUnitSlot, int foldingSlot, int project, int run, int clone, int gen, String fahcore)
            {
                // If fully parsing file, the folding slot should already exist.
                // If it's not there, just make one.
                if (foldingSlots.Exists(x => x.foldingSlotNumber == foldingSlot) == false)
                {
                    Console.WriteLine("LN:{1}:UpdateProject(): Folding Slot doesn't exist so, creating one f_slot = {0} ***************", foldingSlot, LineNumber);
                    foldingSlots.Add(new FoldingSlot() { foldingSlotNumber = foldingSlot, gpuDescription = String.Format("UpdateProject() Created with WU p:{0}.", project) });
                }

                // Add the WU to folding slot.
                foldingSlots.Find(x => x.foldingSlotNumber == foldingSlot).NewWU(workUnitSlot, project, run, clone, gen, fahcore);

            }

            public static void UpdateProgress(String time, int workUnitSlot, int foldingSlot, int steps, int maxSteps)
            {
                // If fully parsing file, the folding slot should already exist.
                // If it's not there, just make one.
                if (foldingSlots.Exists(x => x.foldingSlotNumber == foldingSlot) == false)
                {
                    foldingSlots.Add(new FoldingSlot() { foldingSlotNumber = foldingSlot, gpuDescription = "Unknown. Created by UpdateProgress" });
                    Console.WriteLine("LN:{1}:UpdateProgress(): Folding Slot should already exist but creating one f_slot = {0} ***************", foldingSlot, LineNumber);
                }

                // Add the WU to folding slot.
                foldingSlots.Find(x => x.foldingSlotNumber == foldingSlot).UpdateProgress(time, workUnitSlot, steps, maxSteps);

            }


            public static void UpdateUnitID(int workUnitSlot, int foldingSlot, String unitID)
            {
                // If fully parsing file, the folding slot should already exist.
                // If it's not there, just make one.
                if (foldingSlots.Exists(x => x.foldingSlotNumber == foldingSlot) == false)
                {
                    Console.WriteLine("LN:{1}:UpdateUnitID(): Folding Slot should already exist but creating one f_slot = {0}", foldingSlot, LineNumber);
                    foldingSlots.Add(new FoldingSlot() { foldingSlotNumber = foldingSlot, gpuDescription = "UpdateUnitID() - Created Folding Slot" });
                }

                // Add the WU to folding slot.
                foldingSlots.Find(x => x.foldingSlotNumber == foldingSlot).UpdateUnitID(workUnitSlot, unitID);

            }

            public static void EndProject(int workUnitSlot, int foldingSlot, string credit)
            {
                // Add the WU to folding slot.
                FoldingSlot fs = foldingSlots.Find(x => x.foldingSlotNumber == foldingSlot);
                if (fs != null)
                {
                    WorkUnitInfo wui = fs.EndWU(workUnitSlot, int.Parse(credit));

                    if (wui != null)
                    {
                        // Update the folding slot information
                        wui.foldingSlot = foldingSlot;
                        wui.gpuDescription = fs.gpuDescription;
                        wui.cudaSlot = fs.cudaSlot;

                        workUnits.Add(wui);  // Save the completed wu.
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

                int fs_end = foldingSlots.Count();

                for (int j = 0; j < fs_end; j++)
                //                foreach (FoldingSlot fs in folding_slots)
                {
                    int wu_end = foldingSlots[j].workUnitSlots.Count();
                    var fs = foldingSlots[j];

                    // Can't use foreach because we're removing work_unit_slots as we go along.
                    for (int i = 0; i < wu_end; i++)
                    {
                        Console.WriteLine("LN:{0}EndParse(): f_slot {1} wu_slot{2}", LineNumber, fs.foldingSlotNumber, fs.workUnitSlots[0].workUnitSlotNumber);
                        EndProject(fs.foldingSlotNumber, fs.workUnitSlots[0].workUnitSlotNumber, "0");
                    }
                }
            }

            public bool ParseLine(string Line)
            {
                LineNumber++;
                foreach (LineParser Parser in LineParsers)
                {
                    bool b = (Parser.ParseLine(Line));

                    if (b) return b;
                }
                // None of the parsers matched it, so it add to the unmatched list.
                UnmatchedLines.Add(Line);
                return false;

            }

            public void ShowResults(bool GoodWus, bool BadWus, StreamWriter output_stream)
            {
                output_stream.WriteLine("WUStatus,{0}", workUnits[0].ToStringPropertyNames());

                foreach (WorkUnitInfo wu in workUnits)
                {
                    if (wu.credit < 0 && BadWus)
                        output_stream.WriteLine("BADWU,{0} ", wu);
                    if (wu.credit > 0 && GoodWus)
                        output_stream.WriteLine("GOODWU,{0} ", wu);
                    if (wu.credit == 0 && GoodWus)
                        output_stream.WriteLine("PARTWU,{0} ", wu);

                }
            }

            public void ShowUnmatchedLines()
            {
                Console.WriteLine("Unmatched lines:");
                foreach (String s in UnmatchedLines)
                {
                    Console.WriteLine(s);
                }
            }

        }

        public static IEnumerable<string> ReadLines(string path)
        {
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using (StreamReader reader = File.OpenText(path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }


        static void Main(string[] args)
        {

            Console.WriteLine("FAHLogParser at your service.");

            if ((args.Count() == 1) && (args[0] == "?"))
            {
                Console.WriteLine("Parses all *.txt files in the current directory as if they were FAH log files.");
                Console.WriteLine("They are parsed in filename order which is the correct order for the logs");
                Console.WriteLine("sub-directory. The output file is formatted so it can easily be opened in");
                Console.WriteLine(" a spreadsheet and analyzed.");
                Console.WriteLine("Note: There is minimal error checking and all the console text can be ignored.");
                Console.WriteLine("");

                Console.WriteLine("Usage: FAHLogInfo ? | [outputfilename] | Console");
                Console.WriteLine("   No parameters will create a file called wus.csv with all the WU information.");
                Console.WriteLine("   ? - Gives this help");
                Console.WriteLine("   outputfilename is the name of an optional output file for the WU information.");
                Console.WriteLine("   Console will write the WU information to the Console.");

                return;
            }

            var fpl = new FahLogParser();

            var files = Directory.EnumerateFiles(@".", @"*.txt")
                     .OrderBy(filename => filename).ToArray(); ;


 //           List < String > filelist = new List<String>();

//            foreach (string file in files)
//               filelist.Add(file);
//            filelist.Add(@"../log.txt");

            foreach (string file in files)
            {
                Console.WriteLine("Processing file: {0}\n", file);
                current_filename = file;
                FahLogParser.LineNumber = 0;  // super hack

                foreach (string line in ReadLines(file))
                {
                    fpl.ParseLine(line);
                }
            }

            fpl.EndParse();

            StreamWriter sw;

            String outputfilename;

            if (args.Count() != 0 && args[0] == "Console")
            {
                using (sw = new StreamWriter(Console.OpenStandardOutput()))
                {
                    sw.AutoFlush = true;
                    Console.SetOut(sw);
                    Console.WriteLine("Sending results to the console.");
                    fpl.ShowResults(true, true, sw);
                }
            }

            else
            {
                if (args.Count() == 0)
                {
                    outputfilename = "wus.csv";
                }
                else
                {
                    outputfilename = args[0];
                }

                try
                {
                    using (sw = new StreamWriter(outputfilename))
                    {
                        Console.WriteLine("Writing results to {0}", outputfilename);
                        fpl.ShowResults(true, true, sw);
                    }
                }
                catch (IOException ioex)
                {
                    Console.WriteLine("Cannot open output file {0} - it may be in use.", outputfilename);
                    Console.WriteLine("{0}", ioex.Message);

                }
            }
        }

    }
}


/*            public override string ToString()
            {
                // https://chrisbenard.net/2009/07/23/using-reflection-to-dynamically-generate-tostring-output/
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy;
                System.Reflection.PropertyInfo[] infos = this.GetType().GetProperties(flags);

                StringBuilder sb = new StringBuilder();

                string typeName = this.GetType().Name;
                sb.AppendLine(typeName);
                sb.AppendLine(string.Empty.PadRight(typeName.Length + 5, '='));

                foreach (var info in infos)
                {
                    object value = info.GetValue(this, null);
                    sb.AppendFormat("{0}: {1}{2}", info.Name, value != null ? value : "null", Environment.NewLine);
                }

                return sb.ToString();
            }
            */

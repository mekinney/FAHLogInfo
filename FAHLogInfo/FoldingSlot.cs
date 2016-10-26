using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FAHLogInfo;



namespace FAHLogInfo
{
    public class FoldingSlot
    {
        public int foldingSlotNumber;
        public string gpuDescription { get; set; }
        public string cudaSlot { get; set; }
        public List<WorkUnitSlot> workUnitSlots = new List<WorkUnitSlot>();

        public void NewWU(FahLogParser flp, int wuSlot, int project, int run, int clone, int gen, string fahcore)
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

                Console.WriteLine("LN:{0}:NewWU(): Work Unit Slot exists and doesn't match existing - Removing.wu_slot = {1} {2} {3} {4} {5} f_slot = {6}", flp.lineNumber, wuSlot, project, run, clone, gen, foldingSlotNumber);
                Console.WriteLine("LN:{0}:NewWU(): Work Unit Slot exists and doesn't match existing - Existing info    = {1} {2} {3} {4} {5} f_slot = {6}", flp.lineNumber, wuSlot, wus.wu.project, wus.wu.run, wus.wu.clone, wus.wu.gen, foldingSlotNumber);

                workUnitSlots.Remove(wus);

            }
            workUnitSlots.Add(new WorkUnitSlot()
            {
                workUnitSlotNumber = wuSlot,
                wu = { project = project, run = run, clone = clone, gen = gen, start = flp.currentTime, core = fahcore,
                        startLine = flp.lineNumber, logFilename = flp.currentFilename }
            });
        }

        public void UpdateProgress(FahLogParser flp, string time, int workUnitSlot, int step, int maxSteps)
        {

            // Calculate seconds that have passed
            // Add to total WU time
            // Update Total Frames                        
            // Update TPF

            WorkUnitSlot wus = workUnitSlots.Find(x => x.workUnitSlotNumber == workUnitSlot);
            // If work unit slot exists, remove it and log an error.
            if (wus == null)
            {
                Console.WriteLine("LN:{0}:UpdateProgress(): Work Unit Slot doesn't exist wu_slot = {1}", flp.lineNumber, workUnitSlot);
                return;
            }

            // Convert time to time structure incorporating date
            // Cheesy but quick to write
            string DateString = flp.currentTime.ToString("yyyy-MM-dd") + "T" + time;

            DateTime dt;
            dt = DateTime.ParseExact(DateString, "yyyy-MM-ddTHH:mm:ss", null);
            //                    Console.Write("LN:{0}:UpdateProgress(): DateString = {1}", LineNumber, DateString);

            if (dt < flp.currentTime)
            {
                dt = dt.AddDays(1);
            }
            flp.currentTime = dt;

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

        public void UpdateUnitID(FahLogParser flp, int workUnitSlot, string unitID)
        {
            WorkUnitSlot wus = workUnitSlots.Find(x => x.workUnitSlotNumber == workUnitSlot);
            // Work Unit Slot should always exist.
            if (wus != null)
            {
                if (wus.wu.UnitID != null)
                {
                    if (!wus.wu.UnitID.Equals(unitID))
                    {
                        Console.WriteLine("LN:{0}:UpdateUnitID(): Unit ID's don't match. wu_slot = {1}, New UnitID = {2} Old UnitID = {3}", flp.lineNumber, workUnitSlot, unitID, wus.wu.UnitID);
                    }
                }
                wus.wu.UnitID = unitID;
            }
            else
            {
                Console.WriteLine("LN:{0}:UpdateUnitID(): Work Unit Slot doesn't exist. wu_slot = {1} UnitID = {2}", flp.lineNumber, workUnitSlot, unitID);
            }
        }

        public WorkUnitInfo EndWU(FahLogParser flp, int workUnitSlot, int credit)
        {
            WorkUnitSlot wu = workUnitSlots.Find(x => x.workUnitSlotNumber == workUnitSlot);

            if (wu != null)
            {
                wu.wu.credit = credit;
                wu.wu.end = flp.currentTime;
                wu.wu.endLine = flp.lineNumber;
                wu.wu.cpuFromLog = flp.cpu_from_log;

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
                Console.WriteLine("LN:{0}:EndWU(): Can not find WU. wu_slot = {1} credit = {2}", flp.lineNumber, workUnitSlot, credit);
                return null;
            }
        }

    }

}

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

        public void NewWU(FahLogParser flp, int workUnitSlotNumber, int project, int run, int clone, int gen, string fahcore)
        {

            WorkUnitSlot workUnitSlot = workUnitSlots.Find(x => x.workUnitSlotNumber == workUnitSlotNumber);
            // If work unit slot exists, remove it and log an error.
            // Very rarely the client ends a wu, but don't give a failed or Final credit.
            if (workUnitSlot != null)
            {

                //                        Console.WriteLine("LN:{0}:NewWU(): Work Unit Slot exists. wu_slot = {1} f_slot = {2} **********************", LineNumber, wu_slot, f_slot_number);

                if ((workUnitSlot.wu.project == project) && (workUnitSlot.wu.run == run) && (workUnitSlot.wu.clone == clone) && (workUnitSlot.wu.gen == gen))
                {
                    //                            Console.WriteLine("LN:{0}:NewWU(): Work Unit Slot exists, but matches. Leaving. wu_slot = {1} {2} {3} {4} {5} f_slot = {6}", LineNumber, wu_slot, project, run, clone, gen, f_slot_number);
                    return;
                }

                Console.WriteLine("LN:{0}:NewWU(): Work Unit Slot exists and doesn't match existing - Removing.wu_slot = {1} {2} {3} {4} {5} f_slot = {6}", flp.lineNumber, workUnitSlotNumber, project, run, clone, gen, foldingSlotNumber);
                Console.WriteLine("LN:{0}:NewWU(): Work Unit Slot exists and doesn't match existing - Existing info    = {1} {2} {3} {4} {5} f_slot = {6}", flp.lineNumber, workUnitSlotNumber, workUnitSlot.wu.project, workUnitSlot.wu.run, workUnitSlot.wu.clone, workUnitSlot.wu.gen, foldingSlotNumber);

                workUnitSlots.Remove(workUnitSlot);

            }
            workUnitSlots.Add(new WorkUnitSlot()
            {
                workUnitSlotNumber = workUnitSlotNumber,
                wu = { project = project, run = run, clone = clone, gen = gen, start = flp.currentTime, core = fahcore,
                        startLine = flp.lineNumber, startLogFilename = flp.currentFilename }
            });
        }

        public void UpdateProgress(FahLogParser flp, string time, int workUnitSlotNumber, int step, int maxSteps)
        {

            // Calculate seconds that have passed
            // Add to total WU time
            // Update Total Frames                        
            // Update TPF

            WorkUnitSlot workUnitSlot = workUnitSlots.Find(x => x.workUnitSlotNumber == workUnitSlotNumber);
            // If work unit slot exists, remove it and log an error.
            if (workUnitSlot == null)
            {
                Console.WriteLine("LN:{0}:UpdateProgress(): Work Unit Slot doesn't exist wu_slot = {1}", flp.lineNumber, workUnitSlotNumber);
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

            if (workUnitSlot.wu.lastStep == step)
            {  // Restarting WU.
                workUnitSlot.wu.frameTime = dt;
                return;
            }

            workUnitSlot.wu.lastStep = step;

            if (workUnitSlot.wu.frames == 0)
            {
                // Restarting or first update 
                workUnitSlot.wu.totalComputeTime = TimeSpan.Zero;
                workUnitSlot.wu.frames = 1;
                workUnitSlot.wu.frameTime = dt;
                return;

            }

            if ((workUnitSlot.wu.frameTime == null) || (workUnitSlot.wu.frameTime == DateTime.MinValue))
            {
                Console.WriteLine("- SHOULD NEVER HAPPEN!!!!!");
                // Starting the WU so update the frame time
                workUnitSlot.wu.totalComputeTime = TimeSpan.Zero;
                workUnitSlot.wu.frames = 1;
                workUnitSlot.wu.frameTime = dt;
                return;

            }

            TimeSpan tpf = dt - workUnitSlot.wu.frameTime;
            workUnitSlot.wu.totalComputeTime += tpf;
            workUnitSlot.wu.frames++;
            double d = workUnitSlot.wu.totalComputeTime.TotalSeconds;
            workUnitSlot.wu.frameTime = dt;

        }

        public void UpdateUnitID(FahLogParser flp, int workUnitSlotNumber, string unitID)
        {
            WorkUnitSlot workUnitSlot = workUnitSlots.Find(x => x.workUnitSlotNumber == workUnitSlotNumber);
            // Work Unit Slot should always exist.
            if (workUnitSlot != null)
            {
                if (workUnitSlot.wu.UnitID != null)
                {
                    if (!workUnitSlot.wu.UnitID.Equals(unitID))
                    {
                        Console.WriteLine("LN:{0}:UpdateUnitID(): Unit ID's don't match. wu_slot = {1}, New UnitID = {2} Old UnitID = {3}", flp.lineNumber, workUnitSlotNumber, unitID, workUnitSlot.wu.UnitID);
                    }
                }
                workUnitSlot.wu.UnitID = unitID;
            }
            else
            {
                Console.WriteLine("LN:{0}:UpdateUnitID(): Work Unit Slot doesn't exist. wu_slot = {1} UnitID = {2}", flp.lineNumber, workUnitSlotNumber, unitID);
            }
        }

        public WorkUnitInfo EndWU(FahLogParser flp, int workUnitSlotNumber, int credit)
        {
            WorkUnitSlot workUnitSlot = workUnitSlots.Find(x => x.workUnitSlotNumber == workUnitSlotNumber);

            if (workUnitSlot != null)
            {
                workUnitSlot.wu.credit = credit;
                workUnitSlot.wu.end = flp.currentTime;
                workUnitSlot.wu.endLine = flp.lineNumber;
                workUnitSlot.wu.endLogFilename = flp.currentFilename;
                workUnitSlot.wu.cpuFromLog = flp.cpu_from_log;

                if (workUnitSlot.wu.frames > 1)
                    workUnitSlot.wu.tpf = TimeSpan.FromTicks(workUnitSlot.wu.totalComputeTime.Ticks / (workUnitSlot.wu.frames - 1));

                // Calculate PPD for each WU

                TimeSpan oneDay = new TimeSpan(1, 0, 0, 0, 0);
                TimeSpan delta = new TimeSpan();
                delta = (workUnitSlot.wu.end - workUnitSlot.wu.start);
                double ratio = delta.TotalSeconds / oneDay.TotalSeconds;

                workUnitSlot.wu.elapsedTimePpd = (long)(workUnitSlot.wu.credit / ratio);

                ratio = workUnitSlot.wu.totalComputeTime.TotalSeconds / oneDay.TotalSeconds;
                workUnitSlot.wu.computeTimePpd = (long)(workUnitSlot.wu.credit / ratio);

                workUnitSlots.Remove(workUnitSlot);
                return workUnitSlot.wu;
            }
            else
            {
                Console.WriteLine("LN:{0}:EndWU(): Can not find WU. wu_slot = {1} credit = {2}", flp.lineNumber, workUnitSlotNumber, credit);
                return null;
            }
        }

    }

}

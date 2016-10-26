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
        public int FoldingSlotNumber;
        public string GpuDescription { get; set; }
        public string CudaSlot { get; set; }
        public List<WorkUnitSlot> WorkUnitSlots = new List<WorkUnitSlot>();

        public void NewWU(FahLogParser flp, int workUnitSlotNumber, int project, int run, int clone, int gen, string fahcore)
        {

            WorkUnitSlot workUnitSlot = WorkUnitSlots.Find(x => x.WorkUnitSlotNumber == workUnitSlotNumber);
            // If work unit slot exists, remove it and log an error.
            // Very rarely the client ends a wu, but don't give a failed or Final credit.
            if (workUnitSlot != null)
            {

                // Console.WriteLine("LN:{0}:NewWU(): Work Unit Slot exists. wu_slot = {1} f_slot = {2} **********************", LineNumber, wu_slot, f_slot_number);

                if ((workUnitSlot.wu.Project == project) && (workUnitSlot.wu.Run == run) && (workUnitSlot.wu.Clone == clone) && (workUnitSlot.wu.Gen == gen))
                {
                    // Console.WriteLine("LN:{0}:NewWU(): Work Unit Slot exists, but matches. Leaving. wu_slot = {1} {2} {3} {4} {5} f_slot = {6}", LineNumber, wu_slot, project, run, clone, gen, f_slot_number);
                    return;
                }

                Console.WriteLine("LN:{0}:NewWU(): Work Unit Slot exists and doesn't match existing - Removing.wu_slot = {1} {2} {3} {4} {5} f_slot = {6}", flp.LineNumber, workUnitSlotNumber, project, run, clone, gen, FoldingSlotNumber);
                Console.WriteLine("LN:{0}:NewWU(): Work Unit Slot exists and doesn't match existing - Existing info    = {1} {2} {3} {4} {5} f_slot = {6}", flp.LineNumber, workUnitSlotNumber, workUnitSlot.wu.Project, workUnitSlot.wu.Run, workUnitSlot.wu.Clone, workUnitSlot.wu.Gen, FoldingSlotNumber);

                WorkUnitSlots.Remove(workUnitSlot);

            }
            WorkUnitSlots.Add(new WorkUnitSlot()
            {
                WorkUnitSlotNumber = workUnitSlotNumber,
                wu = { Project = project, Run = run, Clone = clone, Gen = gen, Start = flp.CurrentTime, Core = fahcore,
                        StartLine = flp.LineNumber, StartLogFilename = flp.CurentFilename }
            });
        }

        public void UpdateProgress(FahLogParser flp, string time, int workUnitSlotNumber, int step, int maxSteps)
        {

            // Calculate seconds that have passed
            // Add to total WU time
            // Update Total Frames                        
            // Update TPF

            WorkUnitSlot workUnitSlot = WorkUnitSlots.Find(x => x.WorkUnitSlotNumber == workUnitSlotNumber);
            // If work unit slot exists, remove it and log an error.
            if (workUnitSlot == null)
            {
                Console.WriteLine("LN:{0}:UpdateProgress(): Work Unit Slot doesn't exist wu_slot = {1}", flp.LineNumber, workUnitSlotNumber);
                return;
            }

            // Convert time to time structure incorporating date
            // Cheesy but quick to write
            string DateString = flp.CurrentTime.ToString("yyyy-MM-dd") + "T" + time;

            DateTime dt;
            dt = DateTime.ParseExact(DateString, "yyyy-MM-ddTHH:mm:ss", null);
            //                    Console.Write("LN:{0}:UpdateProgress(): DateString = {1}", LineNumber, DateString);

            if (dt < flp.CurrentTime)
            {
                dt = dt.AddDays(1);
            }
            flp.CurrentTime = dt;

            if (workUnitSlot.wu.LastStep == step)
            {  // Restarting WU.
                workUnitSlot.wu.FrameTime = dt;
                return;
            }

            workUnitSlot.wu.LastStep = step;

            if (workUnitSlot.wu.Frames == 0)
            {
                // Restarting or first update 
                workUnitSlot.wu.TotalComputeTime = TimeSpan.Zero;
                workUnitSlot.wu.Frames = 1;
                workUnitSlot.wu.FrameTime = dt;
                return;
            }

            if ((workUnitSlot.wu.FrameTime == null) || (workUnitSlot.wu.FrameTime == DateTime.MinValue))
            {
                Console.WriteLine("- SHOULD NEVER HAPPEN!!!!!");
                // Starting the WU so update the frame time
                workUnitSlot.wu.TotalComputeTime = TimeSpan.Zero;
                workUnitSlot.wu.Frames = 1;
                workUnitSlot.wu.FrameTime = dt;
                return;
            }

            TimeSpan tpf = dt - workUnitSlot.wu.FrameTime;
            workUnitSlot.wu.TotalComputeTime += tpf;
            workUnitSlot.wu.Frames++;
            double d = workUnitSlot.wu.TotalComputeTime.TotalSeconds;
            workUnitSlot.wu.FrameTime = dt;
        }

        public void UpdateUnitID(FahLogParser flp, int workUnitSlotNumber, string unitID)
        {
            WorkUnitSlot workUnitSlot = WorkUnitSlots.Find(x => x.WorkUnitSlotNumber == workUnitSlotNumber);
            // Work Unit Slot should always exist.
            if (workUnitSlot != null)
            {
                if (workUnitSlot.wu.UnitID != null)
                {
                    if (!workUnitSlot.wu.UnitID.Equals(unitID))
                    {
                        Console.WriteLine("LN:{0}:UpdateUnitID(): Unit ID's don't match. wu_slot = {1}, New UnitID = {2} Old UnitID = {3}", flp.LineNumber, workUnitSlotNumber, unitID, workUnitSlot.wu.UnitID);
                    }
                }
                workUnitSlot.wu.UnitID = unitID;
            }
            else
            {
                Console.WriteLine("LN:{0}:UpdateUnitID(): Work Unit Slot doesn't exist. wu_slot = {1} UnitID = {2}", flp.LineNumber, workUnitSlotNumber, unitID);
            }
        }

        public WorkUnitInfo EndWU(FahLogParser flp, int workUnitSlotNumber, int credit)
        {
            WorkUnitSlot workUnitSlot = WorkUnitSlots.Find(x => x.WorkUnitSlotNumber == workUnitSlotNumber);

            if (workUnitSlot != null)
            {
                workUnitSlot.wu.Credit = credit;
                workUnitSlot.wu.End = flp.CurrentTime;
                workUnitSlot.wu.EndLine = flp.LineNumber;
                workUnitSlot.wu.EndLogFilename = flp.CurentFilename;
                workUnitSlot.wu.CpuName = flp.CpuName;

                if (workUnitSlot.wu.Frames > 1)
                    workUnitSlot.wu.TimePerFrame = TimeSpan.FromTicks(workUnitSlot.wu.TotalComputeTime.Ticks / (workUnitSlot.wu.Frames - 1));

                // Calculate PPD for each WU

                TimeSpan oneDay = new TimeSpan(1, 0, 0, 0, 0);
                TimeSpan delta = new TimeSpan();
                delta = (workUnitSlot.wu.End - workUnitSlot.wu.Start);
                double ratio = delta.TotalSeconds / oneDay.TotalSeconds;

                workUnitSlot.wu.ElapsedTimePpd = (long)(workUnitSlot.wu.Credit / ratio);

                ratio = workUnitSlot.wu.TotalComputeTime.TotalSeconds / oneDay.TotalSeconds;
                workUnitSlot.wu.ComputeTimePpd = (long)(workUnitSlot.wu.Credit / ratio);

                WorkUnitSlots.Remove(workUnitSlot);
                return workUnitSlot.wu;
            }
            else
            {
                Console.WriteLine("LN:{0}:EndWU(): Can not find WU. wu_slot = {1} credit = {2}", flp.LineNumber, workUnitSlotNumber, credit);
                return null;
            }
        }
    }
}

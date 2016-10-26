using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAHLogInfo
{
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
        public string core { get; set; }
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
        public string UnitID { get; set; }
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
                string ElementTypeName = info.GetType().Name;

                // This is a hack because Excel doesn't like the format d.hh:mm:ss by default.
                if (info.Name.Equals("totalComputeTime"))
                {
                    var ts = (TimeSpan)value;

                    string s = string.Format("{0}:{1}:{2}", (int)Math.Floor(ts.TotalHours), ts.Minutes, ts.Seconds);
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


        public string ToStringPropertyNames()
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


}

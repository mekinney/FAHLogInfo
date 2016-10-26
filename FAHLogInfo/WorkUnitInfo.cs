using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAHLogInfo
{
    // ============================================== Work Unit Information we collect =============================
    // These are the all properties we collect and report on as part of a work unit.
    
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
        public string startLogFilename { get; set; }
        public int endLine { get; set; }
        public string endLogFilename { get; set; }
        public string cpuFromLog { get; set; }

        // Produces comma delimited string containing all properties in the class.
        // Derived from: https://chrisbenard.net/2009/07/23/using-reflection-to-dynamically-generate-tostring-output/

        public override string ToString()
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy;
            System.Reflection.PropertyInfo[] infos = this.GetType().GetProperties(flags);

            StringBuilder sb = new StringBuilder();

            string typeName = this.GetType().Name;
            sb.AppendFormat("{0}", typeName);

            foreach (var info in infos)
            {
                object value = info.GetValue(this, null);
                string ElementTypeName = info.GetType().Name;

                // HACK: Excel doesn't like the format d.hh:mm:ss by default.
                // HACK: If the name of the totalComputeTime field is changed, this breaks.
                // TODO: Is there a way to change the formatter for DateTime types?

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

            return sb.ToString();
        }

        // Generates a single comma delimited string of the all the property names.
        // Useful as a header for a CSV file that is read by Excel.
        public string ToStringPropertyNames()
        {
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

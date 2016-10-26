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
        public int Project { get; set; }
        public int Run { get; set; }
        public int Clone { get; set; }
        public int Gen { get; set; }
        public string GpuDescription { get; set; }
        public string CudaSlot { get; set; }
        public int FoldingSlot { get; set; }
        public string Core { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public TimeSpan TimePerFrame { get; set; }
        public DateTime FrameTime { get; set; }
        public TimeSpan TotalComputeTime { get; set; }
        public long ElapsedTimePpd { get; set; }
        public long ComputeTimePpd { get; set; }
        public int Frames { get; set; }
        public int LastStep { get; set; }
        public int Credit { get; set; }
        public string UnitID { get; set; }
        public int StartLine { get; set; }
        public string StartLogFilename { get; set; }
        public int EndLine { get; set; }
        public string EndLogFilename { get; set; }
        public string CpuName { get; set; }

        // Produces comma delimited string containing all properties in the class.
        // Derived from: https://chrisbenard.net/2009/07/23/using-reflection-to-dynamically-generate-tostring-output/

        public override string ToString()
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy;
            System.Reflection.PropertyInfo[] propertyInfoList = this.GetType().GetProperties(flags);

            StringBuilder sb = new StringBuilder();

            string typeName = this.GetType().Name;
            sb.AppendFormat("{0}", typeName);

            foreach (var propertyInfo in propertyInfoList)
            {
                object value = propertyInfo.GetValue(this, null);
                string ElementTypeName = propertyInfo.GetType().Name;

                // HACK: Excel doesn't like the format d.hh:mm:ss by default.
                // HACK: If the name of the totalComputeTime field is changed, this breaks.
                // TODO: Is there a way to change the formatter for DateTime types?

                if (propertyInfo.Name.Equals("totalComputeTime"))
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
            System.Reflection.PropertyInfo[] propertyInfoList = this.GetType().GetProperties(flags);

            StringBuilder sb = new StringBuilder();

            string typeName = this.GetType().Name;
            sb.AppendFormat("{0}", typeName);

            foreach (var propertyInfo in propertyInfoList)
            {
                object value = propertyInfo.GetValue(this, null);
                sb.AppendFormat(",{0}", propertyInfo.Name);
            }
            return sb.ToString();
        }
    }


}

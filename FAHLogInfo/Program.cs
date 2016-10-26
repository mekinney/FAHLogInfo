using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using FAHLogInfo;



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


            // Add ability to also process the log.txt in the parent directory.
            // Would need to change the technique used to open the file to be shared read-only.

            //  files.Add(@"../log.txt");

            foreach (string file in files)
            {
                Console.WriteLine("Processing file: {0}\n", file);
                fpl.currentFilename = file;
                fpl.lineNumber = 0;  // super hack

                foreach (string line in ReadLines(file))
                {
                    fpl.ParseTextLine(line);
                }
            }

            fpl.EndParse();

            StreamWriter sw;

            string outputfilename;

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

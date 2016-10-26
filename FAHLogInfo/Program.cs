using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using FAHLogInfo;

// Objective: Parse through all FAH Log files in logs directory and collect useful
// information about the work units processed and write that to a CSV file.

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


            if ((args.Count() == 1) && (args[0] == "?"))
            {
                Console.WriteLine("");
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

            // It's important to sort the filenames, because work units can span log files
            // so they must be processed in chronological order - which is how the files
            // are named.

            var files = Directory.EnumerateFiles(@".", @"*.txt")
                     .OrderBy(filename => filename).ToArray(); ;

            // Add ability to also process the log.txt in the parent directory.
            // Would need to change the technique used to open the file to be shared read-only.

            //  files.Add(@"../log.txt");

            foreach (string file in files)
            {
                Console.WriteLine("Processing file: {0}\n", file);
                fpl.CurentFilename = file;
                fpl.LineNumber = 0;  // super hack

                foreach (string line in ReadLines(file))
                {
                    fpl.ParseTextLine(line);
                }
            }

            fpl.EndParse();

            StreamWriter sw;

            string outputFilename;

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
                    outputFilename = "wus.csv";
                }
                else
                {
                    outputFilename = args[0];
                }

                try
                {
                    using (sw = new StreamWriter(outputFilename))
                    {
                        Console.WriteLine("Writing results to {0}", outputFilename);
                        fpl.ShowResults(true, true, sw);
                    }
                }
                catch (IOException ioex)
                {
                    Console.WriteLine("Cannot open output file {0} - it may be in use.", outputFilename);
                    Console.WriteLine("{0}", ioex.Message);

                }
            }
        }
    }
}
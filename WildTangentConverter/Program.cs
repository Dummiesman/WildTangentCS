using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using WildTangent;

namespace WildTangentConverter
{
    class Program
    {

        static void Help()
        {
            Console.WriteLine("MDL/SCN to OBJ converter, for WildTangent Game Studios games.");
            Console.WriteLine("------------------------------");
            Console.WriteLine("Usage: wt2obj <file> -arg1 -arg2 ...");
            Console.WriteLine();
            Console.WriteLine("Valid Arguments:");
            Console.WriteLine("-absolutepaths");
            Console.WriteLine("\tExports materials with absolute image paths.");
            Console.WriteLine("-abspaths");
            Console.WriteLine("\tSame as above.");
            Console.WriteLine("-dontquit");
            Console.WriteLine("\tRequire pressing enter to exit the application after conversion.");
            Console.WriteLine("-scale <scale>");
            Console.WriteLine("\tSets export scale, default 0.01.");
            Console.WriteLine("-uv2");
            Console.WriteLine("\tIn Wavefront OBJ mode, this forces the export of the 2nd uv channel only. In COLLADA mode, this does nothing.");
            Console.WriteLine("-obj");
            Console.WriteLine("\tExport as Wavefront OBJ/MTL. Helpers, lights, and vertex colors are unable to be exported in this format.");
            Console.WriteLine("-dae");
            Console.WriteLine("\tExport as COLLADA DAE (default). DAE supports object transforms, helpers, lights, and vertex colors.");
            Console.WriteLine("-help");
            Console.WriteLine("\tShows this screen.");
            Console.Read();
        }

        static string ToStringHandleNull(object input)
        {
            if (input == null)
                return "null";
            return input.ToString();
        }

        static void Main(string[] args)
        {
            Console.Title = "WildTangent MDL/SCN Converter";

            //deal with args
            if (args.Length == 0 || args.Contains("-help"))
            {
                Help();
                return;
            }

            string inFilePath = args[0];
            if (!System.IO.File.Exists(inFilePath))
            {
                Console.WriteLine($"Error: input file '{System.IO.Path.GetFileName(inFilePath)}' doesn't exist.");
                Console.Read();
                return;
            }

            bool exportAsDae = true;
            bool exportAsObj = args.Contains("-obj");
            exportAsDae = !exportAsObj;

            bool stopAtEnd = args.Contains("-dontquit");
            bool useAbsolutePaths = args.Contains("-abspaths") || args.Contains("-absolutepaths");
            float scale = 0.01f;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-scale")
                {
                    string scaleArg = args[i + 1];
                    float.TryParse(scaleArg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out scale);
                }
            }

            //load model
            var model = WTModel.ReadBinary(inFilePath);
            foreach (var mtl in model.Materials)
            {
                Console.WriteLine(mtl.Value.Name);
                mtl.Value.ListSlots(model);
            }
            //process
            string outFileName = System.IO.Path.GetFileNameWithoutExtension(inFilePath);
            string sourcePath = System.IO.Path.GetDirectoryName(inFilePath);
            Console.WriteLine($"Converting {outFileName}...");

            if (exportAsDae)
            {
                var converter = new DAEExporter(model, useAbsolutePaths)
                {
                    SourcePath = sourcePath,
                    Scale = Vector3.One * scale
                };
                converter.Scale.X *= -1f;
                converter.Export($"{outFileName}.dae");
            }
            else if (exportAsObj)
            {
                var converter = new OBJExporter(model, useAbsolutePaths, args.Contains("-uv2"))
                {
                    SourcePath = sourcePath,
                    Scale = Vector3.One * scale
                };
                converter.Export($"{outFileName}.obj");
            }

            Console.WriteLine("Conversion complete!");
            if (stopAtEnd)
                Console.Read();
        }
    }

}

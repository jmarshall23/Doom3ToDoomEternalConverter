using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

class Doom3ToDoomEternalConverter
{
    private const double DOOM3_TO_DOOMETERNAL_SCALE = 1.325 / 52.4934;
    private const double DOOM3_TO_DOOMETERNAL_SCALETEX = 52.4934 / 1.325;

    static void Main(string[] args)
    {
        // Check if exactly one argument (the map file path) is provided
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: Doom3ToDoomEternalConverter <input_map_file>");
            return;
        }

        string inputFilePath = args[0];

        // Ensure the input file has a .map extension
        if (Path.GetExtension(inputFilePath).ToLower() != ".map")
        {
            Console.WriteLine("Error: Input file must have a .map extension.");
            return;
        }

        // Create the output file path by appending _converted before the .map extension
        string outputFilePath = Path.Combine(
            Path.GetDirectoryName(inputFilePath),
            Path.GetFileNameWithoutExtension(inputFilePath) + "_converted.map"
        );

        List<string> brushes = ConvertMapFile(inputFilePath);
        SaveAsDoomEternalFormat(outputFilePath, brushes);

        Console.WriteLine($"Map file {outputFilePath} has been created with adjusted brushDef3 in Doom Eternal format.");
    }

    static List<string> ConvertMapFile(string inputFilePath)
    {
        string[] lines = File.ReadAllLines(inputFilePath);
        List<string> brushes = new List<string>();
        bool insideBrushDef3 = false;
        string currentBrush = "";

        foreach (string line in lines)
        {
            if (line.Trim().StartsWith("brushDef3"))
            {
                insideBrushDef3 = true;
                currentBrush = "";  // Reset current brush, but do not add brushDef3 line again
            }
            else if (insideBrushDef3 && line.Trim() == "}")
            {
                insideBrushDef3 = false;
                brushes.Add(currentBrush);  // Add completed brush to the list
                currentBrush = "";  // Reset for the next brush
            }

            if (insideBrushDef3)
            {
                string adjustedLine = AdjustDistValues(line);
                currentBrush += adjustedLine + "\n";
            }
        }


        return brushes;
    }

    static string AdjustDistValues(string line)
    {
        string pattern = @"\(\s*(-?\d+\.?\d*)\s*(-?\d+\.?\d*)\s*(-?\d+\.?\d*)\s*(-?\d+\.?\d*)\s*\)\s*\(\s*\(\s*(-?\d+\.?\d*)\s*(-?\d+\.?\d*)\s*(-?\d+\.?\d*)\s*\)\s*\(\s*(-?\d+\.?\d*)\s*(-?\d+\.?\d*)\s*(-?\d+\.?\d*)\s*\)\s*\)";
        Match match = Regex.Match(line, pattern);

        if (match.Success)
        {
            string nx = match.Groups[1].Value;
            string ny = match.Groups[2].Value;
            string nz = match.Groups[3].Value;
            string dist = match.Groups[4].Value;

            // Texture coordinates
            string tu1 = match.Groups[5].Value;
            string tv1 = match.Groups[6].Value;
            string tw1 = match.Groups[7].Value;
            string tu2 = match.Groups[8].Value;
            string tv2 = match.Groups[9].Value;
            string tw2 = match.Groups[10].Value;

            // Correctly calculate the scaling factor
            double scalingFactor = DOOM3_TO_DOOMETERNAL_SCALE;

            // Scale the dist value
            double originalDist = Convert.ToDouble(dist);
            double scaledDist = originalDist * scalingFactor;

            // Scale the texture coordinates
            double scaledTu1 = Convert.ToDouble(tu1) * DOOM3_TO_DOOMETERNAL_SCALETEX;
            double scaledTv1 = Convert.ToDouble(tv1) * DOOM3_TO_DOOMETERNAL_SCALETEX;
            double scaledTw1 = Convert.ToDouble(tw1) * DOOM3_TO_DOOMETERNAL_SCALETEX;
            double scaledTu2 = Convert.ToDouble(tu2) * DOOM3_TO_DOOMETERNAL_SCALETEX;
            double scaledTv2 = Convert.ToDouble(tv2) * DOOM3_TO_DOOMETERNAL_SCALETEX;
            double scaledTw2 = Convert.ToDouble(tw2) * DOOM3_TO_DOOMETERNAL_SCALETEX;

            // Ensure that the scaled values are formatted as fixed-point numbers with sufficient precision
            string formattedScaledDist = scaledDist.ToString("F8");
            string formattedScaledTu1 = scaledTu1.ToString("F8");
            string formattedScaledTv1 = scaledTv1.ToString("F8");
            string formattedScaledTw1 = scaledTw1.ToString("F8");
            string formattedScaledTu2 = scaledTu2.ToString("F8");
            string formattedScaledTv2 = scaledTv2.ToString("F8");
            string formattedScaledTw2 = scaledTw2.ToString("F8");

            // Create the new line by reconstructing the entire match with the scaled and formatted values
            string newLine = $"( {nx} {ny} {nz} {formattedScaledDist} ) ( ( {formattedScaledTu1} {formattedScaledTv1} {formattedScaledTw1} ) ( {formattedScaledTu2} {formattedScaledTv2} {formattedScaledTw2} ) )";

            // Replace the entire matched part in the original line
            return line.Replace(match.Value, newLine);
        }

        return line;
    }

    static void SaveAsDoomEternalFormat(string outputFilePath, List<string> brushes)
    {
        using (StreamWriter writer = new StreamWriter(outputFilePath))
        {
            writer.WriteLine("Version 7");
            writer.WriteLine("HierarchyVersion 1");
            writer.WriteLine("entity {");
            writer.WriteLine("\tentityDef world {");
            writer.WriteLine("\t\tinherit = \"worldspawn\";");
            writer.WriteLine("\t\tedit = {");
            writer.WriteLine("\t\t}");
            writer.WriteLine("\t}");

            int handle = 1843093865;  // starting handle, you can modify this to your needs

            foreach (string brush in brushes)
            {
                writer.WriteLine($"{{");
                // Add two spaces before each line of the brush
                string[] brushLines = brush.Split(new[] { "\n" }, StringSplitOptions.None);
                foreach (var brushLine in brushLines)
                {
                    writer.WriteLine($"  {brushLine}");
                }
                writer.WriteLine($"\t}}");
                writer.WriteLine($"}}");
                handle += 1;  // increment handle for each brush
            }

            writer.WriteLine("}");
            writer.WriteLine("}");
        }
    }
}
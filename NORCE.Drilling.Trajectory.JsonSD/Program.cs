using System;
using System.IO;
using NJsonSchema;
using NORCE.Drilling.Trajectory.Model;

namespace NORCE.Drilling.Trajectory.JsonSD
{
    class Program
    {
        static void Main(string[] args)
        {
            GenerateJsonSchemas();
        }

        static void GenerateJsonSchemas()
        {
            string rootDir = ".\\";
            bool found = false;
            do
            {
                DirectoryInfo info = Directory.GetParent(rootDir);
                if (info != null && "Trajectory".Equals(info.Name))
                {
                    found = true;
                }
                else
                {
                    rootDir += "..\\";
                }
            } while (!found);
            rootDir += "NORCE.Drilling.Trajectory.Service\\wwwroot\\Trajectory\\json-schemas\\";
            var trajectorySchema = JsonSchema.FromType<Model.Trajectory>();
            var trajectorySchemaSchemaJson = trajectorySchema.ToJson();
            using (StreamWriter writer = new StreamWriter(rootDir + "Trajectory.txt"))
            {
                writer.WriteLine(trajectorySchemaSchemaJson);
            }           
        }
    }
}

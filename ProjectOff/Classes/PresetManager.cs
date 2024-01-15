using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOff.Classes
{
    public class PresetManager
    {
        private const string FileName = "presets.xml";

        public DataTable LoadPresets()
        {
            DataTable dataTable;

            if (File.Exists(FileName))
            {
                dataTable = DeserializeDataTable(FileName);

                if (dataTable != null && dataTable.Rows.Count > 0)
                {
                    return dataTable;
                }
            }
            dataTable = CreateDefaultPresetsDataTable();
            SerializeDataTable(dataTable, FileName);

            return dataTable;
        }

        public void SavePresets(DataTable presets)
        {
            SerializeDataTable(presets, FileName);
        }

        private DataTable CreateDefaultPresetsDataTable()
        {
            DataTable dataTable = new DataTable("Presets");
            dataTable.Columns.Add("PresetID", typeof(int));
            dataTable.Columns.Add("Time", typeof(int));
            AddPreset(dataTable, 1, 10);
            AddPreset(dataTable, 2, 30);
            AddPreset(dataTable, 3, 60);
            AddPreset(dataTable, 4, 300);
            AddPreset(dataTable, 5, 600);

            return dataTable;
        }

        public void AddPreset(DataTable dataTable, int id, int time)
        {
            DataRow row = dataTable.NewRow();
            row["PresetID"] = id;
            row["Time"] = time;
            dataTable.Rows.Add(row);
        }

        public void SerializeDataTable(DataTable dataTable, string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                dataTable.WriteXml(fs, XmlWriteMode.WriteSchema);
            }
        }

        public DataTable DeserializeDataTable(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    using (FileStream fs = new FileStream(fileName, FileMode.Open))
                    {
                        DataTable dataTable = new DataTable();
                        dataTable.ReadXml(fs);
                        return dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during deserialization: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            return null;
        }
    }
}

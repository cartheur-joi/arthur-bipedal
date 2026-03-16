using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using System.Linq;

namespace Cartheur.Animals.Robot
{
    public class Remember
    {
        static readonly string[] newlineDelimiter = { "\n" };
        static readonly string[] valueDelimiter = { "--" };
        static readonly HashSet<string> AllowedTables = new HashSet<string>
        {
            "AnimationMemory",
            "StablePosition",
            "TrainingSequence"
        };
        public int AnimationID { get; set; }
        public string DataBaseTag { get; set; }
        private string Path { get; set; }
        public static string VerboseCommand { get; set; }
        public Dictionary<string, int> Positions { get; set; }
        public static Dictionary<string, Dictionary<string, int>> CommandPositions { get; set; }
        public static bool AnimationParsed { get; set; }
        public Syntax CommandSyntax { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Remember"/> class. Use this constructor for strictly memory operations.
        /// </summary>
        public Remember()
        {
            DataBaseTag = @"\db\memory.db";
            Positions = new Dictionary<string, int>();
            if (CommandPositions == null)
                CommandPositions = new Dictionary<string, Dictionary<string, int>>();
            Path = Environment.CurrentDirectory;
            CommandSyntax = new Syntax();
            // Retrieve all available data from the database and populate the dictionaries.
            CompileSyntax();
            //var hold = 0;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Remember"/> class. Use this constructor with the desired database for storage.
        /// </summary>
        /// <param name="dataBaseTag">The data base tag.</param>
        /// <remarks>Cannot create two objects with different tags in the same class. Because DataBaseTag was marked as 'static'. Duh!</remarks>
        public Remember(string dataBaseTag)
        {
            DataBaseTag = dataBaseTag;
            Positions = new Dictionary<string, int>();
            if (CommandPositions == null)
                CommandPositions = new Dictionary<string, Dictionary<string, int>>();
            Path = Environment.CurrentDirectory;
        }
        /// <summary>
        /// Initializes the memory by creating dictionaries of all the motors as they are in their current positions.
        /// </summary>
        /// <returns>True if successful.</returns>
        public bool GeneratePositionMemory()
        {
            // Abdomen motors. This already exists.
            return true;
        }
        void CompileSyntax()
        {
            // You will need to code each of the new animations so that they will load. Perhaps there is a way by file to do this.
            // Eight animations in the compendium at the moment.
            RetrieveAnimation(Syntax.RaiseLeftArmAnimation);
            RetrieveAnimation(Syntax.RevertLeftArmAnimation);
            RetrieveAnimation(Syntax.RaiseLeftLegAnimation);
            RetrieveAnimation(Syntax.RevertLeftLegAnimation);
            RetrieveAnimation(Syntax.RaiseRightArmAnimation);
            RetrieveAnimation(Syntax.RevertRightArmAnimation);
            RetrieveAnimation(Syntax.RaiseRightLegAnimation);
            RetrieveAnimation(Syntax.RevertRightLegAnimation);
        }
        public void StoreMemory(string positionData)
        {
            // Store what should be remembered, subsequent to training so it can be recalled if the robot is turned-off.
            // SQLite? Yes!
            try
            {
                // Store the ID, verbose command, and the technical positions.
                string directory = MapPath(DataBaseTag);
                using (SqliteConnection conn = new SqliteConnection(@"Data Source=" + Path + directory))
                {
                    conn.Open();
                    using (SqliteTransaction trans = conn.BeginTransaction())
                    using (SqliteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO AnimationMemory (AnimationID, VerboseCommand, Positions) VALUES (@animationId, @verboseCommand, @positions)";
                        cmd.Parameters.AddWithValue("@animationId", AnimationID);
                        cmd.Parameters.AddWithValue("@verboseCommand", VerboseCommand);
                        cmd.Parameters.AddWithValue("@positions", positionData);
                        cmd.ExecuteNonQuery();
                        trans.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.WriteLog("Error in database: " + ex.Message, Logging.LogType.Error, Logging.LogCaller.Memory);
                return;
            }
            Logging.WriteLog("Animation data saved succesfully.", Logging.LogType.Information, Logging.LogCaller.Memory);
        }
        /// <summary>
        /// Stores the name of the positions, the motor name, and the technical positions.
        /// </summary>
        /// <param name="positionType">Type of the position.</param>
        /// <param name="positionData">The position data.</param>
        /// <remarks>For a new set of goal positions.</remarks>
        public bool StorePosition(string positionType, Dictionary<string, int> positionData)
        {
            string directory = MapPath(DataBaseTag);
            SqliteConnection connection = new SqliteConnection(@"Data Source=" + Path + directory);

            using (connection)
            {
                connection.Open();
                foreach (KeyValuePair<string, int> kvp in positionData)
                {
                    SqliteCommand command = new SqliteCommand
                    {
                        Connection = connection,
                        CommandText = "INSERT INTO StablePosition (PositionType, MotorName, PositionValue) VALUES (@positionType, @motorName, @positionValue)"
                    };
                    command.Parameters.AddWithValue("@positionType", positionType);
                    command.Parameters.AddWithValue("@motorName", kvp.Key);
                    command.Parameters.AddWithValue("@positionValue", kvp.Value);
                    command.ExecuteNonQuery();
                    Logging.WriteLog("Position " + kvp.Key + " written to database", Logging.LogType.Information, Logging.LogCaller.Memory);
                }
                return true;
            }
        }
        /// <summary>
        /// Stores the training sequence.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <param name="trainingSelection">The training selection.</param>
        /// <param name="trainingMotorSequence">The training motor sequence.</param>
        /// <returns></returns>
        public bool StoreTrainingSequence(int sequence, string trainingSelection, Dictionary<string, int> trainingMotorSequence, string dataBaseTag)
        {
            string directory = MapPath(dataBaseTag);
            SqliteConnection connection = new SqliteConnection(@"Data Source=" + Path + directory);

            using (connection)
            {
                connection.Open();
                foreach (KeyValuePair<string, int> kvp in trainingMotorSequence)
                {
                    SqliteCommand command = new SqliteCommand
                    {
                        Connection = connection,
                        CommandText = "INSERT INTO TrainingSequence (SequenceNumber, TrainingType, Motor, Position) VALUES (@sequenceNumber, @trainingType, @motor, @position)"
                    };
                    command.Parameters.AddWithValue("@sequenceNumber", sequence);
                    command.Parameters.AddWithValue("@trainingType", trainingSelection);
                    command.Parameters.AddWithValue("@motor", kvp.Key);
                    command.Parameters.AddWithValue("@position", kvp.Value);
                    command.ExecuteNonQuery();
                    Logging.WriteLog("Training sequence " + sequence + " based on the training selection " + trainingSelection + " written to database", Logging.LogType.Information, Logging.LogCaller.Memory);
                }
                return true;
            }
        }
        /// <summary>
        /// Returns a dictionary object of the data in the database.
        /// </summary>
        /// <param name="positionDataSet">The position data set.</param>
        /// <returns></returns>
        public Dictionary<string, int> TransformPosition(DataSet positionDataSet)
        {
            var dictionary = new Dictionary<string, int>();
            foreach (DataTable table in positionDataSet.Tables)
            {
                foreach (DataRow dr in table.Rows)
                {
                    dictionary.Add(dr[1].ToString(), Convert.ToInt32(dr[2]));
                }
            }
            return dictionary;
        }
        /// <summary>
        /// Clears the table data.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        public bool ClearTable(string tableName)
        {
            if (!AllowedTables.Contains(tableName))
            {
                Logging.WriteLog("Rejected table clear request for " + tableName, Logging.LogType.Warning, Logging.LogCaller.Memory);
                return false;
            }
            string directory = MapPath(DataBaseTag);
            SqliteConnection connection = new SqliteConnection(@"Data Source=" + Path + directory);
            using (connection)
            {
                connection.Open();
                SqliteCommand command = new SqliteCommand();
                command.Connection = connection;
                command.CommandText = "DELETE FROM " + tableName;
                command.ExecuteNonQuery();
                Logging.WriteLog("Table " + tableName + " truncated", Logging.LogType.Information, Logging.LogCaller.Memory);
                return true;
            }
        }
        /// <summary>
        /// Retrieves table-data from the database.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        public DataSet RetrieveData(string tableName)
        {
            if (tableName == "" || !AllowedTables.Contains(tableName))
                return null;
            string directory = MapPath(DataBaseTag);
            SqliteConnection connection = new SqliteConnection(@"Data Source=" + Path + directory);
            DataSet ds = new DataSet();

            using (connection)
            {
                connection.Open();
                SqliteCommand command = new SqliteCommand
                {
                    Connection = connection,
                    CommandText = "SELECT * FROM " + tableName
                };
                try
                {
                    FillDataSet(command, ds, tableName);
                }
                catch (Exception ex)
                {
                    Logging.WriteLog(ex.Message, Logging.LogType.Error, Logging.LogCaller.Memory);
                }
                return ds;
            }
        }
        private int QueryLimbicValue(string limbic, string dataBaseTag)
        {
            // SELECT PositionValue FROM StablePosition WHERE MotorName='r_hip_x'
            string directory = MapPath(dataBaseTag);
            SqliteConnection connection = new SqliteConnection(@"Data Source=" + Path + directory);
            using (connection)
            {
                connection.Open();
                SqliteCommand command = new SqliteCommand
                {
                    Connection = connection,
                    CommandText = "SELECT PositionValue FROM StablePosition WHERE MotorName = @motorName LIMIT 1"
                };
                command.Parameters.AddWithValue("@motorName", limbic);
                object result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return 0;
                return Convert.ToInt32(result);
            }
        }
        /// <summary>
        /// Retrieves the limbic replay from the positions database.
        /// </summary>
        /// <param name="limbicArray">The limbic array.</param>
        /// <param name="dataBaseTag">The database tag.</param>
        /// <returns></returns>
        public Dictionary<string, int> RetrieveLimbicReplay(string[] limbicArray, string dataBaseTag)
        {
            var dictionary = new Dictionary<string, int>();
            for (int i = 0; i < limbicArray.Length; i++)
            {
                dictionary.Add(limbicArray[i], QueryLimbicValue(limbicArray[i], dataBaseTag));
            }
            return dictionary;
        }
        /// <summary>
        /// Retrieves the animation.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="autoParse">if set to <c>true</c> [automatic parse].</param>
        private void RetrieveAnimation(string command, bool autoParse = true)
        {

            // First try is to pass the command itself. The ID will sort later.
            try
            {
                // Recall the animation data.
                string directory = MapPath(DataBaseTag);
                DataSet ds = new DataSet();
                using (SqliteConnection conn = new SqliteConnection(@"Data Source=" + Path + directory))
                {
                    conn.Open();
                    using (SqliteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM AnimationMemory WHERE VerboseCommand = @command";
                        cmd.Parameters.AddWithValue("@command", command);
                        FillDataSet(cmd, ds, "AnimationMemory");
                    }
                }
                if (autoParse && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    ParseAnimation(ds, command);

            }
            catch (Exception ex)
            {
                Logging.WriteLog("Error in recalling animation data: " + ex.Message, Logging.LogType.Error, Logging.LogCaller.Memory);
            }
        }
        /// <summary>
        /// Parses the animation and adds to a searchable dictionary.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>A string of the parsed animation from the database.</returns>
        void ParseAnimation(DataSet input, string command)
        {
            var sum = input.Tables[0].Rows[0]["Positions"].ToString();
            var keyvalue = sum.Split(newlineDelimiter, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < keyvalue.Length; i++)
            {
                string[] entry = keyvalue[i].Split(valueDelimiter, StringSplitOptions.RemoveEmptyEntries);
                if (entry.Length == 2)
                {
                    if (!Positions.ContainsKey(entry[0]))
                        Positions.Add(entry[0], Convert.ToUInt16(entry[1]));
                }
            }
            var output = Positions.Select(kv => kv.Key + "--" + kv.Value.ToString());
            AnimationParsed = true;
            CommandPositions[command] = new Dictionary<string, int>(Positions);
            Positions.Clear();

        }
        /// <summary>
        /// Searches the dictionary animation by a syntax command.
        /// </summary>
        /// <param name="positionName">Name of the position.</param>
        /// <returns></returns>
        public static Dictionary<string, int> SearchDictionaryAnimation(string positionName)
        {
            return CommandPositions.FirstOrDefault(x => x.Key == positionName).Value; ;
        }
        string MapPath(string path)
        {
            string zebra = AppDomain.CurrentDomain.BaseDirectory.ToString();
            return System.IO.Path.Combine(zebra, path);
        }

        static void FillDataSet(SqliteCommand command, DataSet dataSet, string tableName)
        {
            using (SqliteDataReader reader = command.ExecuteReader())
            {
                DataTable table = new DataTable(tableName);
                for (int i = 0; i < reader.FieldCount; i++)
                    table.Columns.Add(reader.GetName(i), typeof(object));

                while (reader.Read())
                {
                    DataRow row = table.NewRow();
                    for (int i = 0; i < reader.FieldCount; i++)
                        row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                    table.Rows.Add(row);
                }

                dataSet.Tables.Add(table);
            }
        }
    }
}

namespace Cartheur.Animals.Robot
{
    public static class Extensions
    {
        static string[] valueDelimiter = { "--" };
        static Dictionary<string, int> MotorFunctionalPairs = new Dictionary<string, int>();

        public static void StoreMotorSequenceAsFile(this Dictionary<string, int> value, string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
            {
                using (TextWriter tw = new StreamWriter(fs))

                    foreach (KeyValuePair<string, int> kvp in value)
                    {
                        tw.WriteLine(string.Format("{0}--{1}", kvp.Key, kvp.Value));
                    }
            }
        }
        public static Dictionary<string, int> BuildMotorSequence(this MotorSequence value, string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                string _line;
                while ((_line = sr.ReadLine()) != null)
                {
                    string[] keyvalue = _line.Split(valueDelimiter, StringSplitOptions.RemoveEmptyEntries);
                    if (keyvalue.Length == 2)
                    {
                        MotorFunctionalPairs.Add(keyvalue[0], Convert.ToUInt16(keyvalue[1]));
                    }
                }
            }
            return MotorFunctionalPairs;
        }
    }
}

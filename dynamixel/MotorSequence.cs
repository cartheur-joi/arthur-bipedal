namespace Cartheur.Animals.Robot
{
    public class MotorSequence
    {
        string[] delimiter = { "," };
        public static Dictionary<string, int> SequenceCommand { get; set; }
        public static Dictionary<string, int> MotorArrayOfInterest { get; set; }
        public static Dictionary<string, int> TrainingMotorSequence { get; set; }
        public static List<Dictionary<string, int>> MotorArraysOfInterest { get; set; }
        public MotorFunctions MotorControl { get; set; }
        public Remember MotorTraining { get; set; }
        public Remember LimbicRetrieval { get; set; }
        
        public MotorSequence() 
        { 
            SequenceCommand= new Dictionary<string, int>();
            MotorArrayOfInterest= new Dictionary<string, int>();
            MotorArraysOfInterest= new List<Dictionary<string, int>>();
            MotorControl= new MotorFunctions();
            // Due to experience with the second instance overriding the first, check this.
            MotorTraining = new Remember(@"\db\trainings.db");
            LimbicRetrieval = new Remember(@"\db\positions.db");
        }
        /// <summary>
        /// Creates the dictionary of positions based upon the <see cref="Limbic.LimbicArea"/>
        /// </summary>
        /// <param name="motorArray">The motor array passed in as, for example, <see cref="Limbic.Abdomen"/>.</param>
        public void CreateDictionaryOfPositions(string[] motorArray)
        {
            foreach (var line in motorArray)
            {
                // Get the current positions of the motors listed in the array.
                MotorArrayOfInterest.Add(line, MotorControl.GetPresentPosition(line));
                MotorArraysOfInterest.Add(MotorArrayOfInterest);
            }
        }
        /// <summary>
        /// Creates and returns the dictionary of positions based upon the <see cref="Limbic.LimbicArea"/>
        /// </summary>
        /// <param name="motorArray">The motor array passed in as, for example, <see cref="Limbic.Abdomen"/>.</param>
        public Dictionary<string, int> ReturnDictionaryOfPositions(string[] motorArray)
        {
            //if (MotorArrayOfInterest.Count != 0)
            //    MotorArrayOfInterest.Clear();
            var dictionary = new Dictionary<string, int>();
            foreach (var line in motorArray)
            {
                // Get the current positions of the motors listed in the array.
                dictionary.Add(line, MotorControl.GetPresentPosition(line));
                // NOT like this as all instances will be updated.
                //MotorArrayOfInterest.Add(line, MotorControl.GetPresentPosition(line));
                //MotorArraysOfInterest.Add(MotorArrayOfInterest);
            }
            return dictionary;
        }
        /// <summary>
        /// Retrieves the motor position and stores as training selection in the database.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <param name="trainingSelection">The training selection.</param>
        /// <param name="motorArray">The motor array.</param>
        /// <param name="instance">The instance.</param>
        public void CreatePositTrainingSelection(int sequence, string trainingSelection, string[] motorArray, Remember instance)
        {
            foreach (var line in motorArray)
            {
                TrainingMotorSequence.Add(line, MotorControl.GetPresentPosition(line));
            }
            MotorTraining.StoreTrainingSequence(sequence, trainingSelection, TrainingMotorSequence, instance.DataBaseTag);
        }
        public void ReplayLimbicPosition(string[] limbicArray, Remember instance)
        {
            MotorControl.MoveMotorSequence(instance.RetrieveLimbicReplay(limbicArray, instance.DataBaseTag));
        }
    }
}

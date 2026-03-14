namespace DynamixelWizard.SubForms
{
    public partial class DemosForm : Form
    {
        public static bool Instance { get; set; }

        public DemosForm()
        {
            InitializeComponent();
        }

        #region Demos in increasing complexity

        public void HandshakingDemo()
        {

        }
        public void InteractionOneDemo()
        {

        }
        public void StandingDemo()
        {

        }

        #endregion

        #region Events

        private void CloseButtonClick(object sender, System.EventArgs e)
        {
            Instance = false;
            Close();
        }

        #endregion
    }
}

namespace MComms_Transmuxer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Configuration.Install;
    using System.Linq;

    /// <summary>
    /// Service installer
    /// </summary>
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        /// <summary>
        /// Creates new instance of ProjectInstaller
        /// </summary>
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called on Install
        /// </summary>
        /// <param name="stateSaver">State saver</param>
        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);
            Statistics.CreatePerfCounterCategory();
        }
    }
}

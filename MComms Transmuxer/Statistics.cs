namespace MComms_Transmuxer
{
    using System;
    using System.Diagnostics;

    public class Statistics
    {
        private const string categoryName = "MComms Transmuxer";
        private const string categoryHelp = "MComms Transmuxer statistics counters";

        private const string sCounterNameNumberOfConnection = "Number of Connections";
        private PerformanceCounter perfCountNumberOfConnection;
        private const string sCounterNameTotalBandwidth = "Total Bandwidth";
        private PerformanceCounter perfCountTotalBandwidth;

        /// <summary>
        /// Create the performance counter categories
        /// </summary>
        /// <returns>false if already existing, true if created</returns>
        /// <remarks>
        /// It is strongly recommended that new performance counter categories be 
        /// created during the installation of the application, 
        /// not during the execution of the application. This allows time for the 
        /// operating system to refresh its list of registered performance counter categories. 
        /// If the list has not been refreshed, the attempt to use the category will fail.
        /// </remarks>
        public static bool CreatePerfCounterCategory()
        {
            try
            {
                // Need to delete the entire category when changing the counters, must be done during install
                if (PerformanceCounterCategory.Exists(categoryName))
                {
                    // Can be deleted manually using this command line tool http://msdn.microsoft.com/en-us/library/windows/desktop/aa372130%28v=vs.85%29.aspx
                    PerformanceCounterCategory.Delete(categoryName);
                }

                Global.Log.DebugFormat("Creating performance counters");

                // Create a collection of type CounterCreationDataCollection.
                CounterCreationDataCollection CounterDatas = new CounterCreationDataCollection();

                // Create the counters and set their properties.
                CounterCreationData cdCounter1 = new CounterCreationData(sCounterNameNumberOfConnection, "Number of Connections", PerformanceCounterType.NumberOfItems32);
                CounterCreationData cdCounter2 = new CounterCreationData(sCounterNameTotalBandwidth, "Total Bandwidth bps", PerformanceCounterType.NumberOfItems32);

                CounterDatas.Add(cdCounter1);
                CounterDatas.Add(cdCounter2);

                // Create the category and pass the collection to it.
                PerformanceCounterCategory.Create(categoryName, categoryHelp, PerformanceCounterCategoryType.MultiInstance, CounterDatas);

                return (true);
            }
            catch (Exception ex)
            {
                Global.Log.ErrorFormat("Exception caught in CreatePerfCounterCategory: {0}", ex.ToString());
            }

            return (false);
        }

        /// <summary>
        /// Create the performance counter instances, Multiview needs to be running for these to show in PerfMon
        /// </summary>
        /// <remarks>
        /// If the category is created without error but the counters can't be created, run the following commands from dos
        /// lodctr /R
        /// cd C:\Windows\Inf\.NETFramework
        /// lodctr corperfmonsymbols.ini
        /// Performance counter categories are stored in the registry at:
        /// HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\MComms Transmuxer and
        /// HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib\009 (very hard to edit manually)
        /// </remarks>
        public bool InitStats()
        {
            try
            {
                string instance = string.Format("Transmuxer {0}", Process.GetCurrentProcess().Id);
                perfCountNumberOfConnection = new PerformanceCounter(categoryName, sCounterNameNumberOfConnection, instance, false);
                perfCountTotalBandwidth = new PerformanceCounter(categoryName, sCounterNameTotalBandwidth, instance, false);

                return true;
            }
            catch (Exception ex)
            {
                Global.Log.ErrorFormat("Exception caught in CreateCounters: {0}", ex.ToString());
                return false;
            }
        }

        public void CollectNetworkInfo(int numberOfConnections, int totalBandwidth)
        {
            if (perfCountNumberOfConnection != null)
            {
                perfCountNumberOfConnection.RawValue = numberOfConnections;
            }

            if (perfCountTotalBandwidth != null)
            {
                perfCountTotalBandwidth.RawValue = totalBandwidth;
            }
        }
    }
}

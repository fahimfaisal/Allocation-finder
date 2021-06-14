using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Net;
using PT1;
using WcfServiceLibrary;
using System.Threading;
using System.ServiceModel;

namespace AllocationsApplication
{
    partial class AllocationsViewerForm : Form
    {
        #region properties
        private Allocations PT1Allocations;
        private Configuration PT1Configuration;
        private ErrorsViewer ErrorListViewer = new ErrorsViewer();
        private AboutBox AboutBox = new AboutBox();
        #endregion

        ConfigData config;
        int completedOperation;
        int timedOutOperations;
        int numberOfOperations;

        AutoResetEvent autoReset = new AutoResetEvent(false);

        List<AllocationData> allAllocations;

        readonly object aLock = new object();
        #region constructors
        public AllocationsViewerForm()
        {
            InitializeComponent();

            this.Text += String.Format(" ({0})", Application.ProductVersion);
        }
        #endregion

        #region File menu event handlers
        private void OpenAllocationsFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearGUI();

            // Process allocations and configuration files.
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // Get both filenames.
                String allocationsFileName = openFileDialog1.FileName;
                String configurationFileName = Allocations.ConfigurationFileName(allocationsFileName);

                // Parse configuration file.
                if (configurationFileName == null)
                    PT1Configuration = new Configuration();
                else
                {
                    using (WebClient configWebClient = new WebClient())
                    using (Stream configStream = configWebClient.OpenRead(configurationFileName))
                    using (StreamReader configFile = new StreamReader(configStream))
                    {
                        Configuration.TryParse(configFile, configurationFileName,
                            out PT1Configuration, out List<String> configurationErrors);
                    }
                }

                // Parse allocations file.
                using (WebClient allocationsWebClient = new WebClient())
                using (Stream allocationsStream = allocationsWebClient.OpenRead(allocationsFileName))
                using (StreamReader allocationsFile = new StreamReader(allocationsStream))
                {
                    Allocations.TryParse(allocationsFile, allocationsFileName, PT1Configuration,
                        out PT1Allocations, out List<String> allocationsErrors);
                }

                // Refesh GUI and Log errors.
                UpdateGUI();
                PT1Allocations.LogFileErrors(PT1Allocations.FileErrorsTXT);
                PT1Allocations.LogFileErrors(PT1Configuration.FileErrorsTXT);
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion

        #region  Clear and Update GUI
        private void ClearGUI()
        {
            // As we are opening a Configuration file,
            // indicate allocations are not valid, and clear GUI.
            allocationToolStripMenuItem.Enabled = false;

            if (allocationWebBrowser.Document != null)
                allocationWebBrowser.Document.OpenNew(true);
            allocationWebBrowser.DocumentText = String.Empty;

            if (ErrorListViewer.WebBrowser.Document != null)
                ErrorListViewer.WebBrowser.Document.OpenNew(true);
            ErrorListViewer.WebBrowser.DocumentText = String.Empty;
        }

        private void UpdateGUI()
        {
            // Update GUI:
            // - enable menu
            // - display Allocations data (whether valid or invalid)
            // - display Allocations and Configuration file errors.
            if (PT1Allocations != null && PT1Allocations.FileValid &&
                PT1Configuration != null && PT1Configuration.FileValid)
                allocationToolStripMenuItem.Enabled = true;

            if (allocationWebBrowser.Document != null)
                allocationWebBrowser.Document.OpenNew(true);
            if (ErrorListViewer.WebBrowser.Document != null)
                ErrorListViewer.WebBrowser.Document.OpenNew(true);

            if (PT1Allocations != null)
            {
                allocationWebBrowser.DocumentText = PT1Allocations.ToStringHTML();
                ErrorListViewer.WebBrowser.DocumentText =
                    PT1Allocations.FileErrorsHTML +
                    PT1Configuration.FileErrorsHTML +
                    PT1Allocations.AllocationsErrorsHTML;
            }
        }
        #endregion

        #region Validate menu event handlers
        private void AllocationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Check if the allocations are valid.
            PT1Allocations.Validate();

            // Update GUI - display allocations file data (whether valid or invalid), 
            // allocations file errors, config file errors, and allocation errors.
            allocationWebBrowser.DocumentText = PT1Allocations.ToStringHTML();
            ErrorListViewer.WebBrowser.DocumentText =
                PT1Allocations.FileErrorsHTML +
                PT1Configuration.FileErrorsHTML +
                PT1Allocations.AllocationsErrorsHTML;

            // Log errors.
            PT1Allocations.LogFileErrors(PT1Allocations.AllocationsErrorsTXT);
        }
        #endregion

        #region View menu event handlers
        private void ErrorListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ErrorListViewer.WindowState = FormWindowState.Normal;
            ErrorListViewer.Show();
            ErrorListViewer.Activate();
        }
        #endregion

        #region Help menu event handlers
        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox.ShowDialog();
        }
        #endregion

        #region Generate Allocations
        private void generateAllocationsButton_Click(object sender, EventArgs e)
        {
            ClearGUI();

            //Get the URL from the comboBox

            string url = this.comboBox1.GetItemText(this.comboBox1.SelectedItem);

            //Get the name of the file from the URL


            string configurationFileName = GetFileName(url);


            //Read the file and create a configuration object

            using (WebClient configWebClient = new WebClient())
            using (Stream stream = configWebClient.OpenRead(url))
            using (StreamReader configFile = new StreamReader(stream))
            {

                Configuration.TryParse(configFile, configurationFileName, out PT1Configuration, out List<String> error);

            }

            //Update the configData class so that algorithm can use it.

            config = UpdateConfig(PT1Configuration);

            Double[,] energyMatrix = Energy(PT1Configuration);
            Double[,] localCommunication = Transform1Double(PT1Configuration.LocalCommunication,config.NumberOfProcessors,config.NumberOfTasks);
            Double[,] remoteCommunication = Transform1Double(PT1Configuration.RemoteCommunication, config.NumberOfProcessors, config.NumberOfTasks);
            //Creating a new thread 

            System.Threading.Tasks.Task.Run(() => DoAsyncCalls());


            //Wait GUI for 5 minutes if desired allocation is not returned

            autoReset.WaitOne(50000);


            //process all allocations returned


            //List to save all the maps returned

            List<int[]> allocationsList = new List<int[]>();


            //List to save all the converted string of the allocations 
            List<string> allocationString = new List<string>();

            //Saving all the lowest energy maps returned
          

           
          
            double old = 0;
            double energy = 0;
            foreach (AllocationData d in allAllocations)
            {

                foreach (int[] item in d.Map)
                {
                    energy = CalcEnergy(item, PT1Configuration, energyMatrix, localCommunication,remoteCommunication);

                    if (allocationsList.Count == 0)
                    {
                        allocationsList.Add(item);
                        old = energy;
                    }
                    else
                    {
                        if (energy<old)
                        {
                            if (energy == old)
                            {
                                allocationsList.Add(item);

                                old = energy;
                            }
                            else
                            {
                                allocationsList.RemoveAt(allocationsList.Count - 1);

                                allocationsList.Add(item);

                                old = energy;
                            }
                           
                          
                        }

                       
                    }

                   
                 
                }

                
            }


           

            //Coverting all the maps to the string object

            allocationString = ConvertToString(allocationsList, config.NumberOfTasks);



            //Create a string containing description of Task Allocation file

            string description = CreateTaff(configurationFileName, allocationString.Count, config.NumberOfTasks, config.NumberOfProcessors, allocationString);

            //Create the Allocation file object and display on GUI

            Allocations.TryParse(description, PT1Configuration, out PT1Allocations, out List<String> errors);


            PT1Allocations.Validate();

            UpdateGUI();

            PT1Allocations.LogFileErrors(PT1Allocations.FileErrorsTXT);
            PT1Allocations.LogFileErrors(PT1Configuration.FileErrorsTXT);
        }

        private double[,] Transform1Double(double[] array, int rows, int columns)
        {
            int count = 0;

            double[,] newArray = new double[rows, columns];

            for (int i = 0; i < newArray.GetLength(0); i++)
            {
                for (int j = 0; j < newArray.GetLength(1); j++)
                {
                    newArray[i, j] = array[count];
                    count++;
                }

            }

            return newArray;
        
        }

        void DoAsyncCalls()
        {
            GreedyReference.GreedyServiceClient greedy = new GreedyReference.GreedyServiceClient();
            HeuresticReference.HueresticServiceClient heurestic = new HeuresticReference.HueresticServiceClient();

            heurestic.HeuresticAlgCompleted += Heurestic_HeuresticAlgCompleted;

            greedy.GreedyAlgCompleted += Greedy_GreedyAlgCompleted;

            //ALBgreedy.GreedyServiceClient albGreedy = new ALBgreedy.GreedyServiceClient();

            //ALBheurestic.HueresticServiceClient albHeurestic = new ALBheurestic.HueresticServiceClient();

            //albGreedy.GreedyAlgCompleted += AlbGreedy_GreedyAlgCompleted;

            //albHeurestic.HeuresticAlgCompleted += AlbHeurestic_HeuresticAlgCompleted;

          



            lock (aLock)
            {
                allAllocations = new List<AllocationData>();
                numberOfOperations = PT1Configuration.Program.Tasks*2;
                completedOperation = 0;
                timedOutOperations = 0;
            }


            for (int i = 0; i < PT1Configuration.Program.Tasks; i++)
            {
                greedy.GreedyAlgAsync(50000, config, i);

                heurestic.HeuresticAlgAsync(50000, config, i);

                //albGreedy.GreedyAlgAsync(50000, config, i);
                //albHeurestic.HeuresticAlgAsync(50000, config, i);


            }

        }

        //private void AlbHeurestic_HeuresticAlgCompleted(object sender, ALBheurestic.HeuresticAlgCompletedEventArgs e)
        //{
        //    try
        //    {



        //        AllocationData data = e.Result;


        //        lock (aLock)
        //        {
        //            completedOperation++;

        //            allAllocations.Add(data);

        //            if (completedOperation == numberOfOperations)
        //            {
        //                autoReset.Set();
        //            }

        //        }
        //    }
        //    catch (Exception ex) when (ex.InnerException is TimeoutException tex)
        //    {
        //        //Handle local timeout
        //        lock(aLock)
        //        {
        //            completedOperation++;

        //            timedOutOperations++;


        //            if (completedOperation == numberOfOperations)
        //            {
        //                autoReset.Set();
        //            }
        //        }
               



        //    }
        //    catch (Exception ex) when (ex.InnerException is FaultException<HeuresticReference.TimeoutFault> fex)
        //    {
        //        //Handle remote timeout

        //        lock(aLock)
        //        {
        //            completedOperation++;

        //            timedOutOperations++;


        //            if (completedOperation == numberOfOperations)
        //            {
        //                autoReset.Set();
        //            }
        //        }
               
        //    }
        //    catch (Exception ex) when (ex.InnerException is CommunicationException cex)
        //    {
        //        lock (aLock)
        //        {
        //            completedOperation++;

        //            timedOutOperations++;


        //            if (completedOperation == numberOfOperations)
        //            {
        //                autoReset.Set();
        //            }
        //        }
                   
        //    }
        //    catch (Exception ex) when (ex.InnerException is WebException wex)
        //    {

        //        lock (aLock)
        //        {
        //            completedOperation++;

        //            timedOutOperations++;


        //            if (completedOperation == numberOfOperations)
        //            {
        //                autoReset.Set();
        //            }
        //        }
        //    }

        //}

        //private void AlbGreedy_GreedyAlgCompleted(object sender, ALBgreedy.GreedyAlgCompletedEventArgs e)
        //{
        //    try
        //    {
        //        AllocationData data = e.Result;

        //        lock (aLock)
        //        {
        //            completedOperation++;

        //            allAllocations.Add(data);

        //            if (completedOperation == numberOfOperations)
        //            {
        //                autoReset.Set();
        //            }

        //        }
        //    }
        //    catch (Exception ex) when (ex.InnerException is TimeoutException tex)
        //    {
        //        //Handle local timeout
        //        lock (aLock)
        //        {
        //            completedOperation++;

        //            timedOutOperations++;


        //            if (completedOperation == numberOfOperations)
        //            {
        //                autoReset.Set();
        //            }
        //        }




        //    }
        //    catch (Exception ex) when (ex.InnerException is FaultException<HeuresticReference.TimeoutFault> fex)
        //    {
        //        //Handle remote timeout

        //        lock (aLock)
        //        {
        //            completedOperation++;

        //            timedOutOperations++;


        //            if (completedOperation == numberOfOperations)
        //            {
        //                autoReset.Set();
        //            }
        //        }

        //    }
        //    catch (Exception ex) when (ex.InnerException is CommunicationException cex)
        //    {
        //        lock (aLock)
        //        {
        //            completedOperation++;

        //            timedOutOperations++;


        //            if (completedOperation == numberOfOperations)
        //            {
        //                autoReset.Set();
        //            }
        //        }

        //    }
        //    catch (Exception ex) when (ex.InnerException is WebException wex)
        //    {

        //        lock (aLock)
        //        {
        //            completedOperation++;

        //            timedOutOperations++;


        //            if (completedOperation == numberOfOperations)
        //            {
        //                autoReset.Set();
        //            }
        //        }
        //    }
        //}

        private void Heurestic_HeuresticAlgCompleted(object sender, HeuresticReference.HeuresticAlgCompletedEventArgs e)
        {
            try
            {



                AllocationData data = e.Result;


                lock (aLock)
                {
                    completedOperation++;

                    allAllocations.Add(data);

                    if (completedOperation == numberOfOperations)
                    {
                        autoReset.Set();
                    }

                }
            }
            catch (Exception ex) when (ex.InnerException is TimeoutException tex)
            {
                //Handle local timeout
                lock (aLock)
                {
                    completedOperation++;

                    timedOutOperations++;


                    if (completedOperation == numberOfOperations)
                    {
                        autoReset.Set();
                    }
                }




            }
            catch (Exception ex) when (ex.InnerException is FaultException<HeuresticReference.TimeoutFault> fex)
            {
                //Handle remote timeout

                lock (aLock)
                {
                    completedOperation++;

                    timedOutOperations++;


                    if (completedOperation == numberOfOperations)
                    {
                        autoReset.Set();
                    }
                }

            }
            catch (Exception ex) when (ex.InnerException is CommunicationException cex)
            {
                lock (aLock)
                {
                    completedOperation++;

                    timedOutOperations++;


                    if (completedOperation == numberOfOperations)
                    {
                        autoReset.Set();
                    }
                }

            }
            catch (Exception ex) when (ex.InnerException is WebException wex)
            {

                lock (aLock)
                {
                    completedOperation++;

                    timedOutOperations++;


                    if (completedOperation == numberOfOperations)
                    {
                        autoReset.Set();
                    }
                }
            }
        }

        private void Greedy_GreedyAlgCompleted(object sender, GreedyReference.GreedyAlgCompletedEventArgs e)
        {
            try
            {
                AllocationData data = e.Result;

                lock (aLock)
                {
                    completedOperation++;

                    allAllocations.Add(data);

                    if (completedOperation == numberOfOperations)
                    {
                        autoReset.Set();
                    }

                }
            }
            catch (Exception ex) when (ex.InnerException is TimeoutException tex)
            {
                //Handle local timeout
                lock (aLock)
                {
                    completedOperation++;

                    timedOutOperations++;


                    if (completedOperation == numberOfOperations)
                    {
                        autoReset.Set();
                    }
                }




            }
            catch (Exception ex) when (ex.InnerException is FaultException<HeuresticReference.TimeoutFault> fex)
            {
                //Handle remote timeout

                lock (aLock)
                {
                    completedOperation++;

                    timedOutOperations++;


                    if (completedOperation == numberOfOperations)
                    {
                        autoReset.Set();
                    }
                }

            }
            catch (Exception ex) when (ex.InnerException is CommunicationException cex)
            {
                lock (aLock)
                {
                    completedOperation++;

                    timedOutOperations++;


                    if (completedOperation == numberOfOperations)
                    {
                        autoReset.Set();
                    }
                }

            }
            catch (Exception ex) when (ex.InnerException is WebException wex)
            {

                lock (aLock)
                {
                    completedOperation++;

                    timedOutOperations++;


                    if (completedOperation == numberOfOperations)
                    {
                        autoReset.Set();
                    }
                }
            }
        }

        private List<String> ConvertToString(List<int[]> allocations, int task)
        {
            List<string> allocationArray = new List<string>();


            foreach (int[] item in allocations)
            {

                int count = 0;

                string s = "";


                for (int i = 0; i < item.Length; i++)
                {
                    count++;
                    if (count == task)
                    {
                        if (i == item.Length - 1)
                        {
                            s += item[i].ToString();
                        }
                        else
                        {
                            s += item[i].ToString() + ";";
                            count = 0;
                        }

                    }
                    else
                    {
                        s += item[i].ToString() + ",";
                    }

                }

                allocationArray.Add(s);
            }

            return allocationArray;
        }




        private ConfigData UpdateConfig(Configuration configuration)
        {

            ConfigData config = new ConfigData();


            Double[] timeMatrix = Transform2D(Runtime(configuration));
            config.Duration = configuration.Program.Duration;
            config.NumberOfProcessors = configuration.Program.Processors;
            config.NumberOfTasks = configuration.Program.Tasks;
            config.TimeMatrix = timeMatrix;
           

            return (config);


        }

        private Double[] Transform2D(Double[,] array)
        {
            Double[] newArray = new Double[array.GetLength(0) * array.GetLength(1)];
            int count = 0;
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    newArray[count] = array[i, j];
                    count++;
                }
            }

            return newArray;

        }


        private string GetFileName(string url)
        {
            Uri uri = new Uri(url);
            string filename = System.IO.Path.GetFileName(uri.LocalPath);

            return filename;
        }

        private double[,] Runtime(Configuration config)
        {
            double[,] runtime = new double[config.Program.Processors, config.Program.Tasks];

            for (int i = 0; i < config.Program.Processors; i++)
            {
                for (int j = 0; j < config.Program.Tasks; j++)
                {
                    if (config.Processors[i].DownloadSpeed >= config.Tasks[j].DownloadSpeed && config.Processors[i].UploadSpeed >= config.Tasks[j].UploadSpeed && config.Processors[i].RAM >= config.Tasks[j].RAM)
                    {
                        double val = (config.Tasks[j].Runtime * (config.Tasks[j].Frequency  / config.Processors[i].Frequency));
                        
                        runtime[i, j] = val;
                    }
                    else
                    {
                        runtime[i, j] = -1;
                    }
                }

            }

            return runtime;
        }

        private double[,] Energy(Configuration config)
        {
            double[,] energy = new double[config.Program.Processors, config.Program.Tasks];

            for (int i = 0; i < config.Program.Processors; i++)
            {
                for (int j = 0; j < config.Program.Tasks; j++)
                {
                    if (config.Processors[i].DownloadSpeed >= config.Tasks[j].DownloadSpeed && config.Processors[i].UploadSpeed >= config.Tasks[j].UploadSpeed && config.Processors[i].RAM >= config.Tasks[j].RAM)
                    {
                        double val = (config.Tasks[j].Runtime * (config.Tasks[j].Frequency / config.Processors[i].Frequency)) * (config.Processors[i].ProcessorType.C[2] * (config.Processors[i].Frequency * config.Processors[i].Frequency) + config.Processors[i].ProcessorType.C[1] * config.Processors[i].Frequency + config.Processors[i].ProcessorType.C[0]);
                       
                        energy[i, j] = val;
                    }
                    else
                    {
                        energy[i, j] = -1;
                    }
                }

            }

            return energy;
        }


        private string CreateTaff(string filename, int count, int task, int processor, List<String> allocations)
        {
            string s = "";
            int id = 0;

            s += Resource.config + Environment.NewLine;
            s += Resource.filename + Resource.separator + "\"" + filename + "\"" + Environment.NewLine;
            s += Resource.config_end + Environment.NewLine;
            s += Resource.allocations + Environment.NewLine;
            s += Resource.count + Resource.separator + count.ToString() + Environment.NewLine;
            s += Resource.tasks + Resource.separator + task.ToString() + Environment.NewLine;
            s += Resource.processors + Resource.separator + processor.ToString() + Environment.NewLine;


            foreach (String line in allocations)
            {
                s += Resource.allocation + Environment.NewLine;
                s += Resource.ID + Resource.separator + id.ToString() + Environment.NewLine;
                s += Resource.map + Resource.separator + line + Environment.NewLine;
                s += Resource.allocation_end + Environment.NewLine;

                id++;
            }

            s += Resource.allocations_end + Environment.NewLine;

            return s;

        }

        private double CalcEnergy(int[] allocation, Configuration config, double[,] Energy, double[,] localCom, double[,] remoteCom)
        {
            List<int[]> activeTasks = new List<int[]>();
            double m = 0d;
           


            int[,] TransformedAllocation = Transform1D(allocation, config.Program.Processors, config.Program.Tasks);
            Double energy = 0;

            for (int i = 0; i < TransformedAllocation.GetLength(0); i++)
            {
                for (int j = 0; j < TransformedAllocation.GetLength(1); j++)
                {
                    if (TransformedAllocation[i,j] == 1)
                    {
                        activeTasks.Add(new int[] { j, i });
                        energy += Energy[i, j];
                    }
                }

            }

            var activeTasksArray = activeTasks.ToArray();

            foreach (int[] element in activeTasksArray)
            {
                for (int i = 0; i < activeTasksArray.Length; i++)
                {
                    if (element[0] == activeTasksArray[i][0])
                    {
                        m += localCom[element[1], activeTasksArray[i][1]];

                    }
                    else
                    {
                        m += remoteCom[element[1], activeTasksArray[i][1]];
                    }
                }

            }

            double lasteng = energy + m;
            return lasteng;

        }


        private int[,] Transform1D(int[] array, int rows, int columns)
        {
            int count = 0;

            int[,] newArray = new int[rows, columns];

            for (int i = 0; i < newArray.GetLength(0); i++)
            {
                for (int j = 0; j < newArray.GetLength(1); j++)
                {
                    newArray[i, j] = array[count];
                    count++;
                }

              
            }

            return newArray;
        }

        #endregion
    }
}

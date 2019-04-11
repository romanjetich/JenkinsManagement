namespace JenkinsManager
{
    using JenkinsManager.Models;
    using JenkinsNET;
    using JenkinsNET.Models;
    using JenkinsNET.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System.Xml.Linq;

    /// <summary>
    /// Interaction logic for JenkinsWindowControl.
    /// </summary>
    public partial class JenkinsWindowControl : UserControl
    {
        JenkinsClient jenkinsClient = null;
        JenkinsJobRunner jenkinsJobRunner = null;
        Dictionary<string, List<JobProperty>> jobData = new Dictionary<string, List<JobProperty>>();
        List<JobProperty> fields;
        /// <summary>
        /// Initializes a new instance of the <see cref="JenkinsWindowControl"/> class.
        /// </summary>
        public JenkinsWindowControl()
        {
            InitializeComponent();
            DataContext = this;

            jenkinsClient = new JenkinsClient
            {
                BaseUrl = "http://localhost:8080/",
                UserName = "admin",
                ApiToken = "118a716bc175f3a101c1e063e299d073a4",
            };
            var jenkins = jenkinsClient.Get();

            List<Job> jobList = new List<Job>();
            foreach (var job in jenkins.Jobs)
            {
                jobList.Add(new Job { Name = job.Name});
            }

            Jobs = new ObservableCollection<Job>(jobList);
            JobCombo.SelectedItem = Jobs[0];

            foreach (var job in jenkins.Jobs)
            {
                fields = new List<JobProperty>();
                JenkinsJobBase jenkinsJob = jenkinsClient.Jobs.Get<JenkinsJobBase>(job.Name) as JenkinsJobBase;
                foreach (var action in jenkinsJob.Actions)
                {
                    FindAllNodes(action.Node);
                }
                jobData[job.Name] = fields;
            }
        }

        public ObservableCollection<Job> Jobs
        {
            get { return (ObservableCollection<Job>)GetValue(JobsProperty); }
            set { SetValue(JobsProperty, value); }
        }

        public static readonly DependencyProperty JobsProperty =
            DependencyProperty.Register("Jobs",
            typeof(ObservableCollection<Job>),
            typeof(JenkinsWindowControl),
            new PropertyMetadata(null));

        public Job SelectedJob
        {
            get { return (Job)GetValue(SelectedJobProperty); }
            set { SetValue(SelectedJobProperty, value); }
        }

        public static readonly DependencyProperty SelectedJobProperty =
            DependencyProperty.Register("SelectedJob",
            typeof(Job),
            typeof(JenkinsWindowControl),
            new PropertyMetadata(null));


        private void Jobs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedJob = JobCombo.SelectedItem as Job;
            SelectedJob.JobProperties = new ObservableCollection<JobProperty>(jobData[SelectedJob.Name]);
        }

        private void FindAllNodes(XNode node)
        {
            if (node == null) return;
            //Console.WriteLine(node.);
            if (node.NodeType == System.Xml.XmlNodeType.Element)
            {
                var element = node as XElement;
                if (element == null) return;
                var firstNode = element.FirstNode as XElement;
                if (firstNode == null)
                {
                    FindAllNodes(element.NextNode);
                    return;
                }

                if (firstNode.Name.LocalName == "parameterDefinition")
                {
                    JobProperty field = new JobProperty();
                    FindAllNodes(firstNode, ref field);
                }
                else
                {
                    FindAllNodes(element.NextNode);
                }
            }
        }

        private void FindAllNodes(XNode node, ref JobProperty field)
        {
            if (node == null) return;

            var element = node as XElement;
            var firstNode = element?.FirstNode as XElement;
            if (firstNode == null) return;

            var value = firstNode?.Value;
            if (firstNode.Name.LocalName.ToLowerInvariant().Contains("name"))
            {
                field.Key = value;
            }
            else if (firstNode.Name.LocalName.ToLowerInvariant().Contains("value"))
            {
                if (field.Value == value)
                {
                    field.FieldType = element.FirstAttribute.Value.ToString();
                    FillField(element.NextNode, ref field);
                    FillField(element.NextNode.NextNode, ref field);
                    fields.Add(field);
                    field = new JobProperty();
                    FindAllNodes(element.Parent.NextNode, ref field);
                    return;
                }
                else
                {
                    field.Value = value;
                }
            }

            FindAllNodes(firstNode, ref field);
        }

        private void FillField(XNode node, ref JobProperty field)
        {
            if (node == null) return;

            var element = node as XElement;
            if (element.Name.LocalName.ToLowerInvariant().Contains("name"))
            {
                field.Key = element.Value;
            }
            else if (element.Name.LocalName.ToLowerInvariant().Contains("value"))
            {
                field.Value = element.Value;
            }
            else if (element.Name.LocalName.ToLowerInvariant().Contains("description"))
            {
                field.Description = element.Value;
            }
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private async void button1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                jenkinsJobRunner = new JenkinsJobRunner(jenkinsClient)
                {
                    BuildTimeout = 60 * 60
                };

                jenkinsJobRunner.StatusChanged += JenkinsJobRunner_StatusChanged;

                string jobName = JobCombo.SelectedValue.ToString();
                jobInfos.Items.Add($"{DateTime.Now.ToLongTimeString()}: start {jobName}");
                JenkinsBuildBase result = null;
                if (jobData[jobName].Count == 0)
                {
                    result = await jenkinsJobRunner.RunAsync(jobName);
                }
                else
                {
                    Dictionary<string, string> parameters = new Dictionary<string, string>();
                    foreach (var jobProperty in SelectedJob.JobProperties)
                    {
                        parameters[jobProperty.Key] = jobProperty.Value;
                    }
                    result = await jenkinsJobRunner.RunWithParametersAsync(jobName, parameters);
                }
                jobInfos.Items.Add($"{DateTime.Now.ToLongTimeString()}: Duration {result.Duration} ms");
                jobInfos.Items.Add($"{DateTime.Now.ToLongTimeString()}: Result {result.Result}");
            }
            catch (Exception ex)
            {
                var t = ex;
            }
        }

        private void JenkinsJobRunner_StatusChanged()
        {
            jobInfos.Items.Add($"{DateTime.Now.ToLongTimeString()}: {jenkinsJobRunner.Status}");
        }

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
        }
    }
}
namespace JenkinsManager
{
    using JenkinsManager.Models;
    using JenkinsManager.Options;
    using JenkinsManager.Windows;
    using JenkinsNET;
    using JenkinsNET.Models;
    using JenkinsNET.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
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
        private readonly JenkinsWindow jenkinsWindow;

        /// <summary>
        /// Initializes a new instance of the <see cref="JenkinsWindowControl"/> class.
        /// </summary>
        public JenkinsWindowControl(JenkinsWindow jenkinsWindow)
        {
            this.jenkinsWindow = jenkinsWindow;
            InitializeComponent();
            DataContext = this;
            ListBinding = new ObservableCollection<ListEntry>();

            Loaded += JenkinsWindowLoaded;
        }

        private void JenkinsWindowLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            jenkinsClient = new JenkinsClient
            {
                BaseUrl = $"http://{Host}:{Port}/",
                UserName = User,
                ApiToken = ApiKey,
            };
            var jenkins = jenkinsClient.Get();

            List<Job> jobList = new List<Job>();
            foreach (var job in jenkins.Jobs)
            {
                jobList.Add(new Job { Name = job.Name });
            }

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

            
            Jobs = new ObservableCollection<Job>(jobList);
            JobCombo.SelectedItem = Jobs[0];
        }

        private JenkinsManagerPackage Package => jenkinsWindow.Package as JenkinsManagerPackage;
        private string Host => (Package?.GetDialogPage(typeof(OptionPage))
            as OptionPage)?.Host ?? "https://localhost";

        private int Port => (Package?.GetDialogPage(typeof(OptionPage))
            as OptionPage)?.Port ?? 8080;

        private string User => (Package?.GetDialogPage(typeof(OptionPage))
            as OptionPage)?.User ?? "admin";

        private string ApiKey => (Package?.GetDialogPage(typeof(OptionPage))
            as OptionPage)?.Key ?? "";

        public Window GetWindow()
        {
            return Window.GetWindow(this);
        }

        public ObservableCollection<ListEntry> ListBinding { get; set; }
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
                ListBinding.Add(new ListEntry { Message = $"start {jobName}" });
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

                ListBinding.Add(new ListEntry { Message = $"Duration { result.Duration } ms" });
                ListBinding.Add(new ListEntry { Message = $"Result {result.Result}" });
            }
            catch (Exception ex)
            {
                var t = ex;
            }
        }

        private void JenkinsJobRunner_StatusChanged()
        {
            ListBinding.Add(new ListEntry { Message = $"{jenkinsJobRunner.Status}" });
        }

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
        }

        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow wnd = new SettingsWindow();
        }

        private void ListBoxJobInfos_Loaded(object sender, RoutedEventArgs e)
        {
            var listBox = (ListBox)sender;
            var scrollViewer = FindScrollViewer(listBox);

            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += (o, args) =>
                {
                    if (args.ExtentHeightChange > 0)
                        scrollViewer.ScrollToBottom();
                };
            }
        }

        private static ScrollViewer FindScrollViewer(DependencyObject root)
        {
            var queue = new Queue<DependencyObject>(new[] { root });

            do
            {
                var item = queue.Dequeue();

                if (item is ScrollViewer)
                    return (ScrollViewer)item;

                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(item); i++)
                    queue.Enqueue(VisualTreeHelper.GetChild(item, i));
            } while (queue.Count > 0);

            return null;
        }

        
    }
    public class ListEntry
    {
        public ListEntry()
        {
            this.Timestamp = DateTime.Now.ToLongTimeString() + ": ";
        }
        public string Timestamp { get; private set; }
        public string Message { get; set; }
    }
}

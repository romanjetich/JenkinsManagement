using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JenkinsManager.Models
{
    public class Job: INotifyPropertyChanged
    {
        private string name;
        private ObservableCollection<JobProperty> jobProperties;
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Name"));
            }
        }

        public ObservableCollection<JobProperty> JobProperties
        {
            get
            {
                return jobProperties;
            }
            set
            {
                jobProperties = value;
                PropertyChanged(this, new PropertyChangedEventArgs("JobProperties"));
            }
        }
    }
}

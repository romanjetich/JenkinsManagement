using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JenkinsManager.Models
{
    public class JobProperty : INotifyPropertyChanged, IEditableObject
    {
        private string key;
        private string value;
        private string description;
        private string fieldType;
        private JobProperty tempValues;

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public string Key
        {
            get
            {
                return key;
            }
            set
            {
                key = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Key"));
            }
        }

        public string Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Value"));
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                description = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Description"));
            }
        }

        public string FieldType
        {
            get
            {
                return fieldType;
            }
            set
            {
                fieldType = value;
                PropertyChanged(this, new PropertyChangedEventArgs("FieldType"));
            }
        }

        public void BeginEdit()
        {
            tempValues = new JobProperty
            {
                Key = Key,
                Value = Value,
                Description = Description,
                FieldType = FieldType
            };
        }

        public void EndEdit()
        {
            //this.Value = tempValues.Value;
            //this.Key = tempValues.Key;
            //this.Description = tempValues.Description;
            //this.FieldType = tempValues.FieldType;
        }

        public void CancelEdit()
        {
            tempValues = null;
        }
    }
}

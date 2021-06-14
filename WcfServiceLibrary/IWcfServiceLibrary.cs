using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WcfServiceLibrary
{
    
    [ServiceContract]
    public interface IWcfServiceLibrary
    {
       
    }

   
    [DataContract]

    public class AllocationData
    { 

        [DataMember]

        public List<int[]> Map { get; set; }

    }




    [DataContract]
    public class ConfigData
    {


        [DataMember]
        public double Duration { get; set; }


        [DataMember]
        public double[] TimeMatrix { get; set; }
      
        [DataMember]
        public int NumberOfTasks { get; set; }

        [DataMember]
        public int NumberOfProcessors { get; set; }



    }
}

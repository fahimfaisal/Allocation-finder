using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using WcfServiceLibrary;

// NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService" in both code and config file together.
[ServiceContract]
public interface IGreedyService
{

	[OperationContract]
	[FaultContract(typeof(TimeoutFault))]
	AllocationData GreedyAlg(int deadline, ConfigData cd, int task);


}
[DataContract]
public class TimeoutFault
{
	[DataMember]
	public String Message { get; set; }

	public TimeoutFault(String message)
	{
		Message = message;
	}
}

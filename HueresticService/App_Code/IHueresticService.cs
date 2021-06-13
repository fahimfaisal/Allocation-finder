using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using WcfServiceLibrary;

[ServiceContract]
public interface IHueresticService
{

	[OperationContract]
	[FaultContract(typeof(TimeoutFault))]
	AllocationData HeuresticAlg(int deadline, ConfigData cd, int task);




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

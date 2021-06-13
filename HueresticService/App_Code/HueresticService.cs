using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using WcfServiceLibrary;

[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]

public class Service : IHueresticService
{
    public AllocationData HeuresticAlg(int deadline, ConfigData cd, int task)
    {

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();


        AllocationData data = new AllocationData();

        Double[,] timeMatrix = Transform1D(cd.TimeMatrix, cd.NumberOfProcessors, cd.NumberOfTasks);

        Double duration = cd.Duration;
        List<int[,]> allocationList = new List<int[,]>();
        List<int[]> transformedAllocation = new List<int[]>();
        int[] allocatedProcessor = new int[cd.NumberOfProcessors];




        for (int l = 0; l < cd.NumberOfProcessors; l++)
        {
            if (stopwatch.ElapsedMilliseconds > deadline)
            {
                TimeoutFault timeoutFault = new TimeoutFault("HeuristicService operation timed out");
                throw new FaultException<TimeoutFault>(timeoutFault);
            }
            else
            {
                Double[] times = new Double[cd.NumberOfProcessors];
                int[,] allocation = new int[cd.NumberOfProcessors, cd.NumberOfTasks];


                for (int k = timeMatrix.GetLength(0) - 1; k >= 0; k--)
                {

                    if (Allzero(allocatedProcessor))
                    {

                        if (timeMatrix[k, task] != -1)
                        {
                            times[k] += timeMatrix[k, task];

                            if (times[k] < duration)
                            {
                                allocatedProcessor[k] = 1;
                                allocation[k, task] = 1;
                                break;
                            }
                            else
                            {
                                times[k] -= timeMatrix[k, task];
                                allocation[k, task] = 0;
                            }
                        }
                        else
                        {
                            allocation[k, task] = 0;
                        }
                    }
                    else
                    {
                        if (timeMatrix[k, task] != -1 && allocatedProcessor[k] != 1)
                        {
                            times[k] += timeMatrix[k, task];

                            if (times[k] < duration)
                            {
                                allocatedProcessor[k] = 1;
                                allocation[k, task] = 1;
                                break;
                            }
                            else
                            {
                                times[k] -= timeMatrix[k, task];
                                allocation[k, task] = 0;
                            }
                        }
                        else
                        {
                            allocation[k, task] = 0;
                        }
                    }


                }
                for (int j = timeMatrix.GetLength(1) - 1; j >= 0; j--)
                {
                    if (j != task)
                    {

                        for (int i = timeMatrix.GetLength(0) - 1; i >= 0; i--)
                        {

                            if (timeMatrix[i, j] != -1)
                            {
                                times[i] += timeMatrix[i, j];

                                if (times[i] < duration)
                                {
                                    allocation[i, j] = 1;
                                    break;

                                }
                                else
                                {
                                    times[i] -= timeMatrix[i, j];
                                    allocation[i, j] = 0;

                                }

                            }
                            else
                            {
                                allocation[i, j] = 0;
                            }


                        }
                    }

                }

                if (validateAllocation(allocation))
                {
                    allocationList.Add(allocation);
                }

               

            }
        }







        foreach (int[,] all in allocationList)
        {
            int[] transformed = Transform2D(all);
            transformedAllocation.Add(transformed);
        }

        data.Map = transformedAllocation;

        return data;

    }




    private bool validateAllocation(int[,] array)
    {
        for (int i = 0; i < array.GetLength(1); i++)
        {
            bool found = false;

            for (int j = 0; j < array.GetLength(0); j++)
            {
                if (array[j,i] == 1)
                {
                    found = true;

                }

            }

            if (found == false)
            {
                return false;
            }
        }

        return true;
    }










    private bool Allzero(int[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] != 0)
            {
                return false;
            }


        }

        return true;
    }


    private Double[,] Transform1D(Double[] array, int rows, int columns)
    {
        int count = 0;

        Double[,] newArray = new Double[rows, columns];

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

    private int[] Transform2D(int[,] array)
    {
        int[] newArray = new int[array.GetLength(0) * array.GetLength(1)];
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

}

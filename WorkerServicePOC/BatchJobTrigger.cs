using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.Batch;
using Amazon.Batch.Model;
using Amazon.ECS;
using Amazon.ECS.Model;

using Amazon.CloudWatch;
using Task = System.Threading.Tasks.Task;

public class BatchTrigger
{
    public static async Task CheckBatch()
    {
        // AWS Batch configuration
        string batchJobDefinitionName = "arn:aws:batch:us-east-1:246970863856:job-definition/aws-job-fargate-definition:2";
        string batchJobQueueName = "arn:aws:batch:us-east-1:246970863856:job-queue/aws-fargate-ECS-poc-job-queues";

        // ECS configuration
        string ecsClusterName = "arn:aws:ecs:us-east-1:246970863856:cluster/AWSBatch-aws-batch-fargate-compute-e9baca74-8fd0-3aeb-89aa-391c780e4a5c";
        int desiredTaskCount = 1; // Number of tasks you expect to be busy before triggering the Batch job

        // AWS credentials and region
        var credentials = new Amazon.Runtime.BasicAWSCredentials("", "");
        var region = Amazon.RegionEndpoint.USEast1;

        // Create clients for Batch and ECS
        var batchClient = new AmazonBatchClient(credentials, region);
        var ecsClient = new AmazonECSClient(credentials, region);

        // Get the current number of running tasks in the ECS cluster



        //   var ecsTasksResponse1 = await ecsClient.ListTasksAsync();

        var ecsTasksResponse = await ecsClient.ListTasksAsync(new ListTasksRequest
        {
            Cluster = ecsClusterName,
            DesiredStatus = DesiredStatus.RUNNING,
            ServiceName = "batch-service" // this was added by sai
        });

        int runningTaskCount = ecsTasksResponse.TaskArns.Count;

        if (runningTaskCount >= desiredTaskCount)
        {
            // The number of running tasks is greater than or equal to the desired task count,
            // so trigger the AWS Batch job

            // Create the AWS Batch job
            var batchJobResponse = await batchClient.SubmitJobAsync(new SubmitJobRequest
            {
                JobName = "testJob",
                JobQueue = batchJobQueueName,
                JobDefinition = batchJobDefinitionName,
                ArrayProperties = new ArrayProperties
                {
                    Size = 3
                },
                Parameters = new Dictionary<string, string>
                    {
                        { "inputPayload", "saitej from job" }
                    },
                ContainerOverrides = new ContainerOverrides
                {
                    //Environment = new List<KeyValuePair<string, string>>
                    //{
                    //    new KeyValuePair<string, string>("ENV_VAR_NAME", "ENV_VAR_VALUE")
                    //},
                    Command = new List<string>
                {
                    "[\"dotnet\",\"WorkerServicePOC.dll\"]",
                }
                },
            });

            Console.WriteLine("AWS Batch job created. Job ID: " + batchJobResponse.JobId);
        }
        else
        {
            Console.WriteLine("No action required. Running task count: " + runningTaskCount);
        }
    }



    public void SubmitRequest()
    {

        var jobData = new Dictionary<string, string>
        {
            { "param1", "value1" },
            { "param2", "value2" },
            { "customData", "Your custom data here" }
        };

        var submitJobRequest = new SubmitJobRequest
        {
            JobDefinition = "your-job-definition-arn",
            JobName = "MyJob",
            JobQueue = "your-job-queue-name",
            Parameters = jobData,
            ContainerOverrides = new ContainerOverrides
            {
                //Environment = new List<KeyValuePair<string, string>>
                //{
                //    new KeyValuePair<string, string>("ENV_VAR_NAME", "ENV_VAR_VALUE")
                //},
                Command = new List<string>
                {
                    "[\"dotnet\",\"WorkerServicePOC.dll\"]",
                },
                Memory = 4096, // Specify the memory in MB
                Vcpus = 2 // Specify the number of vCPUs
            },
            RetryStrategy = new RetryStrategy
            {
                Attempts = 3 // Number of times to retry the job in case of failure
            }
        };
    }

    public static async Task ProcessJobQueue()
    {



        // AWS credentials and region
        var credentials = new Amazon.Runtime.BasicAWSCredentials("", "");
        var region = Amazon.RegionEndpoint.USEast1;

        // Create clients for Batch and ECS
        var batchClient = new AmazonBatchClient(credentials, region);
        var jobQueueName = "arn:aws:batch:us-east-1:246970863856:job-queue/aws-fargate-ECS-poc-job-queues";

        // Retrieve information about the job queue
        //var describeJobQueuesRequest = new ListJobsRequest
        //{
        //    JobQueue = jobQueueName
        //};


        var jobIds = new List<string> { "1da0119f-088b-4730-ac6b-705529c7535d" };

        var describeJobsRequest = new DescribeJobsRequest
        {
            Jobs = jobIds
        };

      //  var describeJobQueuesResponse = await batchClient.ListJobsAsync(describeJobQueuesRequest);


        var response = await batchClient.DescribeJobsAsync(describeJobsRequest);


        if (response.Jobs != null && response.Jobs.Count > 0)
        {
            var job = response.Jobs[0];
            var jobName = job.JobName;
            var parameters = job.Parameters;

            // Access the job parameters as needed
        }








        //  var jobs = describeJobQueuesResponse.JobQueues;
        // var jobQueue = describeJobQueuesResponse.JobQueues.FirstOrDefault();

        // if (jobQueue != null)
        // {
        //   var desiredvCPUs = jobQueue.ComputeEnvironmentOrder.FirstOrDefault()?.Order;
        // var availablevCPUs = jobQueue.Statistics["AVAILABLE"];

        // Determine if there are available ECS tasks
        //if (availablevCPUs >= desiredvCPUs)
        //{
        //    // Submit a job to the job queue
        //    SubmitBatchJob();
        //}
        // }
    }




    //below is using cloud wath 


    public async void TriggerBatchJobIfTasksAreBusy()
    {
        // Retrieve ECS cluster metrics from CloudWatch
        var cpuUtilization = await RetrieveClusterMetricAsync("CPUUtilization", "your-cluster-name");
        var memoryUtilization = await RetrieveClusterMetricAsync("MemoryUtilization", "your-cluster-name");
        var taskCount = await RetrieveClusterMetricAsync("TaskCount", "your-cluster-name");

        // Determine if ECS tasks are busy based on metrics
        if (cpuUtilization > 80 || memoryUtilization > 80 || taskCount > 10)
        {
            // Submit job to AWS Batch
            //  SubmitBatchJob();
        }
    }


    private async Task<double> RetrieveClusterMetricAsync(string metricName, string clusterName)
    {
        var cloudWatchClient = new AmazonCloudWatchClient("AWSAccessKey", "AWSSecretKey", Amazon.RegionEndpoint.USEast2);

        var request = new Amazon.CloudWatch.Model.GetMetricStatisticsRequest
        {
            Namespace = "AWS/ECS",
            MetricName = metricName,
            Dimensions = new List<Amazon.CloudWatch.Model.Dimension>
            {
                new Amazon.CloudWatch.Model.Dimension
                {
                    Name = "ClusterName",
                    Value = clusterName
                }
            },
            StartTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5)), // Adjust time range as needed
            EndTimeUtc = DateTime.UtcNow,
            Period = 60, // Adjust period as needed
            Statistics = new List<string> { "Average" }
        };

        // var response = cloudWatchClient.GetMetricStatistics(request);
        var response = await cloudWatchClient.GetMetricStatisticsAsync(request);
        var dataPoints = response.Datapoints;
        var metricValue = dataPoints.FirstOrDefault()?.Average ?? 0;

        return metricValue;
    }

    #region processJobData
    public void ProcessJob()
    {
        string param1 = Environment.GetEnvironmentVariable("param1");
        string param2 = Environment.GetEnvironmentVariable("param2");
        string customData = Environment.GetEnvironmentVariable("customData");

        // Use the job data for processing
        Console.WriteLine($"param1: {param1}");
        Console.WriteLine($"param2: {param2}");
        Console.WriteLine($"customData: {customData}");
    }


    #endregion

}

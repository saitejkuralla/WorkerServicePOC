//using Amazon.Batch;
//using Amazon.Batch.Model;
//using Amazon;
//using System.Linq;

//public class BatchJobProcessor
//{
//    private readonly AmazonBatchClient batchClient;

//    public BatchJobProcessor()
//    {
//        batchClient = new AmazonBatchClient(AWSAccessKey, AWSSecretKey, RegionEndpoint.YourRegion);
//    }

//    public void ProcessJobQueue()
//    {
//        var jobQueueName = "your-job-queue-name";

//        // Retrieve information about the job queue
//        var describeJobQueuesRequest = new DescribeJobQueuesRequest
//        {
//            JobQueues = new List<string> { jobQueueName }
//        };

//        var describeJobQueuesResponse = batchClient.DescribeJobQueues(describeJobQueuesRequest);
//        var jobQueue = describeJobQueuesResponse.JobQueues.FirstOrDefault();

//        if (jobQueue != null)
//        {
//            var desiredvCPUs = jobQueue.ComputeEnvironmentOrder.FirstOrDefault()?.Order;
//            var availablevCPUs = jobQueue.Statistics["AVAILABLE"];

//            // Determine if there are available ECS tasks
//            if (availablevCPUs >= desiredvCPUs)
//            {
//                // Submit a job to the job queue
//                SubmitBatchJob();
//            }
//        }
//    }

//    private void SubmitBatchJob()
//    {
//        var submitJobRequest = new SubmitJobRequest
//        {
//            JobDefinition = "your-job-definition-arn",
//            JobName = "MyJob",
//            JobQueue = "your-job-queue-name",
//            Parameters = new Dictionary<string, string>
//            {
//                { "param1", "value1" },
//                { "param2", "value2" }
//            }
//        };

//        var submitJobResponse = batchClient.SubmitJob(submitJobRequest);
//        string jobId = submitJobResponse.JobId;
//    }
//}



#region New code 31 check for sure 


//public async Task<bool> AreEcsTasksAvailableAsync(string clusterName)
//{
//    using (var client = new AmazonECSClient())
//    {
//        var request = new DescribeContainerInstancesRequest
//        {
//            Cluster = clusterName
//        };

//        var response = await client.DescribeContainerInstancesAsync(request);
//        var containerInstances = response.ContainerInstances;

//        // Check if there are available ECS tasks
//        foreach (var containerInstance in containerInstances)
//        {
//            if (containerInstance.RunningTasksCount < containerInstance.RegisteredTasksCount)
//            {
//                return true;
//            }
//        }

//        return false;
//    }
//}



//public async Task<string> SubmitBatchJobAsync(string jobDefinition, string jobQueue, string jobName)
//{
//    using (var client = new AmazonBatchClient())
//    {
//        var request = new SubmitJobRequest
//        {
//            JobDefinition = jobDefinition,
//            JobQueue = jobQueue,
//            JobName = jobName
//        };

//        var response = await client.SubmitJobAsync(request);

//        return response.JobId;
//    }
//}




//public async Task ProcessBatchJobsAsync()
//{
//    using (var client = new AmazonBatchClient())
//    {
//        var request = new ListJobsRequest
//        {
//            JobQueue = "YourJobQueue",
//            JobStatus = "SUCCEEDED"
//        };

//        var response = await client.ListJobsAsync(request);
//        var jobs = response.JobSummaryList;

//        foreach (var job in jobs)
//        {
//            // Process job output or perform required actions
//            var jobId = job.JobId;
//            // Retrieve job details using DescribeJobs API and process output
//        }
//    }
//}


//run this as main windows service 

//while (true)
//{
//    bool areTasksAvailable = await AreEcsTasksAvailableAsync("YourClusterName");

//    if (!areTasksAvailable)
//    {
//        // Submit jobs to AWS Batch
//        string jobId = await SubmitBatchJobAsync("YourJobDefinition", "YourJobQueue", "YourJobName");
//        // Wait for the job to complete
//        await WaitForJobCompletionAsync(jobId);
//    }
//    else
//    {
//        // Process Batch jobs
//        await ProcessBatchJobsAsync();
//    }

//    // Wait for a certain interval before checking task availability again
//    await Task.Delay(TimeSpan.FromSeconds(10));
//}


//public async Task WaitForJobCompletionAsync(string jobId)
//{
//    using (var client = new AmazonBatchClient())
//    {
//        while (true)
//        {
//            var request = new DescribeJobsRequest
//            {
//                Jobs = new List<string> { jobId }
//            };

//            var response = await client.DescribeJobsAsync(request);
//            var job = response.Jobs.FirstOrDefault();

//            if (job != null && job.Status == "SUCCEEDED")
//            {
//                // Job completed successfully
//                break;
//            }
//            else if (job != null && (job.Status == "FAILED" || job.Status == "STOPPED"))
//            {
//                // Handle job failure or stop scenarios
//                break;
//            }

//            // Wait for a certain interval before checking the job status again
//            await Task.Delay(TimeSpan.FromSeconds(10));
//        }
//    }
//}



//public async Task<bool> AreEcsTasksAvailableAsync(string clusterName)
//{
//    using (var client = new AmazonECSClient())
//    {
//        var request = new ListTasksRequest
//        {
//            Cluster = clusterName,
//            DesiredStatus = DesiredStatus.RUNNING
//        };

//        var response = await client.ListTasksAsync(request);

//        // Check if there are any running tasks in the cluster
//        if (response.TaskArns.Count > 0)
//        {
//            return true;
//        }

//        return false;
//    }
//}


#endregion





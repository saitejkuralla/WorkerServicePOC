using Amazon;
using Amazon.Batch.Model;
using Amazon.Batch;
using Amazon.ECS.Model;
using Amazon.ECS;
using Amazon.Runtime;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Newtonsoft.Json.Linq;
using System.Net;
using Task = System.Threading.Tasks.Task;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using System.Buffers.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WorkerServicePOC
{
    public class WorkerV1 : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private readonly AmazonStepFunctionsClient client;
        private readonly string accessKeyId;
        private readonly string secretAccessKey;

        public WorkerV1(ILogger<Worker> logger)
        {
            _logger = logger;
            accessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            secretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            AWSCredentials credentials = new BasicAWSCredentials("", "");
            RegionEndpoint region = RegionEndpoint.USEast1;
            client = new AmazonStepFunctionsClient(credentials, region);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("OSR Worker running at: {time}", DateTimeOffset.Now);

                await Start();
                await Task.Delay(2000, stoppingToken);
            }
        }


        public async Task Start()
        {

            while (true)
            {
                bool areTasksAvailable = await AreEcsTasksAvailableAsync("YourClusterName");

                if (!areTasksAvailable)
                {
                    // Submit jobs to AWS Batch
                    string jobId = await SubmitBatchJobAsync("YourJobDefinition", "YourJobQueue", "YourJobName");
                    // Wait for the job to complete
                    await WaitForJobCompletionAsync(jobId);
                }
                else
                {
                    // Process Batch jobs
                    await ProcessBatchJobsAsync();
                }

                // Wait for a certain interval before checking task availability again
                await Task.Delay(TimeSpan.FromSeconds(10));
            }

        }
  
        public async Task<string> SubmitBatchJobAsync(string jobDefinition, string jobQueue, string jobName)
        {
            using (var client = new AmazonBatchClient())
            {
                var request = new SubmitJobRequest
                {
                    JobDefinition = jobDefinition,
                    JobQueue = jobQueue,
                    JobName = jobName
                };

                var response = await client.SubmitJobAsync(request);

                return response.JobId;
            }
        }

        public async Task<bool> AreEcsTasksAvailableAsync(string clusterName)
        {
            using (var client = new AmazonECSClient())
            {
                var request = new ListTasksRequest
                {
                    Cluster = clusterName,
                    DesiredStatus = DesiredStatus.RUNNING
                };

                var response = await client.ListTasksAsync(request);

                // Check if there are any running tasks in the cluster
                if (response.TaskArns.Count > 0)
                {
                    return true;
                }
                return false;
            }
        }

        public async Task ProcessBatchJobsAsync()
        {
            using (var client = new AmazonBatchClient())
            {
                var request = new ListJobsRequest
                {
                    JobQueue = "YourJobQueue",
                    JobStatus = "SUCCEEDED"
                };

                var response = await client.ListJobsAsync(request);
                var jobs = response.JobSummaryList;

                foreach (var job in jobs)
                {
                    // Process job output or perform required actions
                    var jobId = job.JobId;
                    // Retrieve job details using DescribeJobs API and process output
                }
            }
        }

        public async Task WaitForJobCompletionAsync(string jobId)
        {
            using (var client = new AmazonBatchClient())
            {
                while (true)
                {
                    var request = new DescribeJobsRequest
                    {
                        Jobs = new List<string> { jobId }
                    };

                    var response = await client.DescribeJobsAsync(request);
                    var job = response.Jobs.FirstOrDefault();

                    if (job != null && job.Status == "SUCCEEDED")
                    {
                        // Job completed successfully
                        break;
                    }
                    else if (job != null && (job.Status == "FAILED" || job.Status == "STOPPED"))
                    {
                        // Handle job failure or stop scenarios
                        break;
                    }

                    // Wait for a certain interval before checking the job status again
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
            }
        }



        private string ProcessActivity(string input)
        {
            // Perform the necessary processing on the input
            // and return the output

            // For example:
            JObject outputJson = new()
            {
                ["message"] = "Processed: " + input
            };

            string output = outputJson.ToString();
            return output;
        }



    }
}

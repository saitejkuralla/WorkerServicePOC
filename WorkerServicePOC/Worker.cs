using Amazon;
using Amazon.ECS.Model;
using Amazon.Runtime;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;
using Task = System.Threading.Tasks.Task;

namespace WorkerServicePOC
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private readonly AmazonStepFunctionsClient client;
        private readonly string accessKeyId;
        private readonly string secretAccessKey;
        private static int numThreads = 5; // Number of threads to run

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            accessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            secretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            AWSCredentials credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
            RegionEndpoint region = RegionEndpoint.USEast1;
            client = new AmazonStepFunctionsClient(credentials, region);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("OSR Worker running at: {time}", DateTimeOffset.Now);

                Start();
                await Task.Delay(2000, stoppingToken);
            }
        }

        public async void Start()
        {
            GetActivityTaskResponse getActivityTaskResponse = new();

            while (true)
            {
                getActivityTaskResponse = client.GetActivityTaskAsync(new GetActivityTaskRequest
                {
                    WorkerName = "WorkerActivity",
                    ActivityArn = "arn:aws:states:us-east-1:246970863856:activity:WorkerActivity"
                }).GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(getActivityTaskResponse.TaskToken))
                {
                    break;
                }
                else
                {
                    // No tasks available, wait before polling again
                    Thread.Sleep(1000);
                }

            }

            _ = Task.Run(() => RunAsyncBlock(getActivityTaskResponse));

        }

         async Task RunAsyncBlock(GetActivityTaskResponse getActivityTaskResponse)
        {
            if (!string.IsNullOrEmpty(getActivityTaskResponse.TaskToken))
            {
                string hostName = Dns.GetHostName();
                string myIP = Dns.GetHostByName(hostName).AddressList[0].ToString();


                _logger.LogInformation("OSR Worker request processing started at: {time} IP: {dns}", DateTimeOffset.Now, myIP);
                // Perform the activity task here
                string taskToken = getActivityTaskResponse.TaskToken;
                string input = getActivityTaskResponse.Input;

                // Process the input and perform the necessary actions
                string output = ProcessActivity(input);

                // Complete the task by sending the output
                SendTaskSuccessRequest sendTaskSuccessRequest = new SendTaskSuccessRequest
                {
                    TaskToken = taskToken,
                    Output = output
                };


                //check threads 
                var tasks = new List<Task>();

                #region test code to check threads 
                // Create and start multiple threads 

                for (int i = 0; i < numThreads; i++)
                {
                    int threadId = i; // Capturing the current loop variable
                    var thread = new Thread(() => WorkerThread(threadId));
                    thread.Start();
                    tasks.Add(Task.Run(() => thread.Join()));
                }
                #endregion


                // Wait for all threads to complete
                Task.WaitAll(tasks.ToArray());


                client.SendTaskSuccessAsync(sendTaskSuccessRequest).GetAwaiter().GetResult();

                _logger.LogInformation("OSR Worker Processed the request at: {time}", DateTimeOffset.Now);

                _logger.LogInformation("input string from step function {value}", input);
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


        private void WorkerThread(int threadId)
        {
           // _logger.LogInformation($"Thread {threadId} started.");
            // Perform the work for each thread
            // Add your custom logic here

            Thread.Sleep(TimeSpan.FromSeconds(15)); // Simulate work

           // _logger.LogInformation($"Thread {threadId} completed.");
        }
    }
}
using Amazon;
using Amazon.ECS.Model;
using Amazon.Runtime;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
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
        private readonly int numThreads = 5;
        private readonly int numOfTasks = 6;
        private readonly int threadCount; // Number of threads to run
                                          //Allowing Maximum 3 tasks to be executed at a time
        private readonly SemaphoreSlim semaphoreSlim;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            accessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            secretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            threadCount = Convert.ToInt32(Environment.GetEnvironmentVariable("ThreadCount"));
            AWSCredentials credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
            RegionEndpoint region = RegionEndpoint.USEast1;
            client = new AmazonStepFunctionsClient(credentials, region);
            semaphoreSlim = new SemaphoreSlim(threadCount, threadCount);
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

            // asyncronus process handelling 
            _ = Task.Run(() => RunAsyncBlock(getActivityTaskResponse));

        }

        async Task RunAsyncBlock(GetActivityTaskResponse getActivityTaskResponse)
        {
            if (!string.IsNullOrEmpty(getActivityTaskResponse.TaskToken))
            {
                string hostName = Dns.GetHostName();
                string myIP = Dns.GetHostByName(hostName).AddressList[0].ToString();


                _logger.LogInformation("Tag 15 OSR Worker request processing started at: {time} IP: {dns}", DateTimeOffset.Now, myIP);
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
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                //check threads 
                var tasks = new List<Task>();

                #region test code to check threads 
                // Create and start multiple threads 
                for (int i = 0; i < numOfTasks; i++)
                {
                    int threadId = i; // Capturing the current loop variable
                    tasks.Add(Task.Run(async () =>
                    {
                        await semaphoreSlim.WaitAsync();
                        try
                        {
                            await WorkerThreadNew(threadId);
                        }
                        finally
                        {
                            semaphoreSlim.Release();

                        }
                    }));
                }
                #endregion


                // Wait for all threads to complete
                Task.WaitAll(tasks.ToArray());
                stopwatch.Stop();
                Console.WriteLine(
                    $"Processing of tasks Done in {stopwatch.ElapsedMilliseconds / 1000.0} Seconds , count {semaphoreSlim.CurrentCount}");

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

        private async Task WorkerThreadNew(int threadId)
        {
            await Task.Delay(TimeSpan.FromSeconds(20));
            _logger.LogInformation($"Thread {threadId} completed.");
        }
    }
}
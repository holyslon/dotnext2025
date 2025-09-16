using Amazon.S3;
using Amazon.S3.Model;
using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IO;

namespace NetworkingBot.Infrastructure;

public class LeaderboardHostedService(IServiceProvider serviceProvider, ILogger<LeaderboardHostedService> logger) : BackgroundService
{
    record LeaderBoardEntry(string TgUserId, string TgUserName, int Score);
    record LeaderBoardBatch(IReadOnlyCollection<LeaderBoardEntry> Entries, string? Previous,  string? Next);
    public class Options
    {
        public string? BucketName { get; set; }=null;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            try
            {
                using var scope = serviceProvider.CreateScope();

                var options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<Options>>();
                var bucketName = options.Value.BucketName;
                if (bucketName is null)
                {
                    logger.LogInformation("No bucket is configured, skipping execution.");
                    continue;
                }
                
                var lockProvider = scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
                var @lock = await lockProvider.TryAcquireLockAsync(nameof(LeaderboardHostedService), TimeSpan.FromSeconds(10), stoppingToken);
                if (@lock == null)
                {
                    logger.LogInformation("Fail to acquire lock");
                    continue;
                }

                await using (@lock)
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var meetings = from um in dbContext.UserToMeetings
                        join m in dbContext.Meetings on um.MeetingId equals m.Id
                        select new
                        {
                            m.Status,
                            um.UserId
                        };
                    var leaderBoard = meetings.AggregateBy(it => it.UserId, 0, (acc, it) => acc + it.Status)
                        .OrderByDescending(it => it.Value);
                    var leaderBoardWithUser = from l in leaderBoard
                        join u in dbContext.Users on l.Key equals u.Id
                        select new
                        {
                            u.TgUserId,
                            u.Username,
                            l.Value
                        };

                    var data = leaderBoardWithUser.AsAsyncEnumerable().Buffer(100);
                    var s3Client = scope.ServiceProvider.GetRequiredService<AmazonS3Client>();
                    var i = 0;
                    string? prevKey = null;
                    var msManager = scope.ServiceProvider.GetRequiredService<RecyclableMemoryStreamManager>();
                    await foreach (var item in data.WithCancellation(stoppingToken))
                    {
                        var key = i==0 ? "leaderboard" : $"leaderboard-{i}";
                        var nextKey = $"leaderboard-{i+1}";
                        var needNext = item.Count == 100;
                        await using var stream = msManager.GetStream();

                        var lb = new LeaderBoardBatch(
                            [..item.Select(it => new LeaderBoardEntry(it.TgUserId.ToString(), it.Username, it.Value))],
                            Previous: prevKey,
                            Next: needNext ? nextKey : null
                        );
                        
                        await System.Text.Json.JsonSerializer.SerializeAsync(stream, lb, cancellationToken: stoppingToken);
                        stream.Seek(0, SeekOrigin.Begin);
                        
                        await s3Client.PutObjectAsync(new PutObjectRequest
                        {
                            BucketName = bucketName,
                            Key = key,
                            AutoCloseStream = true,
                            InputStream = stream,
                            ContentType = "application/json"
                        }, stoppingToken);
                        i++;
                        prevKey = key;
                    }

                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in leaderboard hosted service");
            }
        }
    }
}
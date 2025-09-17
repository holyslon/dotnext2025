using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NetworkingBot.Domain;
using NetworkingBot.Handlers;
using Telegram.Bot.Types;
using Poll = NetworkingBot.Domain.Poll;
using User = NetworkingBot.Domain.User;

namespace NetworkingBot.Infrastructure;

internal class DbUser
{
    public long Id { get; set; }
    public required string Username { get; set; }
    public required long TgUserId { get; set; }
    public required long ChatId { get; set; }
    public required int State { get; set; }
    public required int ParticipationMode { get; set; }
}

internal class DbMeeting
{
    public long Id { get; set; }
    public required int Status { get; set; }
    public required DateTime CreatedAt { get; set; }
    [NotMapped]
    public StatusEnum TypedStatus
    {
        get => Status switch
        {
            1 => StatusEnum.Current,
            2 => StatusEnum.Cancelled,
            3 => StatusEnum.Finished,
            _ => StatusEnum.Unknown,
        };
        set
        {
            Status = value switch
            {
                StatusEnum.Unknown => 0,
                StatusEnum.Current => 1,
                StatusEnum.Cancelled => 2,
                StatusEnum.Finished => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
        }
    }

    internal enum StatusEnum
    {
        Unknown,
        Current,
        Cancelled,
        Finished
    }
}

internal class DbUserToMeeting
{
    public long Id { get; set; }
    public required long UserId { get; set; }
    public required long MeetingId { get; set; }
    
    public required bool FeedbackAvailable { get; set; }
}

internal class DbConversationTopic
{
    public long Id { get; set; }
    public required string Name { get; set; }
}

[Index(nameof(UserId))]
internal class DbUserToDbTopic
{
    public long Id { get; set; }
    public required long UserId { get; set; }
    public required long TopicId { get; set; }
}

[Index(nameof(UserId))]
[Index(nameof(MeetingId))]
internal class DbFeedback
{
    public long Id { get; set; }
    public required long UserId { get; set; }
    public required long MeetingId { get; set; }
    public required string Feedback { get; set; }
}


[Index(nameof(UserId))]
[Index(nameof(ExternalId))]
internal class DbPool
{
    public long Id { get; set; }
    public required long UserId { get; set; }
    public required string ExternalId { get; set; }
}

[Index(nameof(PoolId))]
internal class DbPoolToDbTopic
{
    public long Id { get; set; }
    public required long PoolId { get; set; }
    public required long TopicId { get; set; }
    public required int Index { get; set; }
}

public interface IApplicationClearer
{
    public ValueTask Clear(CancellationToken cancellationToken = default);
}

internal class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ILogger<ApplicationDbContext> logger)
    : DbContext(options), IUserStorage, IConversationTopicStorage, IPollStorage, IMatchService, IApplicationClearer, IMeetingStorage
{
    private class UserBackend(
        DbUser user,
        List<(DbUserToDbTopic, DbConversationTopic)> topics,
        DbSet<DbUserToDbTopic> userToDbTopics,
        DbMeeting? meeting) : IUserBackend
    {
        public DbUser Inner => user;
        public static async ValueTask<UserBackend> Create(DbUser user, ApplicationDbContext context, CancellationToken cancellationToken)
        {
            var topics = await (from userToTopic in context.UserToTopics
                    join topic in context.Topics on userToTopic.TopicId equals topic.Id
                    where userToTopic.UserId == user.Id
                        select new {userToTopic,topic}).ToListAsync(cancellationToken);
            
            var meeting = await (from userToMeeting in context.UserToMeetings
                join dbMeeting in context.Meetings on userToMeeting.MeetingId equals dbMeeting.Id
                where dbMeeting.Status == 1 && userToMeeting.UserId == user.Id
                select dbMeeting).FirstOrDefaultAsync(cancellationToken);
            return new UserBackend(user,[..topics.Select(i=>(i.userToTopic, i.topic))], context.UserToTopics, meeting);
        }
        
        public bool InMeeting => meeting != null;

        public bool IsActive
        {
            get => user.State is 1 or 3;
            set
            {
                if (value)
                    user.State = user.State switch
                    {
                        1 => 1,
                        2 => 3,
                        3 => 3,
                        _ => 1
                    };
                else
                    user.State = user.State switch
                    {
                        1 => 0,
                        2 => 2,
                        3 => 2,
                        _ => 1
                    };
            }
        }

        public User.ParticipationMode ParticipationMode
        {
            get => user.ParticipationMode switch
            {
                0 => User.ParticipationMode.Unknown,
                1 => User.ParticipationMode.Online,
                2 => User.ParticipationMode.Offline,
                _ => User.ParticipationMode.Unknown
            };
            set
            {
                user.ParticipationMode = value switch
                {
                    User.ParticipationMode.Unknown => 0,
                    User.ParticipationMode.Online => 1,
                    User.ParticipationMode.Offline => 2,
                    _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
                };
            }
        }

        public bool ReadyToParticipate
        {
            get => user.State is 2 or 3;
            set
            {
                if (value)
                    user.State = user.State switch
                    {
                        1 => 3,
                        2 => 2,
                        3 => 3,
                        _ => 2
                    };
                else
                    user.State = user.State switch
                    {
                        1 => 1,
                        2 => 0,
                        3 => 1,
                        _ => 0
                    };
            }
        }

        public long UserId => user.TgUserId;
        public string Name => user.Username;
        public IReadOnlyCollection<ConversationTopic> Topics => topics.Select(i=>Map(i.Item2)).ToImmutableList();
        public long ChatId => user.ChatId;

        public void UpdateTopics(ImmutableArray<ConversationTopic> newTopics)
        {
            var toAdd = newTopics.ExceptBy(
                topics.Select(t=>t.Item2.Id.ToString()), 
                topic => topic.Id).ToImmutableArray();
            var toRemove = topics.ExceptBy(
                newTopics.Select(t=>t.Id), 
                tuple => tuple.Item2.Id.ToString()).ToImmutableArray();

            foreach (var conversationTopic in toAdd)
            {
                var id = long.Parse(conversationTopic.Id);
                var dbUserToDbTopic = new DbUserToDbTopic()
                {
                    TopicId = id,
                    UserId = user.Id,
                };
                userToDbTopics.Add(dbUserToDbTopic);
                topics.Add((dbUserToDbTopic, new DbConversationTopic {Id = id, Name = conversationTopic.Name}));
            }

            foreach (var valueTuple in toRemove)
            {
                topics.Remove(valueTuple);
                userToDbTopics.Remove(valueTuple.Item1);
            }
        }
    }

    public required DbSet<DbUser> Users { get; set; }
    public required DbSet<DbConversationTopic> Topics { get; set; }
    public required DbSet<DbUserToDbTopic> UserToTopics { get; set; }
    public required DbSet<DbMeeting> Meetings { get; set; }
    public required DbSet<DbUserToMeeting> UserToMeetings { get; set; }
    public required DbSet<DbPool> Pools { get; set; }
    public required DbSet<DbPoolToDbTopic> PoolsTopics { get; set; }
    public required DbSet<DbFeedback> Feedbacks { get; set; }

    private static ConversationTopic Map(DbConversationTopic topic)
    {
        return new ConversationTopic(topic.Id.ToString(), topic.Name);
    }


    public async ValueTask<User> GetUserAsync(long userId, CancellationToken cancellationToken)
    {
        var dbUser = await Users.Where(u => u.TgUserId == userId).FirstOrDefaultAsync(cancellationToken);

        if (dbUser == null) throw new IUserStorage.UserNotFound(userId);

        return new User(await UserBackend.Create(dbUser, this, cancellationToken));
    }

    public async ValueTask<bool> WithCreateOrGetUser(Chat chat, Telegram.Bot.Types.User user, Func<User, ValueTask<bool>> action,
        CancellationToken cancellationToken = default)
    {
        var chatId = chat.Id;
        var userId = user.Id;
        await using var tx = await Database.BeginTransactionAsync(cancellationToken);
        var dbUser = await Users.Where(u => u.TgUserId == userId && u.ChatId == chatId)
            .FirstOrDefaultAsync(cancellationToken);
        if (dbUser == null)
        {
            dbUser = new DbUser
            {
                Username = user.Username ?? user.FirstName,
                TgUserId = user.Id,
                ChatId = chat.Id,
                State = 0,
                ParticipationMode = 0
            };
            Users.Add(dbUser);
            await SaveChangesAsync(cancellationToken);
        }

        var domainUser = new User(await UserBackend.Create(dbUser, this, cancellationToken));

        var res = await action(domainUser);
        await UserToMeetings.Where(m=>m.UserId == dbUser.Id).ExecuteUpdateAsync(
            um=>um.SetProperty(i => i.FeedbackAvailable, false), cancellationToken);

        await SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return res;
    }

    public async ValueTask<bool> WithGetUser(long chatId, Func<User, ValueTask<bool>> action,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await Database.BeginTransactionAsync(cancellationToken);
        var dbUser = await Users.Where(u => u.ChatId == chatId)
            .FirstOrDefaultAsync(cancellationToken);
        if (dbUser == null) throw new IUserStorage.UserNotFound(chatId);
        var domainUser = new User(await UserBackend.Create(dbUser, this, cancellationToken));

        var res = await action(domainUser);
        await UserToMeetings.Where(m=>m.UserId == dbUser.Id).ExecuteUpdateAsync(
            um=>um.SetProperty(i => i.FeedbackAvailable, false), cancellationToken);
        await SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return res;
    }

    public async ValueTask<bool> Save(User domainPoll, CancellationToken cancellationToken = default)
    {
        await SaveChangesAsync(cancellationToken);
        return true;
    }

    public async ValueTask<IReadOnlyList<ConversationTopic>> GetTopics(CancellationToken cancellationToken = default)
    {
        return (await Topics.ToListAsync(cancellationToken)).Select(Map).ToImmutableList();
    }
    

    public async ValueTask Save(User user, string poolId, IReadOnlyList<ConversationTopic> topics, CancellationToken cancellationToken)
    {
        if (user.Storage is UserBackend userStorage)
        {
            var pool = new DbPool
            {
                ExternalId = poolId,
                UserId = userStorage.Inner.Id
            };
            Pools.Add(pool);
            await SaveChangesAsync(cancellationToken);
            foreach (var tuple in topics.Index())
            {
                var (index, topic) = tuple;
                PoolsTopics.Add(new DbPoolToDbTopic
                {
                    PoolId = pool.Id,
                    TopicId = long.Parse(topic.Id),
                    Index = index
                });
                
            }
            await UserToMeetings.Where(m=>m.UserId == userStorage.Inner.Id).ExecuteUpdateAsync(
                um=>um.SetProperty(i => i.FeedbackAvailable, false), cancellationToken);

            await SaveChangesAsync(cancellationToken);
        }
        else
        {
            logger.LogWarning("User from unknown storage, pool not saved");
        }
    }

    public async ValueTask<Poll> GetById(string eventPayloadPollId, CancellationToken cancellationToken)
    {
        var pool = await ((from dbPool in Pools
            join dbUser in Users on dbPool.UserId equals dbUser.Id 
            where dbPool.ExternalId == eventPayloadPollId
            select new {dbPool, dbUser}).FirstOrDefaultAsync(cancellationToken));
        if (pool == null)
        {
            throw new IPollStorage.PoolNotFound(eventPayloadPollId);
        }
        
        var dbTopics = await (from poolsTopic in PoolsTopics 
            join topic in  Topics on poolsTopic.TopicId equals topic.Id
            where poolsTopic.PoolId == pool.dbPool.Id
            orderby poolsTopic.Index 
            select new {poolsTopic.Index, topic}).ToListAsync(cancellationToken);
        
        
        var topics = new List<ConversationTopic>(dbTopics.Count);
        foreach (var tuple in dbTopics)
        {
            topics.Insert(tuple.Index, Map(tuple.topic));
        }
        
        return new Poll(new User(await UserBackend.Create(pool.dbUser,  this, cancellationToken)), pool.dbPool.ExternalId, [..topics]);

    }

    public async ValueTask<(bool, Meeting? meeting)> TryFindMatch(User.SearchInfo searchInfo,
        CancellationToken cancellationToken)
    {
        using var _ = logger.BeginScope(new { searchInfo });
        var user = await Users.Where(u=>u.TgUserId == searchInfo.UserId).FirstOrDefaultAsync(cancellationToken: cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User not found");
            return (false, null);
        }

        if (user.State != 3)
        {
            logger.LogWarning("User not ready to participate");
            return (false, null);
        }

        var targetParticipation = searchInfo.ParticipationMode switch
        {
            User.ParticipationMode.Unknown => (false, 0),
            User.ParticipationMode.Online => (true,1), 
            User.ParticipationMode.Offline => (true,2),
            _ => throw new ArgumentOutOfRangeException()
        };

        var currentMeeting = from um in UserToMeetings
            join m in Meetings on um.MeetingId equals m.Id 
            where um.UserId == user.Id && m.Status == 1
                select m;

        if (await currentMeeting.AnyAsync(cancellationToken: cancellationToken))
        {
            return (false, null);
        }
        
        var inMeetings = 
            from m in Meetings
            join mu in UserToMeetings on m.Id equals mu.MeetingId
            where m.Status == 1
            select mu.UserId;
        
        var score = 
            from mu in UserToMeetings
            where mu.UserId == user.Id
            join m in Meetings on mu.MeetingId equals m.Id
            join muo in UserToMeetings.Where(it=>it.UserId != user.Id) on m.Id equals muo.MeetingId
            where m.Status != 1
            select new {
                mu.UserId,
                m.Status 
                };


        IQueryable<DbUser> candidates;
        if (targetParticipation.Item1)
        {
            candidates = from u in Users
                from s in score.Where(it => it.UserId == u.Id).DefaultIfEmpty()
                where !inMeetings.Contains(u.Id) && u.Id != user.Id && u.ParticipationMode == targetParticipation.Item2 && u.State == 3
                orderby s.Status
                select u;
        }
        else
        {
            candidates = from u in Users
                from s in score.Where(it => it.UserId == u.Id).DefaultIfEmpty()
                where !inMeetings.Contains(u.Id) && u.Id != user.Id && u.State == 3
                orderby s.Status
                select u;
        }

        var candidate = candidates.FirstOrDefault();
        if (candidate == null)
        {
            return (false, null);
        }
        
        await using var tx = await Database.BeginTransactionAsync(cancellationToken);
        var dbMeeting = new DbMeeting
        {
            Status = 1,
            CreatedAt = DateTime.UtcNow
        };
        Meetings.Add(dbMeeting);
        await SaveChangesAsync(cancellationToken);
        UserToMeetings.Add(new DbUserToMeeting
        {
            UserId = user.Id,
            MeetingId = dbMeeting.Id,
            FeedbackAvailable = false,
        });
        UserToMeetings.Add(new DbUserToMeeting
        {
            UserId = candidate.Id,
            MeetingId = dbMeeting.Id,
            FeedbackAvailable = false,
        });
        user.State = 1;
        candidate.State = 1;
        await UserToMeetings.Where(m=>m.UserId == user.Id).ExecuteUpdateAsync(
            um=>um.SetProperty(i => i.FeedbackAvailable, false), cancellationToken);
        await UserToMeetings.Where(m=>m.UserId == candidate.Id).ExecuteUpdateAsync(
            um=>um.SetProperty(i => i.FeedbackAvailable, false), cancellationToken);

        await SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return (true, new Meeting(new MeetingBackend(dbMeeting, MeetingUserWithDbId.Create(user), MeetingUserWithDbId.Create(candidate), MeetingUserWithDbId.Create(user), [MeetingUserWithDbId.Create(candidate)],this)));
    }

    public async ValueTask Clear(CancellationToken cancellationToken)
    {
       await Users.ExecuteDeleteAsync(cancellationToken);
       await UserToMeetings.ExecuteDeleteAsync(cancellationToken);
       await UserToTopics.ExecuteDeleteAsync(cancellationToken);
       await Meetings.ExecuteDeleteAsync(cancellationToken);
       await Pools.ExecuteDeleteAsync(cancellationToken);
       await PoolsTopics.ExecuteDeleteAsync(cancellationToken);
       await SaveChangesAsync(cancellationToken);
    }

    record MeetingUserWithDbId(Meeting.User MeetingUser, long DbId)
    {
        public static MeetingUserWithDbId Create(DbUser dbUser)
        {
            return new MeetingUserWithDbId(new Meeting.User(new User.LinkData(dbUser.TgUserId, dbUser.Username), dbUser.ChatId, false), dbUser.Id);
        }
    };
    
    private class MeetingBackend(DbMeeting meeting, MeetingUserWithDbId one, MeetingUserWithDbId another, MeetingUserWithDbId source, IReadOnlyCollection<MeetingUserWithDbId> other, ApplicationDbContext context) : IMeetingBackend
    {
        public Meeting.User One => one.MeetingUser;
        public Meeting.User Another => another.MeetingUser;
        public IEnumerable<Meeting.User> OtherUsers => other.Select(it=>it.MeetingUser);
        public bool InProgress => meeting.Status == 1;
        public Meeting.User Source => source.MeetingUser;
        public bool IsCompleted => meeting.Status > 1;

        public ValueTask Cancel(CancellationToken cancellationToken)
        {
            return context.CancelMeeting(meeting,  cancellationToken);
        }

        public ValueTask Complete(CancellationToken cancellationToken)
        {
            return context.CompleteMeeting(meeting,  cancellationToken);
        }

        public ValueTask SubmitFeedback(string? eventPayloadText, CancellationToken cancellationToken)
        {
            return context.SubmitFeedback(meeting, source, eventPayloadText, cancellationToken);
        }
    }

    private async ValueTask SubmitFeedback(DbMeeting meeting, MeetingUserWithDbId source, string? eventPayloadText,
        CancellationToken cancellationToken)
    {
        await using var tx = await Database.BeginTransactionAsync(cancellationToken);
        Feedbacks.Add(new DbFeedback
        {
            MeetingId = meeting.Id,
            UserId = source.DbId,
            Feedback = eventPayloadText?? ""
        });
        await UserToMeetings.Where(m=>m.MeetingId == meeting.Id).ExecuteUpdateAsync(
            um=>um.SetProperty(i => i.FeedbackAvailable, false), cancellationToken);

        await SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private async ValueTask CompleteMeeting(DbMeeting meeting, CancellationToken cancellationToken)
    {
        await using var tx = await Database.BeginTransactionAsync(cancellationToken);
        
        meeting.TypedStatus = DbMeeting.StatusEnum.Finished;
        
        await UserToMeetings.Where(m=>m.MeetingId == meeting.Id).ExecuteUpdateAsync(
            um=>um.SetProperty(i => i.FeedbackAvailable, true), cancellationToken);
        
        await SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private async ValueTask CancelMeeting(DbMeeting meeting, CancellationToken cancellationToken)
    {
        await using var tx = await Database.BeginTransactionAsync(cancellationToken);
        
        meeting.TypedStatus = DbMeeting.StatusEnum.Cancelled;
        await UserToMeetings.Where(m=>m.MeetingId == meeting.Id).ExecuteUpdateAsync(
            um=>um.SetProperty(i => i.FeedbackAvailable, true), cancellationToken);
        
        await SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async ValueTask<bool> WithMeetingForUser(Chat chat, Telegram.Bot.Types.User user, Func<Meeting, ValueTask<bool>> action, CancellationToken cancellationToken)
    {
        var currentMeeting = from um in UserToMeetings
            join u in Users on um.UserId equals u.Id
            join m in Meetings on um.MeetingId equals m.Id 
            where u.TgUserId == user.Id && u.ChatId == chat.Id 
            orderby m.CreatedAt descending
                                        // && m.Status == 1
            select m;
        
        var meeting = await currentMeeting.FirstOrDefaultAsync(cancellationToken);
        if (meeting == null)
        {
            using var _ = logger.BeginScope(new { chat, user });
            logger.LogInformation("meeting not found");
            return false;
        }
        var usersQuery = from m in Meetings
            join um in UserToMeetings on m.Id equals um.MeetingId 
            join u in Users on um.UserId equals u.Id
            where m.Id == meeting.Id
                select new MeetingUserWithDbId(new Meeting.User(new User.LinkData(u.TgUserId, u.Username), u.ChatId, um.FeedbackAvailable), u.Id);
        var users = await usersQuery.ToListAsync(cancellationToken);
        var source = users.First(u=>u.MeetingUser.ChatId == chat.Id);
        var rest = users.Where(u => u.MeetingUser.ChatId != chat.Id);
        var domain = new Meeting(new MeetingBackend(meeting, users.First(), users.Skip(1).First(), source, [..rest], this));
        return await action(domain);
    }

    public async ValueTask<bool> WithMeetingForChat(long chatId, Func<Meeting, ValueTask<bool>> action, CancellationToken cancellationToken)
    {
        var currentMeeting = from um in UserToMeetings
            join u in Users on um.UserId equals u.Id
            join m in Meetings on um.MeetingId equals m.Id 
            where u.ChatId == chatId 
                  // && m.Status == 1
            orderby m.CreatedAt descending
            select m;
        
        var meeting = await currentMeeting.FirstOrDefaultAsync(cancellationToken);
        if (meeting == null)
        {
            using var _ = logger.BeginScope(new { chatId });
            logger.LogInformation("meeting not found");
            return false;
        }
        var usersQuery = from m in Meetings
            join um in UserToMeetings on m.Id equals um.MeetingId 
            join u in Users on um.UserId equals u.Id
            where m.Id == meeting.Id
            select new MeetingUserWithDbId(new Meeting.User(new User.LinkData(u.TgUserId, u.Username), u.ChatId, um.FeedbackAvailable), u.Id);
        var users = await usersQuery.ToListAsync(cancellationToken);
        var source = users.First(u=>u.MeetingUser.ChatId == chatId);
        var rest = users.Where(u => u.MeetingUser.ChatId != chatId);
        var domain = new Meeting(new MeetingBackend(meeting, users.First(), users.Skip(1).First(),source, [..rest],  this));
        return await action(domain);
    }
}
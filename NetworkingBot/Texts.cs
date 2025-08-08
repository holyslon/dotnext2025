using NetworkingBot.Commands;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = NetworkingBot.Domain.User;

namespace NetworkingBot;

public static class Texts
{
    
    
    public static string Welcome(JoinCommand command, PostponeCommand laterButton) => $"Welcome. To join press {command.Text}. To postpone press {laterButton.Text}.";
    public static string ChooseOnlineOrOffline(OnlineCommand onlineButton, OfflineCommand offlineButton) => $"To online press {onlineButton.Text}. To offline press {offlineButton.Text}.";
    public static string WaitingForYouToReturn(JoinCommand command) => $"Sad. If yo whant come back just type {command.SlashCommand()}";
    public static string ChooseYourInterests() => "Choose your interests.";
    public static string Online() => "Welcome";
    public static string Offline() => "Welcome";
    public static string ChooseTheme() => "Welcome";
    public static string Confirm() => "Welcome";
    public static string FoundAPair() => "Welcome";
    public static string SearchingForAPair() => "Welcome";
    public static string WelcomeMessage() => "Welcome";


    public static string YesButton() => "yes";
    public static string SubmitInterestsButton() => "submit";
    public static string LaterButton() => "may be later";
    public static string OnlineButton() => "online";
    public static string OfflineButton() => "offline";

    public static string ReadyForMeetingButton() => "Ready";

    public static string WaitForNextMatch(PostponeCommand postpone) => "We will contact with you when we find next pair for you";

    public static string MeetingHappenButton()=> "meeting_happen";

    public static string MeetingCanceledButton()=> "meeting_canceled";

    public static MatchMessageType MatchMessage => new();

    public class MatchMessageType
    {
        public string Text(User.LinkData user, MeetingHappenCommand meetingHappenCommand, MeetingCanceledCommand meetingCanceledCommand) => $"Hello we find a person to have coffee with for you. Just dm to {user.ToHtmlLink()}. When you finish just press {meetingHappenCommand.Text} for return to matching. If you dont - just press {meetingCanceledCommand.Text} and we cancel the meeting";

        public ParseMode ParseMode => ParseMode.Html;
    }

    public static string AddReviewButton()=> "add_review";
    

    public static string MeetingCompleted(ReadyForMeeting readyForMeeting, PostponeCommand postpone)
    {
        return "Meeting completed";
    }

    public static string MeetingCanceled(ReadyForMeeting readyForMeeting, PostponeCommand postpone)
    {
        return "Meeting canceled";
    }
}

internal static class LinkDataExtensions
{
    public static string ToHtmlLink(this User.LinkData data)
    {
        return $"<a href='tg://user?id={data.UserId}'>{data.Name}</a>";
    }
}

public static class Interests
{
    public static string DotNet => "DotNet";
    public static string PostgreSql => "PostgreSql";
    public static string Async => "Async";
}
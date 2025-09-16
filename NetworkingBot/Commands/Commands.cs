namespace NetworkingBot.Commands;

public static class Commands
{
    public static JoinCommand Join => new();
    public static PostponeCommand Postpone => new();
    public static OnlineCommand Online => new();
    public static OfflineCommand Offline => new();
    public static ReadyForMeeting ReadyForMeeting => new();
    public static MeetingHappenCommand MeetingHappenCommand => new();
    public static MeetingCanceledCommand MeetingCanceledCommand => new();
}

public class StartCommand : ISlashCommand
{
    public string Action => "start";
}

public class JoinCommand() : SlashAndInlineSlashCommand(Texts.YesButton(), "join");

public class PostponeCommand() : SlashAndInlineSlashCommand(Texts.LaterButton(), "postpone");

public class OnlineCommand() : SlashAndInlineSlashCommand(Texts.OnlineButton(), "online");

public class OfflineCommand() : SlashAndInlineSlashCommand(Texts.OfflineButton(), "offline");

public class ReadyForMeeting() : SlashAndInlineSlashCommand(Texts.ReadyForMeetingButton(), "ready_for_meeting");

public class MeetingHappenCommand() : SlashAndInlineSlashCommand(Texts.MeetingHappenButton(), "meeting_happen");

public class MeetingCanceledCommand() : SlashAndInlineSlashCommand(Texts.MeetingCanceledButton(), "meeting_canceled");
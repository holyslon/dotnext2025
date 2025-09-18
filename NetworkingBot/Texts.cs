using NetworkingBot.Commands;
using NetworkingBot.Domain;
using Telegram.Bot.Types.Enums;

namespace NetworkingBot;

public static class Texts
{
    public static string Welcome(JoinCommand command, JustWatching laterButton)
    {
        return $"Привет! Если готов(а) познакомиться с кем-нибудь, нажми {command.Text.Bold().Quoted()}. Если просто наблюдаешь - выбери {laterButton.Text.Bold().Quoted()} 😊";
    }

    public static string ChooseOnlineOrOffline(OnlineCommand onlineButton, OfflineCommand offlineButton)
    {
        return $"Отлично 🎉 Уже ищу тебе собеседника! А пока расскажи - ты на площадке или онлайн?";
    }

    public static string WaitingForYouToReturn(JoinCommand command)
    {
        return $"Грустно. Но если передумаешь - набери {command.SlashCommand()} и я начну поиск пары для тебя";
    }
    public static string WaitingForYouToReturn(ReadyForMeeting command)
    {
        return $"Грустно. Но если передумаешь - набери {command.SlashCommand()} и я начну поиск пары для тебя";
    }
    public static string ChooseYourInterests()
    {
        return "Выбери о чем тебе интересно поговорить";
    }


    public static string YesButton()
    {
        return "Да";
    }

    public static string LaterButton()
    {
        return "Может позже";
    }

    public static string OnlineButton()
    {
        return "Онлайн";
    }

    public static string OfflineButton()
    {
        return "На площадке";
    }

    public static string ReadyForMeetingButton()
    {
        return "Готов";
    }

    public static string WaitForNextMatch(PostponeCommand postpone)
    {
        return $"Я напишу, как только найду для тебя следующую пару. Если не готов пока общаться, просто нажми {postpone.Text.Bold().Quoted()}";
    }

    public static string MeetingHappenButton()
    {
        return "Встреча состоялась";
    }

    public static string MeetingCanceledButton()
    {
        return "Не встретились";
    }

    public static MatchMessageType MatchMessage => new();
    public static MessageFromUserType MessageFromUser => new();

    public class MatchMessageType
    {
        public string Text(string baseUrl, IUser.LinkData user, MeetingHappenCommand meetingHappenCommand,
            MeetingCanceledCommand meetingCanceledCommand)
        {
            return $"""
                    🎉 Ура! Мы нашли тебе собеседника.

                    Напиши пользователю {user.ToHtmlLink()}.
                    Не стесняйся предложить место для встречи: кофебрейк-зона, какой-то стенд для ориентира или улица у площадки ☀️

                    Когда встретитесь, нажми кнопку {meetingHappenCommand.Text.Bold().Quoted()} — я анонимно посчитаю новые знакомства 😊

                    Если встреча не случилась — жми {meetingCanceledCommand.Text.Bold().Quoted()}, и я подберу нового собеседника 👌

                    ⚠️ Если ник не кликается — у собеседника включена приватность. Просто напиши сюда, и он увидит сообщение в чате с ботом.
                    """;
        }

        public ParseMode ParseMode => ParseMode.Html;
    }
    
    public class MessageFromUserType
    {
        public string Text(IUser.LinkData user, string? originalMessage)
        {
            return
                $"Пользователь {user.ToHtmlLink()} отправил сообщение: {originalMessage}";
        }

        public ParseMode ParseMode => ParseMode.Html;
    }


    public static string MeetingCompleted(ReadyForMeeting readyForMeeting, PostponeCommand postpone)
    {
        return $"Встреча закончена. Если готов к следующей, просто нажми {readyForMeeting.Text.Bold().Quoted()}. Если не готов пока общаться, нажми {postpone.Text.Bold().Quoted()}. Так же я буду тебе очень благодарен, если ты оставишь обратную связь по встрече. Ты можешь просто написать сообщение и я все увижу ☺️";
    }
    public static string ThankYouForFeedBack(ReadyForMeeting readyForMeeting, PostponeCommand postpone)
    {
        return $"Спасибо огромное за обратную связь ❤️ Она помогает мне стать лучше! А теперь, если готов к следующей встрече, просто нажми {readyForMeeting.Text.Bold().Quoted()}, ну а если пока не готов общаться, нажми {postpone.Text.Bold().Quoted()}";
    }
    public static string MeetingCanceled(ReadyForMeeting readyForMeeting, PostponeCommand postpone)
    {
        return $"Встреча отменена. Мне жаль что так получилось. Если готов к следующей, просто нажми {readyForMeeting.Text.Bold().Quoted()}. Если не готов пока общаться, нажми {postpone.Text.Bold().Quoted()}. Также я буду тебе очень благодарен, если ты оставишь обратную связь по встрече. Ты можешь просто написать сообщение и я все увижу ☺️";
    }
}

internal static class StringExtensions
{
    public static string Bold(this string data)
    {
        return $"<b>{data}</b>";
    }
    public static string Quoted(this string data)
    {
        return $"\"{data}\"";
    }
}

internal static class LinkDataExtensions
{
    public static string ToHtmlLink(this IUser.LinkData data)
    {
        // return $"<a href='{baseUrl}/user/{data.UserId}'>{data.Name}</a>";
        return $"<a href='tg://user?id={data.UserId}'>{data.Name}</a>";
    }
}

public static class Interests
{
    public static string DotNet => "DotNet";
    public static string PostgresSql => "Без темы";
    public static string Async => "Architecture";
}
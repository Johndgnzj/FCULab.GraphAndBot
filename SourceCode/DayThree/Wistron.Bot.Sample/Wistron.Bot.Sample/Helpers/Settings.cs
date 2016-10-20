using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Wistron.Bot.Sample.Helpers
{
    public static class Settings
    {
        public static string SendMessageUrl = @"https://graph.microsoft.com/v1.0/me/microsoft.graph.sendmail";
        public static string GetMeUrl = @"https://graph.microsoft.com/v1.0/me";
        public static string GetContactsUrl = @"https://graph.microsoft.com/v1.0/me/contacts/";
        public static string GetEventsUrl = @"https://graph.microsoft.com/v1.0/me/events/";
        public static string PostEventsUrl = @"https://graph.microsoft.com/v1.0/me/calendar/events/";
        public static string PostEmailUrl = @"https://graph.microsoft.com/v1.0/me/sendMail";
        public static string GetOneDriveUrl = @"https://graph.microsoft.com/v1.0/me/drive/root/children";
        public static string CreateOneDriveUrl = @"https://graph.microsoft.com/v1.0/me/drive/root:/{0}:/content";
        public static string SearchOneDriveUrl = @"https://graph.microsoft.com/v1.0/me/drive/root/search(q='{0}')";

        public static string OutlookUrl = @"https://outlook.office.com/";

        public const int ThreeLineButtonConst = 3;
        public const int FiveLineButtonConst = 5;
        public const int SixLineButtonConst = 6;
    }
}
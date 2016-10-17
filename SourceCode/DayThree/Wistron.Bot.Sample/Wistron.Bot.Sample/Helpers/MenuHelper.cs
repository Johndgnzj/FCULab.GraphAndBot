using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Wistron.Bot.Sample.Helpers
{
    public class MenuHelper
    {
        public enum MainMenu { CANCEL = 0, TOKEN = 1, ME = 2, GETCONTACT = 3, GETCALENDAR = 4, GETEMAIL = 5, SENDEMAIL = 6, CREATEEVENT = 7, LOGON = 8, LOGOUT = 9, GETONECONTACT = 10, GETONEDRIVE = 11 }
        /// <summary>
        /// 取得(登入/未登入)主選單
        /// </summary>
        /// <param name="IsLogin"></param>
        /// <returns></returns>
        public static Dictionary<String, String> GetMainMenu(bool IsLogin)
        {
            Dictionary<String, String> menu = new Dictionary<String, String>();
            if (IsLogin)
            {
                menu.Add(MainMenu.TOKEN.ToString(), "Show Token");
                menu.Add(MainMenu.ME.ToString(), "Show My Info");
                menu.Add(MainMenu.GETCONTACT.ToString(), "Get Contact List");
                menu.Add(MainMenu.GETCALENDAR.ToString(), "Get Calendar Event List");
                menu.Add(MainMenu.GETEMAIL.ToString(), "Get Email List");
                menu.Add(MainMenu.GETONEDRIVE.ToString(), "Get OneDrive List");
                menu.Add(MainMenu.SENDEMAIL.ToString(), "Send Email");
                menu.Add(MainMenu.CREATEEVENT.ToString(), "Create an Event");
                menu.Add(MainMenu.LOGOUT.ToString(), "Sign out");
            }
            else { menu.Add(MainMenu.LOGON.ToString(), "Sign in your account"); }
            menu.Add(MainMenu.CANCEL.ToString(), "Cancel");
            return menu;
        }
    }
}
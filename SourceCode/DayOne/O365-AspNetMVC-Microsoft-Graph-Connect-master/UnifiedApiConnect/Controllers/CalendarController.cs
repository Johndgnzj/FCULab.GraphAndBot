using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using UnifiedApiConnect.Helpers;
using UnifiedApiConnect.Models;

namespace UnifiedApiConnect.Controllers
{
    public class CalendarController : Controller
    {
        // GET: Calendar
        public ActionResult Index(UserInfo userInfo)
        {
            EnsureUser(ref userInfo);

            ViewBag.UserInfo = userInfo;

            return View();
        }

        /// <summary>
        /// 取得Calender Event List
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> GetEventInfoList()
        {
            if (string.IsNullOrEmpty((string)Session[SessionKeys.Login.AccessToken])) return RedirectToAction(nameof(Index), "Home");

            List<EventInfo> myDataModelList = new List<EventInfo>();

            myDataModelList = await UnifiedApiHelper.GetEventInfoAsync((string)Session[SessionKeys.Login.AccessToken]);

            return View(myDataModelList);
        }
        /// <summary>
        /// 在Calendar上建立一個Event
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> CreateEvent()
        {
            if (string.IsNullOrEmpty((string)Session[SessionKeys.Login.AccessToken])) return RedirectToAction(nameof(Index), "Home");
            var currentUser = (UserInfo)Session[SessionKeys.Login.UserInfo];

            var ret = await UnifiedApiHelper.CreateEventAsync((string)Session[SessionKeys.Login.AccessToken], 
                getCalendarEventContents(currentUser.Name, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), currentUser.Name, currentUser.Address));

            //return View(myDataModelList);
            return RedirectToAction("GetEventInfoList", "Calendar");

        }
        // Use the login user name or recipient email address if no user name.
        void EnsureUser(ref UserInfo userInfo)
        {
            var currentUser = (UserInfo)Session[SessionKeys.Login.UserInfo];

            if (userInfo == null || string.IsNullOrEmpty(userInfo.Address))
            {
                userInfo = currentUser;
            }
            else if (userInfo.Address.Equals(currentUser.Address, StringComparison.OrdinalIgnoreCase))
            {
                userInfo = currentUser;
            }
            else
            {
                userInfo.Name = userInfo.Address;
            }
        }
        public string getCalendarEventContents(string name, DateTime startDatetime, DateTime endDatetime, string attendeName, string attendeEmail)
        {
            var a = new { emailAddress = new { name = attendeName, address = attendeEmail } };
            List<Object> attendees = new List<object> { a };
            JObject o = JObject.FromObject(new
            {
                subject = "透過程式建立的事件",
                isAllDay = false,
                start = new
                {
                    dateTime = startDatetime.ToString("yyyy-MM-dd HH:mm:ss"),
                    timeZone = "UTC"
                },
                end = new
                {
                    dateTime = endDatetime.ToString("yyyy-MM-dd HH:mm:ss"),
                    timeZone = "UTC"
                },
                attendees = attendees,
                location = new { displayName = "ConCall Meeting" },
                reminderMinutesBeforeStart = 0,
                isReminderOn = true,
                body = new
                {
                    contentType = "HTML",
                    content = string.Format("發起人：{0}<br/> 與會者：{1}", name, attendeName)
                }
            });

            return o.ToString();
        }


    }
}
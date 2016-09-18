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
    public class OneDriveController : Controller
    {
        // GET: OneDrive
        public ActionResult Index(UserInfo userInfo)
        {
            EnsureUser(ref userInfo);

            ViewBag.UserInfo = userInfo;
            return View();
        }


        public async Task<ActionResult> GetOneDriveList()
        {
            List<OneDriveInfo> myDataModelList = new List<OneDriveInfo>();

            myDataModelList = await UnifiedApiHelper.GetOneDriveAsync((string)Session[SessionKeys.Login.AccessToken]);
            
            return View(myDataModelList);
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

    }
}
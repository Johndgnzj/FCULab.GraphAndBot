using John.FCULab.Bot.Sample.AttendanceSystem.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace John.FCULab.Bot.Sample.AttendanceSystem
{
    [Serializable]
    public class AttendanceDialog : IDialog<object>
    {
        protected UserModel user = new UserModel();
        public enum GendarOption { Male, Female };
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            context.UserData.TryGetValue<UserModel>("user", out user);
            if (user == null)
            {
                user = new UserModel();
                context.UserData.SetValue<UserModel>("user", user);
                PromptDialog.Text(context,
                FillUserAsync,
                "Please tell me your name first!",
                "Didn't get that!");
            }else
            {
                if (message.Text.Contains("?") || message.Text.Contains("help"))
                {
                    List<object> list = new List<object>();
                    list.Add("我要請假");
                    list.Add("查詢請假記錄");
                    list.Add("沒事");
                    IEnumerable<object> en = list;
                    PromptDialog.Choice<object>(context,
                        MenuActionAsync,
                        en,
                        "How can i server you?",
                        "Didn't get that!");

                }
                else
                {
                    await context.PostAsync(string.Format("Hello {0}, What can i do for you?", user.name));
                    await context.PostAsync(string.Format("You can type \"?\" or help to get guide menu."));
                    context.Wait(MessageReceivedAsync);
                }
            }
        }
        public async Task<bool> CheckUserData(IDialogContext context)
        {
            context.UserData.TryGetValue<UserModel>("user", out user);
            if (user == null)
            {
                user = new UserModel();
                context.UserData.SetValue<UserModel>("user", user);
            }
            if (string.IsNullOrEmpty(user?.name))
            {
                PromptDialog.Text(context,
                    FillUserAsync,
                    "Please tell me your name",
                    "Didn't get that!");
            }
            else if (string.IsNullOrEmpty(user?.id))
            {
                PromptDialog.Text(context,
                    FillidAsync,
                    "Please input your id",
                    "Didn't get that!");
            }
            else if (string.IsNullOrEmpty(user?.gendar))
            {
                List<object> list = new List<object>();
                list.Add("Male");
                list.Add("Female");
                IEnumerable<object> en = list;
                PromptDialog.Choice<object>(context,
                    SelectGendarAsync,
                    en,
                    "Please Select your gendar",
                    "Didn't get that!");
            }
            else { return true; }
            return false;
            
        }
        public virtual async Task FillUserAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var name = await argument;
            context.UserData.TryGetValue<UserModel>("user", out user);
            user.name = name;
            context.UserData.SetValue<UserModel>("user", user);
            PromptDialog.Text(context,
            FillidAsync,
            "Please input your id",
            "Didn't get that!");
        }
        public virtual async Task FillidAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var id = await argument;
            context.UserData.TryGetValue<UserModel>("user", out user);
            user.id = id;
            context.UserData.SetValue<UserModel>("user", user);
            List<object> list = new List<object>();
            list.Add("Male");
            list.Add("Female");
            IEnumerable<object> en = list;
            PromptDialog.Choice<object>(context,
                SelectGendarAsync,
                en,
                "Please Select your gendar",
                "Didn't get that!");

        }
        public virtual async Task SelectGendarAsync(IDialogContext context, IAwaitable<object> argument)
        {
            var gendarList = await argument;
            context.UserData.TryGetValue<UserModel>("user", out user);
            user.gendar = gendarList.ToString();
            context.UserData.SetValue<UserModel>("user", user);
            await context.PostAsync(string.Format("Hello {0}, What can i do for you?", user.name));
            context.Wait(MessageReceivedAsync);
        }
        public virtual async Task MenuActionAsync(IDialogContext context,IAwaitable<object> argument)
        {
            var actionResult = await argument;
            switch (actionResult.ToString())
            {
                case "我要請假":
                    List<object> LeaveReasonList = new List<object>();
                    LeaveReasonList.Add("Personal Leave");
                    LeaveReasonList.Add("Sick Leave");
                    LeaveReasonList.Add("Funeral Leave");
                    LeaveReasonList.Add("Menstrual Leave");

                    IEnumerable<object> en = LeaveReasonList;
                    PromptDialog.Choice<object>(context,
                        SelectLeaveReasonAsync,
                        en,
                        "Please Select your gendar",
                        "Didn't get that!");
                    break;
                case "查詢請假記錄":
                    context.UserData.TryGetValue<UserModel>("user", out user);
                    await context.PostAsync(string.Format("Hi {0}, Your total leave day :{1}",user.name,(user.PersonalLeave + user.SickLeave + user.FuneralLeave + user.MenstrualLeave)));
                    context.Wait(MessageReceivedAsync);
                    break;
                case "沒事":
                default:
                    await context.PostAsync("If you need anything, just call me.");
                    context.Wait(MessageReceivedAsync);
                    break;
            }
        }
        public virtual async Task SelectLeaveReasonAsync(IDialogContext context, IAwaitable<object> argument)
        {
            var LeaveReason = await argument;
            context.UserData.SetValue<string>("tmp_leaveReason", LeaveReason.ToString());
            PromptDialog.Text(context,
            SelectLeaveDateAsync,
            "Please input your Leave Date",
            "Didn't get that!");

        }
        public virtual async Task SelectLeaveDateAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var LeaveDate = await argument;
            DateTime outDt;
            if (DateTime.TryParse(LeaveDate,out outDt))
            {
                context.UserData.SetValue<DateTime>("tmp_leaveDate", outDt);
                string LeaveReason = "";
                context.UserData.TryGetValue<string>("tmp_leaveReason", out LeaveReason);
                string confirm = string.Format("Are you wanna take a {0} in {1:MM/dd}?", LeaveReason, outDt);
                PromptDialog.Confirm(context,
                    ConfirmLeaveAsync,
                    confirm,
                    "this is not an option.",
                    3, PromptStyle.Auto);
            }
            else
            {
                PromptDialog.Text(context,
                SelectLeaveDateAsync,
                "Didn't get that!",
                "Didn't get that!");
            }
        }
        public virtual async Task ConfirmLeaveAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                context.UserData.TryGetValue<UserModel>("user", out user);
                string LeaveReason = "";
                context.UserData.TryGetValue<string>("tmp_leaveReason", out LeaveReason);
                switch (LeaveReason)
                {
                    case "Personal Leave":
                        user.PersonalLeave += 1;
                        break;
                    case "Sick Leave":
                        user.SickLeave += 1;
                        break;
                    case "Funeral Leave":
                        user.FuneralLeave += 1;
                        break;
                    case "Menstrual Leave":
                        user.MenstrualLeave += 1;
                        break;
                    default:
                        break;
                }
                context.UserData.SetValue<UserModel>("user", user);
                await context.PostAsync("Your leave apply is proccessed.");
            }
            else
            {
                await context.PostAsync("The leave apply was canceled.");
            }
            context.UserData.RemoveValue("tmp_leaveReason");
            context.UserData.RemoveValue("tmp_leaveDate");
            context.Wait(MessageReceivedAsync);

        }

    }
}
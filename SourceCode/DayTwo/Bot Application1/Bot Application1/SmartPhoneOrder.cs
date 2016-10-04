using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Bot.Builder.FormFlow;
using Newtonsoft.Json;

namespace Bot_Application1
{
    public enum PhoneType
    {
        Android=1,iOS=2,BlackBerry=3,WindowPhone=4
    }
    [Serializable]
    public class SmartPhoneOrder
    {
        public PhoneType? phoneType;
        public static IForm<SmartPhoneOrder> BuildForm()
        {
            OnCompletionAsyncDelegate<SmartPhoneOrder> processOrder = async (context, state) =>
            {
                var reply = context.MakeMessage();
                reply.Text = "Your Order is:" + JsonConvert.SerializeObject(state);
                await context.PostAsync(reply);
            };

            return new FormBuilder<SmartPhoneOrder>()
                .Message("Welcome to the simple SmartPhone order bot!")
                .OnCompletion(processOrder)
                .Build();
        }
    }
}
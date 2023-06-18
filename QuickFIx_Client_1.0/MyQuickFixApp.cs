using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickFIx_Client_1._0
{
    class MyQuickFixApp : MessageCracker, IApplication
    {
        private SessionSettings settings;

        public MyQuickFixApp(SessionSettings settings)
        {
            this.settings = settings;
        }

        void IApplication.FromAdmin(Message message, SessionID sessionID)
        {
            Console.WriteLine("客户端：FromAdmin");
            Crack(message, sessionID);
        }

        void IApplication.FromApp(Message message, SessionID sessionID)
        {
            Console.WriteLine("客户端：FromApp");
        }

        void IApplication.OnCreate(SessionID sessionID)
        {
            Console.WriteLine("客户端：创建");
            Session.SendToTarget(new QuickFix.FIX44.NewOrderSingle() { Account = new QuickFix.Fields.Account("Hello") });
        }

        void IApplication.OnLogon(SessionID sessionID)
        {
            Console.WriteLine("客户端：登录成功");
        }

        void IApplication.OnLogout(SessionID sessionID)
        {
            Console.WriteLine("客户端：登出成功");
        }

        void IApplication.ToAdmin(Message message, SessionID sessionID)
        {
            Console.WriteLine("客户端：ToAdmin");
        }

        void IApplication.ToApp(Message message, SessionID sessionID)
        {
            Console.WriteLine("客户端：ToApp");
        }

        #region 消息处理
        public void OnMessage(string msg, SessionID sessionID)
        {
            Console.WriteLine("客户端接收消息" + msg);
        }
        #endregion 
    }
}

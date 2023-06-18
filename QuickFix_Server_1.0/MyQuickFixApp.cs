using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickFix_Server_1._0
{
    class MyQuickFixApp : MessageCracker, IApplication
    {
        void IApplication.FromAdmin(Message message, SessionID sessionID)
        {
            Crack(message, sessionID);
            Console.WriteLine("服务端：FromAdmin");
        }

        void IApplication.FromApp(Message message, SessionID sessionID)
        {
            Console.WriteLine("服务端：FromApp");
        }

        void IApplication.OnCreate(SessionID sessionID)
        {
            Console.WriteLine("服务端：创建");
        }

        void IApplication.OnLogon(SessionID sessionID)
        {
            Console.WriteLine("服务端：登录成功");
        }

        void IApplication.OnLogout(SessionID sessionID)
        {
            Console.WriteLine("服务端：登出成功");
        }

        void IApplication.ToAdmin(Message message, SessionID sessionID)
        {
            Console.WriteLine("服务端：ToAdmin");
        }

        void IApplication.ToApp(Message message, SessionID sessionID)
        {
            Console.WriteLine("服务端：ToApp");
        }

        public void OnMessage(string msg, SessionID sessionID)
        {
            Console.WriteLine("服务端收到消息：" + msg);
        }

    }
}

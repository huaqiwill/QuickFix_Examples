using QuickFix;
using QuickFix.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickFIx_Client_1._0
{
    class Program
    {
        static void Main(string[] args)
        {
            SessionSettings settings = new SessionSettings("./config/sample_initiator.cfg"); //加载配置文件
            IMessageStoreFactory storeFactory = new FileStoreFactory(settings);
            ILogFactory logFactory = new FileLogFactory(settings);
            MyQuickFixApp fixApp = new MyQuickFixApp(settings);

            SocketInitiator fixClient = new SocketInitiator(
                        fixApp,
                        storeFactory,
                        settings,
                        logFactory);
            fixClient.Start(); //开始连接以FIX 接受端
        }
    }
}

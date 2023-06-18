#define DEBUG

using QuickFix;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace QuickFix_Server_1._0
{
    class Program
    {
        static void Main(string[] args)
        {
            SessionSettings settings = new SessionSettings("./config/sample_acceptor.cfg");
            IApplication myApp = new MyQuickFixApp();
            IMessageStoreFactory storeFactory = new FileStoreFactory(settings);
            ILogFactory logFactory = new FileLogFactory(settings);
            ThreadedSocketAcceptor acceptor = new ThreadedSocketAcceptor(
                myApp,
                storeFactory,
                settings,
                logFactory);

            acceptor.Start();
            while (true)
            {
                System.Console.WriteLine("o hai");
                System.Threading.Thread.Sleep(1000);
            }
            acceptor.Stop();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetaQuotes.MT5CommonAPI;
using MetaQuotes.MT5ManagerAPI;
using System.Threading;

namespace QuickFIx_Client_1._0
{

    //+------------------------------------------------------------------+
    //| 经销商请求接收器                                                    |
    //+------------------------------------------------------------------+
    class CRequestSink : CIMTRequestSink
    {
        CIMTManagerAPI m_manager = null;            // Manager界面
        EventWaitHandle m_event_request = null;            // 请求通知事件
                                                           //+------------------------------------------------------------------+
                                                           //| 初始化本机实现                                                   |
                                                           //+------------------------------------------------------------------+
        public MTRetCode Initialize(CIMTManagerAPI manager, EventWaitHandle event_request)
        {
            //--- 检查
            if (manager == null || event_request == null)
                return (MTRetCode.MT_RET_ERR_PARAMS);
            //--- 
            m_manager = manager;
            m_event_request = event_request;
            //---
            return (RegisterSink());
        }



        //+------------------------------------------------------------------+
        //|接收器事件处理程序                                                |
        //+------------------------------------------------------------------+
        public override void OnRequestAdd(CIMTRequest request) { NotifyRequest(); }
        public override void OnRequestUpdate(CIMTRequest request) { NotifyRequest(); }
        public override void OnRequestDelete(CIMTRequest request) { NotifyRequest(); }
        public override void OnRequestSync() { NotifyRequest(); }
        //+------------------------------------------------------------------+
        //| 请求通知                                                          |
        //+------------------------------------------------------------------+
        void NotifyRequest()
        {
            //--- 检查可用请求
            if (m_manager.RequestTotal() > 0)
            {
                //--- 请求已存在      
                if (!m_event_request.WaitOne(0))
                    m_event_request.Set();
            }
            else
            {
                //--- 请求队列为空
                if (m_event_request.WaitOne(0))
                    m_event_request.Reset();
            }
        }
    }
    //+------------------------------------------------------------------+
    //| 经销商订单接收器                                                 |
    //+------------------------------------------------------------------+
    class COrderSink : CIMTOrderSink
    {
        CIMTManagerAPI m_manager = null;
        //+------------------------------------------------------------------+
        //| 初始化本机实现                                                   |
        //+------------------------------------------------------------------+
        public MTRetCode Initialize(CIMTManagerAPI manager)
        {
            //--- 检查
            if (manager == null)
                return (MTRetCode.MT_RET_ERR_PARAMS);
            //--- 
            m_manager = manager;
            //---
            return (RegisterSink());
        }
        //+------------------------------------------------------------------+
        //|                                                                  |
        //+------------------------------------------------------------------+
        public override void OnOrderAdd(CIMTOrder order)
        {
            if (order != null)
            {
                string str = order.Print();
                m_manager.LoggerOut(EnMTLogCode.MTLogOK, "{0} 已添加", str);
            }
        }
        //+------------------------------------------------------------------+
        //|                                                                  |
        //+------------------------------------------------------------------+
        public override void OnOrderUpdate(CIMTOrder order)
        {
            if (order != null)
            {
                string str = order.Print();
                m_manager.LoggerOut(EnMTLogCode.MTLogOK, "{0} 已更新", str);
            }
        }
        //+------------------------------------------------------------------+
        //|                                                                  |
        //+------------------------------------------------------------------+
        public override void OnOrderDelete(CIMTOrder order)
        {
            if (order != null)
            {
                string str = order.Print();
                m_manager.LoggerOut(EnMTLogCode.MTLogOK, "{0} 已删除", str);
            }
        }
    }

    class CPositionSink : CIMTPositionSink
    {
        CIMTManagerAPI m_manager = null;
        Action<CIMTPosition> addCallBack;
        Action<CIMTPosition> delCallBack;
        Action<CIMTPosition> upateCallBack;

        //+------------------------------------------------------------------+
        //| 初始化本机实现                                                   |
        //+------------------------------------------------------------------+
        public MTRetCode Initialize(CIMTManagerAPI manager, Action<CIMTPosition> addCallBack, Action<CIMTPosition> delCallBack, Action<CIMTPosition> upateCallBack)
        {
            //--- 检查
            if (manager == null)
                return (MTRetCode.MT_RET_ERR_PARAMS);
            //--- 
            m_manager = manager;
            this.addCallBack = addCallBack;
            this.delCallBack = delCallBack;
            this.upateCallBack = upateCallBack;
            //---
            return RegisterSink();
        }
        public override void OnPositionAdd(CIMTPosition position)
        {
            addCallBack?.Invoke(position);
        }
        public override void OnPositionDelete(CIMTPosition position)
        {
            delCallBack?.Invoke(position);
        }

        public override void OnPositionUpdate(CIMTPosition position)
        {
            upateCallBack?.Invoke(position);
        }
    }

    class CUserSink : CIMTUserSink
    {
        CIMTManagerAPI m_manager = null;
        public MTRetCode Initialize(CIMTManagerAPI manager)
        {
            //--- 检查
            if (manager == null)
                return (MTRetCode.MT_RET_ERR_PARAMS);
            //--- 
            m_manager = manager;
            return RegisterSink();
        }
        public override void OnUserAdd(CIMTUser user)
        {
            base.OnUserAdd(user);
        }
        public override void OnUserDelete(CIMTUser user)
        {
            base.OnUserDelete(user);
        }
    }
    //+------------------------------------------------------------------+
    //|经销商                                                            |
    //+------------------------------------------------------------------+
    public class MT5App : CIMTManagerSink
    {
        /// <summary>
        /// 连接超时（毫秒）
        /// </summary>
        const uint MT5_CONNECT_TIMEOUT = 30000;
        /// <summary>
        /// 处理线程的堆栈大小（字节）
        /// </summary>
        const int STACK_SIZE_COMMON = 1024 * 1024;

        CRequestSink m_request_sink = null;            //请求接收器
        COrderSink m_order_sink = null;                //订单接收器
        CUserSink m_user_sink = null;

        CPositionSink m_Position_sink = null;                //持仓接收器
        //CIMTPositionArray m_PositionGetByGroup_mask = null;  //为一个或多个客户组获取所有当前未结的持仓

        public CIMTManagerAPI m_manager = null;            // Manager 界面
        public string m_server = null;                     // 服务器地址
        public ulong m_login = 0;                          // 经销商登录
        public string m_password = null;                   // 经销商密码
        CIMTRequest m_request = null;                      // 请求接口
        CIMTConfirm m_confirm = null;                      // 确认界面
        int m_stop_flag = 0;                               // 交易停止标志
        Thread m_thread_dealer = null;                     // 交易线程
        public int m_connected = 0;                        // 连接标志
        EventWaitHandle m_event_request = null;            // 请求通知事件
        EventWaitHandle m_event_answer = null;             // 应答通知事件
        public Action<CIMTPosition> PositionAddCallBack;
        public Action<CIMTPosition> PositionDelCallBack;
        public Action<CIMTPosition> PositionUpateCallBack;

        public Action OnConnected;
        /// <summary>
        ///  构造函数 
        /// </summary>
        public MT5App()
        {
        }
        /// <summary>
        /// 释放
        /// </summary>
        public override void Release()
        {
            //--- 断开连接，取消订阅
            Stop();
            //--- 卸载工厂，全部释放
            Shutdown();
            //---
            base.Release();
        }
        /// <summary>
        /// 创建请求
        /// </summary>
        /// <returns></returns>
        public CIMTRequest RequestCreate()
        {
            if (m_manager == null)
                return (null);
            //---
            return (m_manager.RequestCreate());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public CIMTConfirm ConfirmCreate()
        {
            if (m_manager == null)
                return (null);
            //---
            return (m_manager.DealerConfirmCreate());
        }
        /// <summary>
        /// 经销商停止
        /// </summary>
        public void Stop()
        {
            //--- 如果创建了manager界面
            if (m_manager != null)
            {
                //--- 取消订阅 来自通知
                m_manager.Unsubscribe(this);
                //--- 取消订阅 来自请求
                m_manager.RequestUnsubscribe(m_request_sink);
                ////---取消订阅 来自订单
                m_manager.OrderUnsubscribe(m_order_sink);
            }
            //---等待交易线程退出
            if (m_thread_dealer != null)
            {
                //--- 设置线程停止标志
                Interlocked.Exchange(ref m_stop_flag, 1);
                //--- 设置应答事件
                m_event_answer.Set();
                //--- 将经销商线程从等待状态释放
                if (!m_event_request.WaitOne(0))
                    m_event_request.Set();
                //--- 等待线程退出
                m_thread_dealer.Join(Timeout.Infinite);
                m_thread_dealer = null;
            }
        }
        /// <summary>
        /// 初始化库 
        /// </summary>
        /// <returns></returns>
        public bool Initialize()
        {
            //---
            string message = string.Empty;
            MTRetCode res = MTRetCode.MT_RET_OK_NONE;
            uint version = 0;
            //--- 检查
            //---
            try
            {
                //---初始化CIMTManagerSink本机链接
                if ((res = RegisterSink()) != MTRetCode.MT_RET_OK)
                {
                    message = string.Format("注册接收器失败 ({0})", res);
                    return (false);
                }
                //---加载管理器API
                if ((res = SMTManagerAPIFactory.Initialize(null)) != MTRetCode.MT_RET_OK)
                {
                    message = string.Format("加载manager API失败 ({0})", res);
                    return (false);
                }
                //--- 检查Manager API版本
                if ((res = SMTManagerAPIFactory.GetVersion(out version)) != MTRetCode.MT_RET_OK)
                {
                    message = string.Format("经销商：获取版本失败 ({0})", res);
                    return (false);
                }
                if (version != SMTManagerAPIFactory.ManagerAPIVersion)
                {
                    message = string.Format("经销商：Manager API版本错误，版本 {0} 必修的", SMTManagerAPIFactory.ManagerAPIVersion);
                    return (false);
                }
                //--- 创建管理器界面
                if ((m_manager = SMTManagerAPIFactory.CreateManager(SMTManagerAPIFactory.ManagerAPIVersion, out res)) == null || res != MTRetCode.MT_RET_OK)
                {
                    message = string.Format("经销商：创建manager界面失败 ({0})", res);
                    return (false);
                }
                //--- 创建请求对象
                if ((m_request = m_manager.RequestCreate()) == null)
                {
                    m_manager.LoggerOut(EnMTLogCode.MTLogErr, "经销商：创建请求对象失败");
                    return (false);
                }
                //--- 创建确认对象
                if ((m_confirm = m_manager.DealerConfirmCreate()) == null)
                {
                    m_manager.LoggerOut(EnMTLogCode.MTLogErr, "经销商：创建确认对象失败");
                    return (false);
                }
                //--- 创建请求事件
                m_event_request = new EventWaitHandle(false, EventResetMode.ManualReset);
                //--- 创建请求事件
                m_event_answer = new EventWaitHandle(false, EventResetMode.AutoReset);
                //--- 
                m_request_sink = new CRequestSink();
                if ((res = m_request_sink.Initialize(m_manager, m_event_request)) != MTRetCode.MT_RET_OK)
                {
                    m_manager.LoggerOut(EnMTLogCode.MTLogErr, "经销商：创建请求接收器失败");
                    return (false);
                }
                m_user_sink = new CUserSink();
                if ((res = m_user_sink.Initialize(m_manager)) != MTRetCode.MT_RET_OK)
                {
                    m_manager.LoggerOut(EnMTLogCode.MTLogErr, "用户：创建请求接收器失败");
                    return (false);
                }

                //--- 
                m_order_sink = new COrderSink();
                if ((res = m_order_sink.Initialize(m_manager)) != MTRetCode.MT_RET_OK)
                {
                    m_manager.LoggerOut(EnMTLogCode.MTLogErr, "订单：创建请求接收器失败");
                    return (false);
                }
                //----
                m_Position_sink = new CPositionSink();
                if ((res = m_Position_sink.Initialize(m_manager, PositionAddCallBack, PositionDelCallBack, PositionUpateCallBack)) != MTRetCode.MT_RET_OK)
                {
                    m_manager.LoggerOut(EnMTLogCode.MTLogErr, "持仓：创建请求接收器失败");
                    return (false);
                }

                //--- 已完
                return (true);
            }
            catch (Exception ex)
            {
                if (m_manager != null)
                    m_manager.LoggerOut(EnMTLogCode.MTLogErr, "经销商：初始化失败 - {0}", ex.Message);
                //---
            }
            //--- 已完成，但有错误
            return (false);
        }
        /// <summary>
        ///  Manager API 停止运转 
        /// </summary>
        void Shutdown()
        {
            //--- 自由请求接收器
            if (m_request_sink != null)
            {
                m_request_sink.Dispose();
                m_request_sink = null;
            }
            //---自由订单接收器
            if (m_order_sink != null)
            {
                m_order_sink.Dispose();
                m_order_sink = null;
            }
            //--- 关闭应答事件
            if (m_event_answer != null)
            {
                m_event_answer.Close();
                m_event_answer = null;
            }
            //--- 关闭请求事件
            if (m_event_request != null)
            {
                m_event_request.Close();
                m_event_request = null;
            }
            //--- 释放请求对象
            if (m_request != null)
            {
                m_request.Dispose();
                m_request = null;
            }
            //--- 释放确认对象
            if (m_confirm != null)
            {
                m_confirm.Dispose();
                m_confirm = null;
            }
            //---如果创建了manager界面
            if (m_manager != null)
            {
                //--- 释放 manager 界面
                m_manager.Dispose();
                m_manager = null;
            }
            //--- parent
        }
        /// <summary>
        /// 应要求回答
        /// </summary>
        /// <param name="confirm"></param>
        public void DealerAnswer(CIMTConfirm confirm)
        {
            //--- 设置确认
            m_confirm.Assign(confirm);
            //---回答就绪
            m_event_answer.Set();
        }
        /// <summary>
        ///  获取最后一个请求
        /// </summary>
        /// <param name="request"></param>
        public void GetLastRequest(ref CIMTRequest request)
        {
            //--- 返回请求
            request.Assign(m_request);
        }
        public void Restart()
        {
            if (m_thread_dealer != null)
            {
                OnDisconnect();
            }
            else
            {
                Start(m_server, m_login, m_password);
            }

        }
        /// <summary>
        ///  经销商启动
        /// </summary>
        /// <param name="server"></param>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool Start(string server, ulong login, string password)
        {
            //--- 检查 manager
            if (m_manager == null)
                return (false);

            //--- 别让例外情况在这里发生
            try
            {
                //--- 检查经销商是否已启动
                if (m_thread_dealer != null)
                {
                    //--- 经销商线程正在运行
                    if (m_thread_dealer.IsAlive)
                        return (false);
                    //---
                    m_thread_dealer = null;
                }
                //--- 保存授权信息
                m_server = server;
                m_login = login;
                m_password = password;
                //--- 订阅通知
                if (m_manager.Subscribe(this) != MTRetCode.MT_RET_OK)
                    return (false);
                //--- 订阅请求
                if (m_manager.RequestSubscribe(m_request_sink) != MTRetCode.MT_RET_OK)
                    return (false);
                //--- 订阅订单
                if (m_manager.OrderSubscribe(m_order_sink) != MTRetCode.MT_RET_OK)
                    return (false);

                //---订阅持仓订单
                if (m_manager.PositionSubscribe(m_Position_sink) != MTRetCode.MT_RET_OK)
                    return (false);


                //--- 开始处理线程
                m_stop_flag = 0;
                m_connected = 0;
                //--- 开始线程
                m_thread_dealer = new Thread(DealerFunc, STACK_SIZE_COMMON);
                m_thread_dealer.Start();
                //--- 完成
                return (true);
            }
            catch (Exception ex)
            {
                if (m_manager != null)
                    m_manager.LoggerOut(EnMTLogCode.MTLogErr, "经销商：启动失败 - {0}", ex.Message);
                //---
            }
            //--- 已完成，但有错误
            return (false);
        }


        /// <summary>
        /// 连接MT5服务器
        /// </summary>
        /// <returns></returns>
        public bool Connect()
        {
            MTRetCode res = m_manager.Connect(m_server, m_login, m_password, null,
                                                   CIMTManagerAPI.EnPumpModes.PUMP_MODE_SYMBOLS |
                                                   CIMTManagerAPI.EnPumpModes.PUMP_MODE_GROUPS |
                                                   CIMTManagerAPI.EnPumpModes.PUMP_MODE_USERS |
                                                   CIMTManagerAPI.EnPumpModes.PUMP_MODE_ORDERS |
                                                   CIMTManagerAPI.EnPumpModes.PUMP_MODE_POSITIONS,
                                                   MT5_CONNECT_TIMEOUT);
            return res == MTRetCode.MT_RET_OK;
        }

        /// <summary>
        /// 处理线程函数
        /// </summary>
        void DealerFunc()
        {
            //--- 处理
            while (Interlocked.Add(ref m_stop_flag, 0) == 0)
            {
                //--- 连接到服务器
                if (Interlocked.Add(ref m_connected, 0) == 0)
                {
                    //--- 将manager连接到服务器
                    MTRetCode res = m_manager.Connect(m_server, m_login, m_password, null,
                                                    CIMTManagerAPI.EnPumpModes.PUMP_MODE_SYMBOLS |
                                                    CIMTManagerAPI.EnPumpModes.PUMP_MODE_GROUPS |
                                                    CIMTManagerAPI.EnPumpModes.PUMP_MODE_USERS |
                                                    CIMTManagerAPI.EnPumpModes.PUMP_MODE_ORDERS |
                                                    CIMTManagerAPI.EnPumpModes.PUMP_MODE_POSITIONS,
                                                    MT5_CONNECT_TIMEOUT);
                    if (res != MTRetCode.MT_RET_OK)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    //--- 启动经销商
                    if (m_manager.DealerStart() != MTRetCode.MT_RET_OK)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    Interlocked.Exchange(ref m_connected, 1);
                    //--- 重置应答事件
                    m_event_answer.Reset();
                }
                //--- 等待请求
                m_event_request.WaitOne(Timeout.Infinite);
                //--- 检查停止标志
                if (Interlocked.Add(ref m_stop_flag, 0) != 0)
                    break;
                //--- 获取下一个请求
                if (m_manager.DealerGet(m_request) == MTRetCode.MT_RET_OK)
                {
                    MTTickShort tick;
                    string str = "";
                    //--- 清除确认
                    m_confirm.Clear();
                    //--- 打印请求
                    str = m_request.Print();
                    //--- 获取请求符号的最后勾号
                    if (m_manager.TickLast(m_request.Symbol(), m_request.Group(), out tick) != MTRetCode.MT_RET_OK)
                    {
                        //--- ticks 未找到
                        //--- 选择符号
                        m_manager.SelectedAdd(m_request.Symbol());
                        //--- 请求id
                        m_confirm.ID(m_request.ID());
                        //--- 返回请求
                        m_confirm.Retcode(MTRetCode.MT_RET_REQUEST_RETURN);
                        if (m_manager.DealerAnswer(m_confirm) == MTRetCode.MT_RET_OK)
                            m_manager.LoggerOut(EnMTLogCode.MTLogOK, "'{0}': return {1} for '{2}'", m_login, str, m_request.Login());
                        continue;
                    }
                    //--- 设置订单价格
                    if (m_request.PriceOrder() == 0)
                        switch (m_request.Type())
                        {
                            case CIMTOrder.EnOrderType.OP_BUY:
                                m_request.PriceOrder(tick.ask);
                                break;
                            case CIMTOrder.EnOrderType.OP_SELL:
                                m_request.PriceOrder(tick.bid);
                                break;
                        }
                    //--- 请求就绪
                    //  m_parent.FireAPIRequest();
                    //--- 等待回答
                    m_event_answer.WaitOne(Timeout.Infinite);
                    //--- 检查停止标志
                    if (Interlocked.Add(ref m_stop_flag, 0) != 0)
                        break;
                    //--- 发送确认
                    if (m_manager.DealerAnswer(m_confirm) == MTRetCode.MT_RET_OK)
                        m_manager.LoggerOut(EnMTLogCode.MTLogOK, "'{0}': confirm {1} for '{2}'", m_login, str, m_request.Login());
                }
                else
                    Thread.Sleep(100);
            }
            //--- 停止经销商
            m_manager.DealerStop();
            //--- 断开 manager
            m_manager.Disconnect();
        }
        public override void OnConnect()
        {
            base.OnConnect();
            OnConnected?.Invoke();
        }

        /// <summary>
        /// 断开连接通知   
        /// </summary>
        public override void OnDisconnect()
        {
            //--- 需要重新连接
            Interlocked.Exchange(ref m_connected, 0);
            //--- 继续经销商线程
            if (!m_event_request.WaitOne(0))
                m_event_request.Set();
            //--- 设置事件答案
            m_event_answer.Set();
            //--- 发送断开连接消息
            // m_parent.FireAPIDisconnect();
        }
    }
}

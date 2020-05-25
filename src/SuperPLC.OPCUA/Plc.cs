using Opc.Ua;
using OpcUaHelper;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace SuperPLC.OPCUA
{
    /// <summary>
    /// 通过OPC与PLC通信类
    /// </summary>
    public class Plc
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="ip">PLC的IP地址</param>
        /// <param name="username">OPC的用户名，如果未启用用户验证，不用填</param>
        /// <param name="password">OPC的密码，如果未启用用户验证，不用填</param>
        public Plc(string ip, string username = null, string password = null)
        {
            Ip = ip;
            Url = $"opc.tcp://{Ip}:4840";
            client = new OpcUaClient();
            client.OpcStatusChange += Client_OpcStatusChange;
            if (username != null && password != null)
            {
                client.UserIdentity = new UserIdentity(username, password);
            }
            subscriptions = new Dictionary<string, Action<OpcData>>();
        }

        private OpcUaClient client;

        private Dictionary<string, Action<OpcData>> subscriptions = new Dictionary<string, Action<OpcData>>();

        /// <summary>
        /// 获取PLC的IP地址
        /// </summary>
        public string Ip { get; private set; }

        /// <summary>
        /// 获取用于连接到OPC的URL
        /// </summary>
        public string Url { get; private set; }

        /// <summary>
        /// 是否能ping通PLC，若延迟大于1000ms，则认为ping不通
        /// </summary>
        public bool IsAvailable
        {
            get
            {
                try
                {
                    if (IPAddress.TryParse(Ip, out IPAddress iPAddress))
                    {
                        using (var ping = new Ping())
                        {
                            var result = ping.Send(iPAddress, 1000);
                            return result.Status == IPStatus.Success;
                        }
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 获取与OPC/PLC的连接状态
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return client != null && IsAvailable && client.Connected;
            }
        }

        /// <summary>
        /// 异常信息事件
        /// </summary>
        public event Action<string> ErrorEvent;

        /// <summary>
        /// 连接状态改变事件
        /// </summary>
        public event Action<bool> StatusChangeEvent;

        /// <summary>
        /// 打开连接
        /// </summary>
        /// <returns></returns>
        public bool Open()
        {
            try
            {
                client.ConnectServer(Url).Wait();
                return client.Connected;
            }
            catch (Exception ex)
            {
                ErrorEvent?.Invoke($"连接OPC异常【{ex.Message}】");
                return false;
            }
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            try
            {
                client?.Disconnect();
            }
            catch
            { }
        }

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public T Read<T>(string nodeId)
        {
            TryRead(nodeId, out T value);
            return value;
        }

        /// <summary>
        /// 尝试读取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nodeId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryRead<T>(string nodeId, out T value)
        {
            try
            {
                value = client.ReadNode<T>(nodeId);
                return true;
            }
            catch (Exception ex)
            {
                ErrorEvent?.Invoke($"读取OPC异常【{ex.Message}】");
                value = default;
                return false;
            }
        }

        /// <summary>
        /// 尝试写入数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nodeId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryWrite<T>(string nodeId, T value)
        {
            try
            {
                return client.WriteNode(nodeId, value);
            }
            catch (Exception ex)
            {
                ErrorEvent?.Invoke($"读取OPC异常【{ex.Message}】");
                return false;
            }
        }

        /// <summary>
        /// 订阅信号
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public bool AddSubscription(string nodeId, Action<OpcData> action)
        {
            try
            {
                if (!subscriptions.ContainsKey(nodeId))
                {
                    client.AddSubscription(nodeId, nodeId, (key, item, e) =>
                    {
                        if (subscriptions.ContainsKey(key))
                        {
                            subscriptions[key]?.Invoke(new OpcData(key, (e.NotificationValue as MonitoredItemNotification).Value.WrappedValue.Value));
                        }
                    });
                    subscriptions.Add(nodeId, action);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                ErrorEvent?.Invoke($"订阅OPC异常【{ex.Message}】");
                return false;
            }
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public bool CancelSubscription(string nodeId)
        {
            try
            {
                if (subscriptions.ContainsKey(nodeId))
                {
                    subscriptions.Remove(nodeId);
                }
                client.RemoveSubscription(nodeId);
                return true;
            }
            catch (Exception ex)
            {
                ErrorEvent?.Invoke($"取消订阅OPC异常【{ex.Message}】");
                return false;
            }
        }

        private void Client_OpcStatusChange(object sender, OpcUaStatusEventArgs e)
        {
            StatusChangeEvent?.Invoke(IsConnected);
        }
    }
}

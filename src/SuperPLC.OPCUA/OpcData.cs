namespace SuperPLC.OPCUA
{
    /// <summary>
    /// 订阅后发布的数据
    /// </summary>
    public class OpcData
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="value"></param>
        public OpcData(string nodeId, object value)
        {
            NodeId = nodeId;
            Value = value;
        }

        /// <summary>
        /// NodeId
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// 数据值
        /// </summary>
        public object Value { get; set; }
    }
}

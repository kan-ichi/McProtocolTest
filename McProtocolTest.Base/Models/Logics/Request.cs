using System;

namespace McProtocolTest.Models.Logics
{
    /// <summary>
    /// 処理依頼
    /// </summary>
    [Serializable]
    public class Request : Interface.IRequest
    {
        /// <summary>
        /// 処理依頼パラメーター
        /// </summary>
        public object Parameter { get; set; }
    }
}

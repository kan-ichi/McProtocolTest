using McProtocolTest.Utils;
using System;
using System.Net.Sockets;

namespace McProtocolTest.Models.Logics
{
    /// <summary>
    /// PLCのデバイス読出／書込処理
    /// </summary>
    public class ReadWritePlcDevice
    {
        /// <summary>処理依頼パラメーター</summary>
        public struct Parameter
        {
            /// <summary>PLCのIPアドレス</summary>
            public string PlcIPAddress { get; set; }
            /// <summary>PLCのポート番号</summary>
            public ushort PlcPortNumber { get; set; }
            /// <summary>PLCへデータ送信してから受信までの待機時間</summary>
            public ushort PlcResponseWaitMilliSecond { get; set; }
            /// <summary>コマンド種類（読出／書込／ランダム読出）</summary>
            public McProtocolUtil.CommandType CommandType { get; set; }
            /// <summary>デバイス種類</summary>
            public McProtocolUtil.DeviceType DeviceType { get; set; }
            /// <summary>先頭デバイス番号</summary>
            public ushort DeviceReadWriteStartNumber { get; set; }
            /// <summary>デバイス点数</summary>
            public ushort DeviceReadWriteCount { get; set; }
            /// <summary>書込データ</summary>
            public byte[] WriteWords { get; set; }
        }

        /// <summary>処理結果</summary>
        public struct Result
        {
            /// <summary>処理開始日時</summary>
            public DateTime RequestDateTime { get; set; }
            /// <summary>処理終了日時</summary>
            public DateTime ResponseDateTime { get; set; }
            /// <summary>終了コード（正常時は0）</summary>
            public short ReturnCode { get; set; }
            /// <summary>応答データ</summary>
            public string[] ReceivedWords { get; set; }
        }

        /// <summary>処理エントリーポイント</summary>
        public Interface.IResponse DoProcess(Interface.IRequest _request)
        {
            var parameter = (Parameter)_request.Parameter;
            var result = new Result { RequestDateTime = DateTime.Now };

            byte[] plcRequest;
            ushort expectedReceiveDataLength;
            try
            {
                var ret = McProtocolUtil.CreateRequest(parameter.CommandType, parameter.DeviceType, parameter.DeviceReadWriteStartNumber, parameter.DeviceReadWriteCount);
                plcRequest = ret.PlcRequest;
                expectedReceiveDataLength = ret.ExpectedReceiveDataLength;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            byte[] receivedData = new byte[expectedReceiveDataLength];
            int receivedDataLength = 0;
            try
            {
                using (var tcpClient = new TcpClient(parameter.PlcIPAddress, parameter.PlcPortNumber))
                {
                    // PLCに接続
                    var networkStream = tcpClient.GetStream();
                    networkStream.ReadTimeout = 500;
                    networkStream.WriteTimeout = 500;

                    // PLCにデータを送信
                    networkStream.Write(plcRequest, 0, plcRequest.Length);

                    // PLCの処理を待機
                    System.Threading.Thread.Sleep(parameter.PlcResponseWaitMilliSecond);

                    // PLCからデータを受信
                    while (networkStream.DataAvailable)
                    {
                        receivedDataLength += networkStream.Read(receivedData, receivedDataLength, receivedData.Length);
                    }

                    networkStream?.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            if (receivedDataLength == 0)
            {
                throw new System.IO.InvalidDataException("PLCからのデータ受信に失敗しました（頻発する場合、PLCデータ受信の待機時間が短すぎる可能性があります）");
            }

            try
            {
                var plcResponse = McProtocolUtil.ParseReceivedBytes(receivedData);
                result.ReturnCode = plcResponse.ReturnCode;
                result.ReceivedWords = plcResponse.ReceivedWords;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            result.ResponseDateTime = DateTime.Now;
            return new Response { IsSucceed = true, Result = result };
        }

    }
}

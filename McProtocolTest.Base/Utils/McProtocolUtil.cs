using System;
using System.Collections.Generic;

namespace McProtocolTest.Utils
{
    /// <summary>
    /// MCプロトコルユーティリティーライブラリ
    /// </summary>
    public static class McProtocolUtil
    {
        /// <summary>送受信電文ヘッダー部のバイト数</summary>
        private const ushort HEADER_BYTE_LENGTH = 11;

        /// <summary>コマンド種類（読出／書込／ランダム読出）</summary>
        public enum CommandType
        {
            #region コマンド種類
            /// <summary>ワード単位 一括読出</summary>
            READ_WORD,
            /// <summary>ビット単位 一括読出</summary>
            READ_BIT,
            /// <summary>ワード単位 一括書込</summary>
            WRITE_WORD,
            /// <summary>ビット単位 一括書込</summary>
            WRITE_BIT,
            /// <summary>ワード単位 ランダム読出</summary>
            READ_RANDOM,
            #endregion
        };

        /// <summary>デバイス種類</summary>
        public enum DeviceType
        {
            #region デバイス種類
            /// <summary>特殊リレー</summary>
            SM,
            /// <summary>特殊レジスタ</summary>
            SD,
            /// <summary>入力</summary>
            X,
            /// <summary>出力</summary>
            Y,
            /// <summary>内部リレー</summary>
            M,
            /// <summary>ラッチリレー</summary>
            L,
            /// <summary>アナンシェータ</summary>
            F,
            /// <summary>リンクリレー</summary>
            B,
            /// <summary>データレジスタ</summary>
            D,
            /// <summary>リンクレジスタ</summary>
            W,
            /// <summary>タイマ 接点</summary>
            TS,
            /// <summary>タイマ コイル</summary>
            TC,
            /// <summary>タイマ 現在値</summary>
            TN,
            /// <summary>カウンタ 接点</summary>
            CS,
            /// <summary>カウンタ コイル</summary>
            CC,
            /// <summary>カウンタ 現在値</summary>
            CN,
            /// <summary>リンク特殊リレー</summary>
            SB,
            /// <summary>リンク特殊レジスタ</summary>
            SW,
            /// <summary>ファイルレジスタ(ブロック)</summary>
            R,
            /// <summary>ファイルレジスタ(連番)</summary>
            ZR,
            #endregion
        };

        /// <summary>
        /// コマンド種類から電文のコマンド部分を生成します（上位と下位は反転せすに生成するので、呼出元でエンディアン変換してください）
        /// </summary>
        private static byte[] GetCommandPart(CommandType _commandType)
        {
            switch (_commandType)
            {
                case CommandType.READ_WORD  : return "0401".ToBytes();
                case CommandType.READ_BIT   : return "0401".ToBytes();
                case CommandType.WRITE_WORD : return "1401".ToBytes();
                case CommandType.WRITE_BIT  : return "1401".ToBytes();
                case CommandType.READ_RANDOM: return "0403".ToBytes();
                default                     : return "0000".ToBytes();
            }
        }

        /// <summary>
        /// コマンド種類から電文のサブコマンド部分を生成します（上位と下位は反転せすに生成するので、呼出元でエンディアン変換してください）
        /// </summary>
        private static byte[] GetSubCommandPart(CommandType _commandType)
        {
            switch (_commandType)
            {
                case CommandType.READ_WORD  : return "0000".ToBytes();
                case CommandType.READ_BIT   : return "0001".ToBytes();
                case CommandType.WRITE_WORD : return "0000".ToBytes();
                case CommandType.WRITE_BIT  : return "0001".ToBytes();
                case CommandType.READ_RANDOM: return "0000".ToBytes();
                default                     : return "0000".ToBytes();
            }
        }

        /// <summary>
        /// デバイス種類からデバイスコードを取得します
        /// </summary>
        private static byte GetDeviceCode(DeviceType _deviceType)
        {
            switch (_deviceType)
            {
                case DeviceType.SM: return 0x91;
                case DeviceType.SD: return 0xA9;
                case DeviceType.X : return 0x9C;
                case DeviceType.Y : return 0x9D;
                case DeviceType.M : return 0x90;
                case DeviceType.L : return 0x92;
                case DeviceType.F : return 0x93;
                case DeviceType.B : return 0xA0;
                case DeviceType.D : return 0xA8;
                case DeviceType.W : return 0xB4;
                case DeviceType.TS: return 0xC1;
                case DeviceType.TC: return 0xC0;
                case DeviceType.TN: return 0xC2;
                case DeviceType.CS: return 0xC4;
                case DeviceType.CC: return 0xC3;
                case DeviceType.CN: return 0xC5;
                case DeviceType.SB: return 0xA1;
                case DeviceType.SW: return 0xB5;
                case DeviceType.R : return 0xAF;
                case DeviceType.ZR: return 0xB0;
                default           : return 0x00;
            }
        }

        /// <summary>
        /// コマンド種類・デバイス点数から要求データ長を計算します
        /// </summary>
        private static ushort GetRequesteDataLength(CommandType _commandType, ushort _deviceReadWriteCount)
        {
            switch (_commandType)
            {
                case CommandType.READ_WORD:
                case CommandType.READ_BIT:
                    // 「CPU監視タイマー」と「要求データ（コマンド～デバイス点数）」のバイト数
                    return 2 + 10;
                case CommandType.WRITE_WORD:
                case CommandType.WRITE_BIT:
                    // 「CPU監視タイマー」と「要求データ（コマンド～デバイス点数）」「書込データ」のバイト数
                    return (ushort)(2 + 10 + _deviceReadWriteCount * 2);
                case CommandType.READ_RANDOM:
                    // 「CPU監視タイマー」と「要求データ（コマンド～デバイス点数）」のバイト数
                    return 2 + 10;
                default: return 0;
            };
        }

        /// <summary>
        /// デバイス読出／書込の電文を生成します
        /// </summary>
        /// <param name="_commandType">コマンド種類（読出／書込／ランダム読出）</param>
        /// <param name="_deviceType">デバイス種類</param>
        /// <param name="_deviceReadWriteStartNumber">先頭デバイス番号</param>
        /// <param name="_deviceReadWriteCount">デバイス点数</param>
        /// <param name="_writeWords">書込データ</param>
        /// <returns>送信電文、受信電文の予想データ長</returns>
        public static (byte[] PlcRequest, ushort ExpectedReceiveDataLength) CreateRequest(CommandType _commandType, DeviceType _deviceType, uint _deviceReadWriteStartNumber, ushort _deviceReadWriteCount, byte[] _writeWords = null)
        {
            #region 電文作成可能であるかのチェック（エラーがあれば例外をスローします）
            if (_writeWords == null)
            {
                switch (_commandType)
                {
                    case CommandType.WRITE_WORD:
                    case CommandType.WRITE_BIT:
                        throw new ArgumentException("コマンド「書込」を指定しましたが、書込データが null です");
                }
            }
            else
            {
                if (_writeWords.Length % 2 != 0)
                {
                    throw new ArgumentException($"書込データのサイズ { _writeWords.Length } は 2 の倍数にしてください");
                }
                if (_writeWords.Length / 2 != _deviceReadWriteCount)
                {
                    throw new ArgumentException($"指定したデバイス点数 { _deviceReadWriteCount } と書込データのサイズ { _writeWords.Length } が一致していません");
                }
            }
            #endregion

            var sendData = new List<byte>();
            #region 「書込データ」以外の部分の電文作成
            // x86 アーキテクチャでは、BitConverter.GetBytes の値をそのまま使用（「リトル・エンディアン」を返すので）
            sendData.AddRange("0050".ToBytes().Reverse()); // サブヘッダ
            sendData.AddRange("00".ToBytes());             // ネットワーク番号
            sendData.AddRange("FF".ToBytes());             // PC番号
            sendData.AddRange("03FF".ToBytes().Reverse()); // 要求先ユニット I/O番号
            sendData.AddRange("00".ToBytes());             // 要求先ユニット 局番号
            sendData.AddRange(BitConverter.GetBytes(GetRequesteDataLength(_commandType, _deviceReadWriteCount))); // 要求データ長
            sendData.AddRange("0010".ToBytes().Reverse());                // CPU監視タイマー
            sendData.AddRange(GetCommandPart(_commandType).Reverse());    // コマンド
            sendData.AddRange(GetSubCommandPart(_commandType).Reverse()); // サブコマンド
            sendData.AddRange(BitConverter.GetBytes(_deviceReadWriteStartNumber).AsSpan(0, 3).ToArray()); // 先頭デバイス番号
            sendData.Add(GetDeviceCode(_deviceType));                        // デバイスコード
            sendData.AddRange(BitConverter.GetBytes(_deviceReadWriteCount)); // デバイス点数
            #endregion

            #region 「書込データ」部分の電文作成
            switch (_commandType)
            {
                case CommandType.WRITE_WORD:
                case CommandType.WRITE_BIT:
                    int length = _writeWords.Length;
                    for (int i = 0; i < length - 1; i = i + 2)
                    {
                        sendData.Add(_writeWords[i + 1]); // 書込データ（下位）
                        sendData.Add(_writeWords[i + 0]); // 書込データ（上位）
                    }
                    break;
            }
            #endregion

            switch (_commandType)
            {
                case CommandType.WRITE_WORD:
                case CommandType.WRITE_BIT:
                    return (PlcRequest: sendData.ToArray(), ExpectedReceiveDataLength: HEADER_BYTE_LENGTH);
                default:
                    return (PlcRequest: sendData.ToArray(), ExpectedReceiveDataLength: (ushort)(HEADER_BYTE_LENGTH + _deviceReadWriteCount * 2));
            }
        }

        /// <summary>
        ///  デバイス読出／書込結果の受信電文を解析します
        /// </summary>
        /// <param name="_receivedBytes">受信電文</param>
        /// <returns>ReturnCode：終了コード、ReceivedWords：受信ワード</returns>
        public static (short ReturnCode, string[] ReceivedWords) ParseReceivedBytes(byte[] _receivedBytes)
        {
            #region 受信電文のチェック（エラーがあれば例外をスローします）
            if (_receivedBytes == null)
            {
                throw new ArgumentNullException(nameof(_receivedBytes));
            }
            if (_receivedBytes.Length < HEADER_BYTE_LENGTH)
            {
                throw new FormatException($"受信データのヘッダー部サイズが { HEADER_BYTE_LENGTH } バイト未満です");
            }

            int receivedDataLength = _receivedBytes.Length - HEADER_BYTE_LENGTH + 2; // 「データ部サイズ」=「受信電文」-「ヘッダー部」+「終了コード」のバイト数
            ushort receivedByteLength = BitConverter.ToUInt16(_receivedBytes, 7);    // 受信電文から「応答データ長」を取得
            if (receivedDataLength != receivedByteLength)
            {
                throw new FormatException($"受信データのデータ部サイズ { receivedDataLength } が受信データの「応答データ長」 { receivedByteLength } と一致しません");
            }
            if (receivedDataLength % 2 != 0)
            {
                throw new FormatException($"受信データのデータ部サイズ { receivedDataLength } が 2 の倍数ではありません");
            }
            #endregion

            var returnCode = BitConverter.ToInt16(_receivedBytes, 9); // 終了コード
            var receivedWords = new List<string>();                   // 受信ワード
            #region 受信電文の「受信ワード部」を取得
            for (int i = HEADER_BYTE_LENGTH; i < _receivedBytes.Length - 1; i = i + 2)
            {
                var bytes = _receivedBytes.AsSpan(i, 2);
                receivedWords.Add(bytes[1].ToString("X2") + bytes[0].ToString("X2")); // 受信ワード（下位）（上位）
            }
            #endregion

            return (ReturnCode: returnCode, ReceivedWords: receivedWords.ToArray());
        }

        /// <summary>
        /// 16進数表記の文字列をバイト配列に変換します
        /// </summary>
        private static byte[] ToBytes(this string _byteString)
        {
            int length = _byteString.Length;
            #region 引数の文字数が奇数の場合、頭に「0」を付ける
            if (length % 2 == 1)
            {
                _byteString = "0" + _byteString;
                length++;
            }
            #endregion

            List<byte> bytes = new List<byte>();
            #region 文字列を二個ずつバイト配列に変換
            for (int i = 0; i < length - 1; i = i + 2)
            {
                string buf = _byteString.Substring(i, 2);
                bytes.Add(Convert.ToByte(buf, 16));
            }
            #endregion

            return bytes.ToArray();
        }

        /// <summary>
        /// バイト配列の並びを反転します（エンディアン変換に利用してください）
        /// </summary>
        private static byte[] Reverse(this byte[] _bytes)
        {
            byte[] newBytes = new byte[_bytes.Length];
            _bytes.CopyTo(newBytes, 0);
            Array.Reverse(newBytes);
            return newBytes;
        }

    }
}

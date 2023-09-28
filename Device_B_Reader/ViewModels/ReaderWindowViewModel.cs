using McProtocolTest.Models.Logics;
using McProtocolTest.Utils;
using System.IO;
using System.Linq;

namespace McProtocolTest.ViewModels
{
    class ReaderWindowViewModel : ReaderWindowViewModelBase
    {
        /// <summary>
        /// タイトルバーに表示する文字列を生成します
        /// </summary>
        protected override string CreateWindowTitle()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string versionString = string.Join(".", System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion.Split('.').Take(3));
            return $"{ Path.GetFileNameWithoutExtension(assembly.Location) } (ver.{ versionString })  Device B Reading Test";
        }

        /// <summary>
        /// 「PLCのデバイス読出／書込処理」の「処理依頼パラメーター」を生成します
        /// </summary>
        protected override ReadWritePlcDevice.Parameter CreateReadWritePlcDeviceParameter()
        {
            return new ReadWritePlcDevice.Parameter
            {
                PlcIPAddress = base.PlcIPAddress.Value,
                PlcPortNumber = base.PlcPortNumber.Value,
                PlcResponseWaitMilliSecond = base.PlcResponseWaitMilliSecond.Value,
                CommandType = McProtocolUtil.CommandType.READ_WORD,
                DeviceType = McProtocolUtil.DeviceType.B,
                DeviceReadWriteStartNumber = base.DeviceReadWriteStartNumber.Value,
                DeviceReadWriteCount = base.DeviceReadWriteCount.Value,
            };
        }

    }
}

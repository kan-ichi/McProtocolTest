using McProtocolTest.Models.Interface;
using McProtocolTest.Models.Logics;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace McProtocolTest.ViewModels
{
    abstract class ReaderWindowViewModelBase : IDisposable
    {
        #region view binding items

        public ReactivePropertySlim<string> WindowTitle { get; set; }
        public ReactiveCommand WindowContentRendered { get; set; }
        public ReactiveCommand WindowClosing { get; set; }

        public ReactivePropertySlim<string> PlcIPAddress { get; set; }
        public ReactivePropertySlim<ushort> PlcPortNumber { get; set; }
        public ReactivePropertySlim<ushort> DeviceReadWriteStartNumber { get; set; }
        public ReactivePropertySlim<ushort> DeviceReadWriteCount { get; set; }
        public ReactivePropertySlim<ushort> PlcDeviceAccessTimerIntervalMilliSecond { get; set; }
        public ReactivePropertySlim<ushort> PlcResponseWaitMilliSecond { get; set; }

        public ReactivePropertySlim<DateTime> RequestDateTime { get; set; }
        public ReactivePropertySlim<int> ElapsedTimeMilliSecond { get; set; }
        public ReactivePropertySlim<string[]> ReceivedWords { get; set; }
        public ReactivePropertySlim<string> LogMessage { get; set; }

        public ReactivePropertySlim<bool> IsPlcDeviceDisconnected { get; set; }
        public ReactiveCommand ConnectPlcDeviceClick { get; set; }
        public ReactiveCommand DisconnectPlcDeviceClick { get; set; }

        #endregion

        #region クラス内変数・コンストラクタ

        /// <summary>PLCアクセス処理用タイマー</summary>
        private DispatcherTimer PlcDeviceAccessTimer;

        /// <summary>「PLCアクセス処理中であるか」の判定フラグ</summary>
        protected bool IsPlcDeviceAccessInProgress = false;

        /// <summary>「ログ」表示エリアの内容</summary>
        private List<string> LogMessages = new List<string>();

        /// <summary>「ログ」表示エリアの最大行数</summary>
        private const int LOG_MESSAGE_MAX_LINE = 10000;

        private CompositeDisposable Disposables = new CompositeDisposable();

        void IDisposable.Dispose() { this.Disposables.Dispose(); }

        public ReaderWindowViewModelBase()
        {
            this.InitializeBindings();
        }

        #endregion

        #region イベント処理

        /// <summary>
        /// ウインドウ初期表示時の処理
        /// </summary>
        private void Window_ContentRendered()
        {
            this.PreparePlcDeviceAccessTimer();
        }

        /// <summary>
        /// ウインドウを閉じる時の処理
        /// </summary>
        private void Window_Closing()
        {
            this.PlcDeviceAccessTimer?.Stop();
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// タイマーにより行うPLCへのアクセス処理
        /// </summary>
        private void PlcDeviceAccessTimer_Tick(object _sender, EventArgs _e)
        {
            if (this.IsPlcDeviceAccessInProgress)
            {
                // 前回の処理が終わっていないので、今回の処理はスキップ
            }
            else
            {
                this.AccessPlcDevice();
            }
        }

        /// <summary>
        /// ボタン〔Connect〕押下処理
        /// </summary>
        private void ConnectPlcDevice_Click()
        {
            this.IsPlcDeviceDisconnected.Value = false;

            if (this.PlcDeviceAccessTimer != null)
            {
                this.PlcDeviceAccessTimer.Interval = TimeSpan.FromMilliseconds(this.PlcDeviceAccessTimerIntervalMilliSecond.Value);
                this.PlcDeviceAccessTimer.Start();
            }
        }

        /// <summary>
        /// ボタン〔Disconnect〕押下処理
        /// </summary>
        private void DisconnectPlcDevice_Click()
        {
            this.IsPlcDeviceDisconnected.Value = true;

            this.PlcDeviceAccessTimer?.Stop();
        }

        #endregion

        #region 各種メソッド

        /// <summary>
        /// タイトルバーに表示する文字列を生成します
        /// </summary>
        protected abstract string CreateWindowTitle();

        /// <summary>
        /// PLCアクセス処理用タイマーを準備します
        /// </summary>
        private void PreparePlcDeviceAccessTimer()
        {
            if (this.PlcDeviceAccessTimer == null)
            {
                this.PlcDeviceAccessTimer = new DispatcherTimer(DispatcherPriority.Normal);
                this.PlcDeviceAccessTimer.Tick += new EventHandler(this.PlcDeviceAccessTimer_Tick);
            }
            else
            {
                this.PlcDeviceAccessTimer.Stop();
            }
            this.PlcDeviceAccessTimer.Interval = TimeSpan.FromMilliseconds(this.PlcDeviceAccessTimerIntervalMilliSecond.Value);
        }

        /// <summary>
        /// 「PLCのデバイス読出／書込処理」の「処理依頼パラメーター」を生成します
        /// </summary>
        protected abstract ReadWritePlcDevice.Parameter CreateReadWritePlcDeviceParameter();

        /// <summary>
        /// PLCにアクセスし、デバイス操作を行います
        /// </summary>
        private async void AccessPlcDevice()
        {
            this.IsPlcDeviceAccessInProgress = true;

            var logicParameter = this.CreateReadWritePlcDeviceParameter();
            var logic = new ReadWritePlcDevice();
            IResponse logicResponse = null;
            try
            {
                Task<IResponse> logicDoProcess = Task.Run(() =>
                {
                    return logic.DoProcess(new Request { Parameter = logicParameter });
                });
                logicResponse = await logicDoProcess;
            }
            catch (Exception ex)
            {
                this.AppendLogMessageWithTimeStamp(ex.Message.Replace(Environment.NewLine, string.Empty), DateTime.Now);
                this.IsPlcDeviceAccessInProgress = false;
                return;
            }

            if (logicResponse.IsSucceed)
            {
                var logicResult = (ReadWritePlcDevice.Result)logicResponse.Result;
                this.RequestDateTime.Value = logicResult.RequestDateTime;
                this.ElapsedTimeMilliSecond.Value = (logicResult.ResponseDateTime - logicResult.RequestDateTime).Milliseconds;
                this.ReceivedWords.Value = logicResult.ReceivedWords;
            }

            this.IsPlcDeviceAccessInProgress = false;
        }

        /// <summary>
        /// ログメッセージ表示エリアにメッセージを追加します
        /// </summary>
        private void AppendLogMessageWithTimeStamp(string _msg, DateTime _timeStamp)
        {
            string appendMessage = _timeStamp.ToString("yyyy/MM/dd HH:mm:ss.fff") + " " + _msg;

            this.LogMessages.Add(appendMessage);
            if (this.LogMessages.Count > LOG_MESSAGE_MAX_LINE + 100)
            {
                // 「ログ」表示エリアの古いメッセージ削除は、ある程度まとまってから行う（時間のかかる処理なので）
                this.LogMessages.RemoveRange(0, 100);
            }
            this.LogMessage.Value = string.Join(Environment.NewLine, this.LogMessages);
        }

        #region ビューにバインドされている項目を初期化

        /// <summary>
        /// ビューにバインドされている項目を初期化します
        /// </summary>
        private void InitializeBindings()
        {
            this.WindowTitle = new ReactivePropertySlim<string>().AddTo(this.Disposables);
            this.WindowTitle.Value = this.CreateWindowTitle();
            this.WindowContentRendered = new ReactiveCommand().AddTo(this.Disposables).WithSubscribe(this.Window_ContentRendered);
            this.WindowClosing = new ReactiveCommand().AddTo(this.Disposables).WithSubscribe(this.Window_Closing);

            this.PlcIPAddress = new ReactivePropertySlim<string>("127.0.0.1").AddTo(this.Disposables);
            this.PlcPortNumber = new ReactivePropertySlim<ushort>(5000).AddTo(this.Disposables);
            this.DeviceReadWriteStartNumber = new ReactivePropertySlim<ushort>(0).AddTo(this.Disposables);
            this.DeviceReadWriteCount = new ReactivePropertySlim<ushort>(960).AddTo(this.Disposables);
            this.PlcDeviceAccessTimerIntervalMilliSecond = new ReactivePropertySlim<ushort>(200).AddTo(this.Disposables);
            this.PlcResponseWaitMilliSecond = new ReactivePropertySlim<ushort>(100).AddTo(this.Disposables);

            this.RequestDateTime = new ReactivePropertySlim<DateTime>().AddTo(this.Disposables);
            this.ElapsedTimeMilliSecond = new ReactivePropertySlim<int>().AddTo(this.Disposables);
            this.ReceivedWords = new ReactivePropertySlim<string[]>().AddTo(this.Disposables);
            this.LogMessage = new ReactivePropertySlim<string>().AddTo(this.Disposables);

            this.IsPlcDeviceDisconnected = new ReactivePropertySlim<bool>(true).AddTo(this.Disposables);

            this.ConnectPlcDeviceClick = this.IsPlcDeviceDisconnected.
                ToReactiveCommand().AddTo(this.Disposables).
                WithSubscribe(() => this.ConnectPlcDevice_Click());

            this.DisconnectPlcDeviceClick = this.IsPlcDeviceDisconnected.Inverse().
                ToReactiveCommand().AddTo(this.Disposables).
                WithSubscribe(() => this.DisconnectPlcDevice_Click());
        }

        #endregion

        #endregion

    }
}

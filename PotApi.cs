#if DEBUG
using System.Diagnostics;
#endif
using System.Runtime.InteropServices;

namespace PotApi;
public class PotApi : IDisposable {

    public EventHandler<PlaybackEventArgs> PlaybackStateChanged;
    public class PlaybackEventArgs(PlayBackState state, nint time) : EventArgs {
        public PlayBackState State { get; set; } = state;
        public nint Time { get; set; } = time;
    }
    public enum PlayBackState {
        Stop,
        Pause,
        Play
    }

    private CancellationTokenSource _cts;

    public void StartListener() {
        var currentTime = GetCurrentTime();
        var playbackEventargs = new PlaybackEventArgs((PlayBackState)GetPlayStatus(), currentTime);
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        Task.Factory.StartNew(async () => {
            while (true) {
                await Task.Delay(500); // Time is not very accurate so have to have some delay.
                if (ct.IsCancellationRequested) break;
                var cTime = GetCurrentTime();

                playbackEventargs.Time = cTime;
                var state = (PlayBackState)GetPlayStatus();

                if (state != playbackEventargs.State) {
                    playbackEventargs.State = state;
                    PlaybackStateChanged?.Invoke(this, playbackEventargs);
                } else if (cTime != currentTime) {
                    PlaybackStateChanged?.Invoke(this, playbackEventargs);
                }

                currentTime = cTime;
            }
        });
    }

    public void StopListener() {
        _cts.Cancel();
        _cts.Dispose();
    }

    public static class WinApi {
        [DllImport("user32", CharSet = CharSet.Ansi, EntryPoint = "SendMessageA")]
        public static extern nint SendMessage(nint hWnd, uint dwMsg, nuint wParam, nint lParam = 0);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out uint processId);
    }

    public static class Constants {
        public const int WM_USER = 0x0400;
        public const int WM_COPYDATA = 0x4A;

        // Default command
        //
        // int Volume = SendMessage(hWnd, POT_COMMAND, POT_GET_VOLUME, 0);
        // POT_GET_XXX only support SendMessage
        //
        // Send(Post)Message(hWnd, POT_COMMAND, POT_SET_VOLUME, Volume);
        // POT_SET_XXX support SendMessage or PostMessage
        public const uint POT_COMMAND = WM_USER;
        public const nuint POT_GET_VOLUME = 0x5000; // 0 ~ 100
        public const nuint POT_SET_VOLUME = 0x5001; // 0 ~ 100
        public const nuint POT_GET_TOTAL_TIME = 0x5002; // ms unit
        public const nuint POT_GET_PROGRESS_TIME = 0x5003; // ms unit
        public const nuint POT_GET_CURRENT_TIME = 0x5004; // ms unit
        public const nuint POT_SET_CURRENT_TIME = 0x5005; // ms unit
        public const nuint POT_GET_PLAY_STATUS = 0x5006; // 0:Stopped, 1:Paused, 2:Running
        public const nuint POT_SET_PLAY_STATUS = 0x5007; // 0:Toggle, 1:Paused, 2:Running
        public const nuint POT_SET_PLAY_ORDER = 0x5008; // 0:Prev, 1:Next
        public const nuint POT_SET_PLAY_CLOSE = 0x5009;

        // POT_VIRTUAL_KEY_XXX is available from version 2023-08-30
        public const nuint POT_VIRTUAL_KEY_SHIFT = 0x0100;
        public const nuint POT_VIRTUAL_KEY_CONTROL = 0x0200;
        public const nuint POT_VIRTUAL_KEY_ALT = 0x0400;
        public const nuint POT_VIRTUAL_KEY_EXT = 0x0800;
        public const nuint POT_SEND_VIRTUAL_KEY = 0x5010; // Virtual Key(VK_UP, VK_DOWN....) | POT_VIRTUAL_KEY_XXXX

        public const nuint POT_GET_MUTE = 0x5011; // 0: none, 1:mute
        public const nuint POT_SET_MUTE = 0x5012; // 0: none, 1:mute
        public const nuint POT_GET_OSD = 0x5013; // 0: none, 1:all, 2:simple
        public const nuint POT_SET_OSD = 0x5014; // 0: none, 1:all, 2:simple

        public const nuint POT_GET_AVISYNTH_USE = 0x6000;
        public const nuint POT_SET_AVISYNTH_USE = 0x6001; // 0: off, 1:on
        public const nuint POT_GET_VAPOURSYNTH_USE = 0x6010;
        public const nuint POT_SET_VAPOURSYNTH_USE = 0x6011; // 0: off, 1:on
        public const nuint POT_GET_VIDEO_WIDTH = 0x6030;
        public const nuint POT_GET_VIDEO_HEIGHT = 0x6031;
        public const nuint POT_GET_VIDEO_FPS = 0x6032;// scale by 1000

        // String getting
        // Send(Post)Message(hWnd, POT_COMMAND, POT_GET_XXXXX, (WPARAM)ReceiveHWND);
        // then PotPlayer call SendMessage(ReceiveHWND, WM_COPY_DATA, string(utf8) data...
        // COPYDATASTRUCT::dwData is POT_GET_XXXXX
        public const nuint POT_GET_AVISYNTH_SCRIPT = 0x6002;
        public const nuint POT_GET_VAPOURSYNTH_SCRIPT = 0x6012;
        public const nuint POT_GET_PLAYFILE_NAME = 0x6020;

        // String setting... Using WM_COPYDATA
        // COPYDATASTRUCT cds = { 0, };
        // cds.dwData = POT_SET_xxxxxxxx;
        // cds.cbData = urf8.GetLength();
        // cds.lpData = (void *)(LPCSTR)urf8;
        // SendMessage(hWnd, WM_COPYDATA, hwnd, (WPARAM)&cds); 
        public const nuint POT_SET_AVISYNTH_SCRIPT = 0x6003;
        public const nuint POT_SET_VAPOURSYNTH_SCRIPT = 0x6013;
        public const nuint POT_SET_SHOW_MESSAGE = 0x6040;

        public const uint POT_CMD = 0x0111;
        public const nuint POT_OPEN_FILE = 0x27AE;
    }
    public struct COPYDATASTRUCT {
        public IntPtr dwData;
        public int cbData;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpData;
    }

    public class PotWndProcReceiver : Form {
        private PotApi _api;
        public PotWndProcReceiver(PotApi api) { _api = api; SetFormProperties(); }

        private void SetFormProperties() {
            Text = "";
            Width = 0;
            Height = 0;
            ShowIcon = false;
            ShowInTaskbar = false;
            Opacity = 0.0;
            Visible = false;
            ControlBox = false;
            MaximizeBox = false;
            MinimizeBox = false;
            FormBorderStyle = FormBorderStyle.None;
        }

        protected override void WndProc(ref Message m) {
            try {
                _api.WndProc(ref m);
                base.WndProc(ref m);
            } catch (Exception e) {
#if DEBUG
                Debug.WriteLine(e.Message);
#endif
            }
        }

    }

    public IntPtr Hwnd { get; set; }
    public nint ProcReceiverHwnd { get; set; }
    public PotApi(IntPtr hWnd) {
        Hwnd = hWnd;
        _procReceiver = new PotWndProcReceiver(this);
        _procReceiver.HandleCreated += (object? sender, EventArgs e) => {
#if DEBUG
            Debug.WriteLine($"Handle Created: {e}");
#endif
            ProcReceiverHwnd = _procReceiver.Handle;
        };
#if DEBUG
        Debug.WriteLine(_procReceiver.Handle);
#endif
        ProcReceiverHwnd = _procReceiver.Handle;
    }
    private PotWndProcReceiver _procReceiver;

    public nint SendCommand(uint command, nuint wParam, nint lParam = 0) => WinApi.SendMessage(Hwnd, command, wParam, lParam);
    public nint SendCommand(nuint wParam, nint lParam = 0) => WinApi.SendMessage(Hwnd, Constants.POT_COMMAND, wParam, lParam);

    public void SetVolume(int volume) => SendCommand(Constants.POT_SET_VOLUME, volume);
    public nint GetVolume() => SendCommand(Constants.POT_GET_VOLUME);
    public void Mute() => SendCommand(Constants.POT_SET_MUTE, 1);
    public void UnMute() => SendCommand(Constants.POT_SET_MUTE, 0);
    public bool Muted() => SendCommand(Constants.POT_GET_MUTE) == 1;

    public nint GetTotalTime() => SendCommand(Constants.POT_GET_TOTAL_TIME);
    public nint GetProgressTime() => SendCommand(Constants.POT_GET_PROGRESS_TIME);
    public nint GetCurrentTime() => SendCommand(Constants.POT_GET_CURRENT_TIME);
    public void SetCurrentTime(nint ms) => SendCommand(Constants.POT_SET_CURRENT_TIME, ms);
    public nint GetPlayStatus() => SendCommand(Constants.POT_GET_PLAY_STATUS);
    public void PlayPause() => SendCommand(Constants.POT_SET_PLAY_STATUS, 0);
    public void Play() => SendCommand(Constants.POT_SET_PLAY_STATUS, 1);
    public void Pause() => SendCommand(Constants.POT_SET_PLAY_STATUS, 2);
    public void PlayPrevious() => SendCommand(Constants.POT_SET_PLAY_ORDER, 0);
    public void PlayNext() => SendCommand(Constants.POT_SET_PLAY_ORDER, 1);
    public void PlayClose() => SendCommand(Constants.POT_SET_PLAY_CLOSE);

    public void SetShift() => SendCommand(Constants.POT_VIRTUAL_KEY_SHIFT);
    public void SetCtrl() => SendCommand(Constants.POT_VIRTUAL_KEY_CONTROL);
    public void SetAlt() => SendCommand(Constants.POT_VIRTUAL_KEY_ALT);
    public void SendKey(nint key) => SendCommand(Constants.POT_SEND_VIRTUAL_KEY, key);

    public OsdState GetOsdState() => (OsdState)SendCommand(Constants.POT_GET_OSD);
    public void SetOsdState(OsdState state) => SendCommand(Constants.POT_SET_OSD, (nint)state);

    public void GetAvisynthUse() => SendCommand(Constants.POT_GET_AVISYNTH_USE);
    public void EnableAvisynthUse() => SendCommand(Constants.POT_SET_AVISYNTH_USE, 1);
    public void DisableAvisynthUse() => SendCommand(Constants.POT_SET_AVISYNTH_USE, 0);

    public void GetVapoursynthUse() => SendCommand(Constants.POT_GET_VAPOURSYNTH_USE);
    public void EnableVapoursynthUse() => SendCommand(Constants.POT_SET_VAPOURSYNTH_USE, 1);
    public void DisableVapoursynthUse() => SendCommand(Constants.POT_SET_VAPOURSYNTH_USE, 0);

    public nint GetVideoWidth() => SendCommand(Constants.POT_GET_VIDEO_WIDTH);
    public nint GetVideoHeight() => SendCommand(Constants.POT_GET_VIDEO_HEIGHT);
    public nint GetVideoFps() => SendCommand(Constants.POT_GET_VIDEO_FPS);
    public void ShowOpenFileDialog() => SendCommand(Constants.POT_CMD, Constants.POT_OPEN_FILE);

    private string _response = string.Empty;
    private Task<T> Tasker<T>(Func<T> func) {
        return Task.FromResult(func());
    }
    public string GetVapourSynthScript() {
        _response = string.Empty;
        SendCommand(Constants.POT_GET_VAPOURSYNTH_SCRIPT, ProcReceiverHwnd);
        return Tasker(() => {
            var timeOut = 1000;
            while (_response == string.Empty) {
                timeOut -= 10;
                if (timeOut <= 0) return "Request timed out";
                Thread.Sleep(10);
            }
            return _response;
        }).Result;
    }

    public string GetAviSynthScript() {
        _response = string.Empty;
        SendCommand(Constants.POT_GET_AVISYNTH_SCRIPT, ProcReceiverHwnd);
        return Tasker(() => {
            var timeOut = 1000;
            while (_response == string.Empty) {
                timeOut -= 10;
                if (timeOut <= 0) return "Request timed out";
                Thread.Sleep(10);
            }
            return _response;
        }).Result;
    }

    public string GetPlayingFileName() {
        _response = string.Empty;
        SendCommand(Constants.POT_GET_PLAYFILE_NAME, ProcReceiverHwnd);
        return Tasker(() => {
            var timeOut = 1000;
            while (_response == string.Empty) {
                timeOut -= 10;
                if (timeOut <= 0) return "Request timed out";
                Thread.Sleep(10);
            }
            return _response;
        }).Result;
    }

    public void WndProc(ref Message m) {
        switch (m.Msg) {
            case Constants.WM_COPYDATA:
                try {
                    var data = Marshal.PtrToStructure<COPYDATASTRUCT>(m.LParam);
                    _response = data.lpData;
                } catch (Exception ex) {
                    _response = ex.Message;
                }
                break;
        }
    }

    public void Dispose() {
        _cts.Cancel();
        _cts.Dispose();
        _procReceiver.Close();
        _procReceiver.Dispose();
    }

    public enum OsdState {
        None,
        Simple,
        All
    }

    public string? GetProcessPath(IntPtr hwnd) {
        WinApi.GetWindowThreadProcessId(hwnd, out uint pid);
        Process proc = Process.GetProcessById((int)pid);
        return proc.MainModule?.FileName;
    }

    public void PlayFile(string filePath, bool current = true) {
        var procPath = GetProcessPath(Hwnd);
        if(procPath == null) return;
        Process.Start($""""""{procPath}" "{filePath}" {(current ? "/current" : "/new")}"""""");
    }
    public void AddFile(string filePath) {
        var procPath = GetProcessPath(Hwnd);
        if (procPath == null) return;
        Process.Start($""""""{procPath}" "{filePath}" /add"""""");
    }

}

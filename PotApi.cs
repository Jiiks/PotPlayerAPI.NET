#define DONT_USE_FORMS

using System.Runtime.InteropServices;

namespace PotApi;

#if USE_FORMS
public struct COPYDATASTRUCT {
    public IntPtr dwData;
    public int cbData;
    [MarshalAs(UnmanagedType.LPStr)]
    public string lpData;
}
#endif

#if USE_FORMS
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
        _api.WndProc(ref m);
        base.WndProc(ref m);
    }

}
#endif

public class PotConstants {

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
}

public static class PotWinApi {
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, nuint wParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, nuint wParam, nint lParam);
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, nuint wParam, uint lParam);
}

#if USE_FORMS
public class PotApi {
    public IntPtr Hwnd { get; set; }
    public PotApi(IntPtr hWnd) {
        Hwnd = hWnd;
        _procReceiver = new PotWndProcReceiver(this);
    }
    private PotWndProcReceiver _procReceiver;
#else
public class PotApi(IntPtr hWnd) {
    public IntPtr Hwnd { get; set; } = hWnd;
#endif



    public void SetVolume(int volume) => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_SET_VOLUME, volume);
    public nint GetVolume() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_GET_VOLUME);
    public void Mute() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_SET_MUTE, 1);
    public void UnMute() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_SET_MUTE, 0);
    public bool Muted() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_GET_MUTE) == 1;

    public nint GetTotalTime() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_GET_TOTAL_TIME);
    public nint GetProgressTime() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_GET_PROGRESS_TIME);
    public nint GetCurrentTime() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_GET_CURRENT_TIME);
    public void SetCurrentTime(nint ms) => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_SET_CURRENT_TIME, ms);

    public void PlayPause() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_SET_PLAY_STATUS, 0);
    public void Play() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_SET_PLAY_STATUS, 1);
    public void Pause() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_SET_PLAY_STATUS, 2);
    public void PlayPrevious() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_SET_PLAY_ORDER, 0);
    public void PlayNext() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_SET_PLAY_ORDER, 1);
    public void PlayClose() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_SET_PLAY_CLOSE);

    public void SetShift() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_VIRTUAL_KEY_SHIFT);
    public void SetCtrl() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_VIRTUAL_KEY_CONTROL);
    public void SetAlt() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_VIRTUAL_KEY_ALT);
    public void SendKey(nint key) => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_SEND_VIRTUAL_KEY, key);

    public OsdState GetOsdState() => (OsdState)PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_GET_OSD);
    public void SetOsdState(OsdState state) => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_SET_OSD, (nint)state);

    public void GetAvisynthUse() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_GET_AVISYNTH_USE);
    public void EnableAvisynthUse() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_SET_AVISYNTH_USE, 1);
    public void DisableAvisynthUse() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_SET_AVISYNTH_USE, 0);

    public void GetVapoursynthUse() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_GET_VAPOURSYNTH_USE);
    public void EnableVapoursynthUse() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_SET_VAPOURSYNTH_USE, 1);
    public void DisableVapoursynthUse() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_SET_VAPOURSYNTH_USE, 0);

    public nint GetVideoWidth() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_GET_VIDEO_WIDTH);
    public nint GetVideoHeight() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_GET_VIDEO_HEIGHT);
    public nint GetVideoFps() => PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_GET_VIDEO_FPS) * 1000;

    //These don't seem to trigger SendMessage :c
    // Send(Post)Message(hWnd, POT_COMMAND, POT_GET_XXXXX, (WPARAM)ReceiveHWND);
    // then PotPlayer call SendMessage(ReceiveHWND, WM_COPY_DATA, string(utf8) data...
    // COPYDATASTRUCT::dwData is POT_GET_XXXXX
#if USE_FORMS
    public void GetVapurSynthScript() {
        PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_GET_VAPOURSYNTH_SCRIPT, _procReceiver.Handle);
    }

    public void GetPlayingFileName() {
        PotWinApi.SendMessage(Hwnd, PotConstants.POT_COMMAND, PotConstants.POT_GET_PLAYFILE_NAME, _procReceiver.Handle);
    }

    public void WndProc(ref Message m) {
        switch(m.Msg) {
            case PotConstants.WM_COPYDATA:
                
                break;
        }
        Console.WriteLine($"Window message received  {m.HWnd} {m.Msg} {m.LParam} {m.WParam}");
    }
#endif

    public enum OsdState {
        None,
        Simple,
        All
    }


}

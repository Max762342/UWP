/*
 * xb_shim.c - XB-Launcher native helper.
 *
 * Small C DLL compiled with MinGW. Provides Xbox-gamepad state polling and a
 * fullscreen helper used by the WPF shell when running on Xbox hardware.
 *
 * Build:
 *   x86_64-w64-mingw32-gcc -shared -O2 -o xb_shim.dll xb_shim.c -lwinmm -luser32
 */

#include <windows.h>
#include <mmsystem.h>
#include <xinput.h>
#include <stdbool.h>

#define XBSHIM_API __declspec(dllexport)

/* ---- Gamepad state ---- */

typedef struct {
    int connected;
    int buttons;        /* bitfield: A=1 B=2 X=4 Y=8 LB=16 RB=32 Back=64 Start=128 */
    float leftX;
    float leftY;
    float rightX;
    float rightY;
    float leftTrigger;
    float rightTrigger;
} XbGamepad;

XBSHIM_API bool xb_gamepad_poll(int index, XbGamepad* out) {
    if (!out) return false;
    ZeroMemory(out, sizeof(*out));

    XINPUT_STATE state;
    ZeroMemory(&state, sizeof(state));
    if (XInputGetState((DWORD)index, &state) != ERROR_SUCCESS) {
        out->connected = 0;
        return true;
    }

    out->connected = 1;
    out->buttons = (int)state.Gamepad.wButtons;
    out->leftX = state.Gamepad.sThumbLX / 32767.0f;
    out->leftY = state.Gamepad.sThumbLY / 32767.0f;
    out->rightX = state.Gamepad.sThumbRX / 32767.0f;
    out->rightY = state.Gamepad.sThumbRY / 32767.0f;
    out->leftTrigger = state.Gamepad.bLeftTrigger / 255.0f;
    out->rightTrigger = state.Gamepad.bRightTrigger / 255.0f;
    return true;
}

XBSHIM_API int xb_gamepad_count(void) {
    int n = 0;
    for (int i = 0; i < 4; i++) {
        XINPUT_STATE s;
        if (XInputGetState((DWORD)i, &s) == ERROR_SUCCESS) n++;
    }
    return n;
}

/* ---- Window helpers ---- */

XBSHIM_API void xb_window_fullscreen(HWND hwnd) {
    if (!hwnd) return;
    SetWindowLongPtrW(hwnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);
    SetWindowPos(hwnd, HWND_TOP, 0, 0,
                 GetSystemMetrics(SM_CXSCREEN),
                 GetSystemMetrics(SM_CYSCREEN),
                 SWP_FRAMECHANGED | SWP_SHOWWINDOW);
}

XBSHIM_API void xb_window_windowed(HWND hwnd, int x, int y, int w, int h) {
    if (!hwnd) return;
    SetWindowLongPtrW(hwnd, GWL_STYLE, WS_OVERLAPPEDWINDOW | WS_VISIBLE);
    SetWindowPos(hwnd, HWND_TOP, x, y, w, h,
                 SWP_FRAMECHANGED | SWP_SHOWWINDOW);
}

XBSHIM_API void xb_beep(int freq, int ms) {
    Beep((DWORD)freq, (DWORD)ms);
}

BOOL WINAPI DllMain(HINSTANCE h, DWORD reason, LPVOID reserved) {
    (void)h; (void)reason; (void)reserved;
    return TRUE;
}

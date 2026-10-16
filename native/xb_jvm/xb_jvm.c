// xb_jvm.c - Native bridge that embeds the HotSpot JVM in-process and calls a
// static main(String[]). Used by XB-Launcher's UWP/Xbox path where java.exe
// cannot be spawned as a child process but LoadLibrary + JNI are permitted.
//
// Build (x64, from a VS x64 Native Tools prompt):
//   cl /LD /O2 /I"%JAVA_HOME%\include" /I"%JAVA_HOME%\include\win32" ^
//      xb_jvm.c jvm.lib /Fe:xb_jvm.dll

#include <jni.h>
#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdarg.h>

// JNI_VERSION_21 may not be defined in older jni.h headers. Define it if missing.
#ifndef JNI_VERSION_21
#define JNI_VERSION_21 0x00150000
#endif

static JavaVM* g_vm = NULL;
static volatile LONG g_running = 0;

typedef jint(JNICALL* CreateJavaVM_fn)(JavaVM**, void**, void*);

static void LogLine(const char* logFile, const char* fmt, ...)
{
    if (!logFile || !*logFile) return;
    char buf[2048];
    va_list ap;
    va_start(ap, fmt);
    vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    FILE* f = NULL;
    if (fopen_s(&f, logFile, "a") == 0 && f) {
        fputs(buf, f);
        fputc('\n', f);
        fclose(f);
    }
}

static char* WToUtf8(const wchar_t* w)
{
    if (!w) return NULL;
    int n = WideCharToMultiByte(CP_UTF8, 0, w, -1, NULL, 0, NULL, NULL);
    char* s = (char*)malloc(n > 0 ? (size_t)n : 1);
    if (!s) return NULL;
    WideCharToMultiByte(CP_UTF8, 0, w, -1, s, n, NULL, NULL);
    return s;
}

__declspec(dllexport) const char* XbBridgeVersion(void)
{
    return "xb_jvm 1.0";
}

__declspec(dllexport) int XbIsGameRunning(void)
{
    return InterlockedCompareExchange(&g_running, 0, 0) != 0;
}

__declspec(dllexport) void XbRequestStop(void)
{
    // Best effort; the embedded game owns its own threads.
}

__declspec(dllexport) int XbLaunchGame(
    const wchar_t* jvmDllPath,
    const wchar_t* jreDir,
    const wchar_t* classpath,
    const wchar_t* mainClass,
    const wchar_t** jvmOptions, int jvmOptionCount,
    const wchar_t** gameArgs, int gameArgCount,
    const wchar_t* logFileUtf16)
{
    char* logFile = WToUtf8(logFileUtf16);
    LogLine(logFile, "xb_jvm: booting JVM from %ls", jvmDllPath);

    if (g_vm) {
        LogLine(logFile, "xb_jvm: a JVM already exists in this process.");
        free(logFile);
        return JNI_EEXIST;
    }

    HMODULE jvmMod = LoadLibraryW(jvmDllPath);
    if (!jvmMod) {
        LogLine(logFile, "xb_jvm: LoadLibrary(jvm.dll) failed err=%lu", GetLastError());
        free(logFile);
        return -100;
    }

    CreateJavaVM_fn createVm = (CreateJavaVM_fn)GetProcAddress(jvmMod, "JNI_CreateJavaVM");
    if (!createVm) {
        LogLine(logFile, "xb_jvm: GetProcAddress(JNI_CreateJavaVM) failed err=%lu", GetLastError());
        free(logFile);
        return -101;
    }

    int n = jvmOptionCount + 1;
    JavaVMOption* opts = (JavaVMOption*)calloc((size_t)n, sizeof(JavaVMOption));
    int idx = 0;
    for (int i = 0; i < jvmOptionCount; ++i) {
        char* o8 = WToUtf8(jvmOptions[i]);
        opts[idx++].optionString = _strdup(o8 ? o8 : "");
        free(o8);
    }
    char cp[8192];
    char* cp8 = WToUtf8(classpath);
    snprintf(cp, sizeof(cp), "-Djava.class.path=%s", cp8 ? cp8 : "");
    free(cp8);
    opts[idx++].optionString = _strdup(cp);

    JavaVMInitArgs vmArgs;
    memset(&vmArgs, 0, sizeof(vmArgs));
    vmArgs.version = JNI_VERSION_21;  // BanditLauncher uses JNI_VERSION_21
    vmArgs.nOptions = n;
    vmArgs.options = opts;
    vmArgs.ignoreUnrecognized = JNI_FALSE;  // BanditLauncher: strict, fail on unknown options

    JNIEnv* env = NULL;
    jint rc = createVm(&g_vm, (void**)&env, &vmArgs);
    for (int i = 0; i < n; ++i) free(opts[i].optionString);
    free(opts);
    if (rc != JNI_OK) {
        LogLine(logFile, "xb_jvm: JNI_CreateJavaVM failed rc=%d", (int)rc);
        free(logFile);
        return (int)rc;
    }

    InterlockedExchange(&g_running, 1);
    LogLine(logFile, "xb_jvm: JVM created, invoking %ls.main()", mainClass);

    char* clsUtf8 = WToUtf8(mainClass);
    for (char* p = clsUtf8; p && *p; ++p) { if (*p == '.') *p = '/'; }

    jclass cls = (*env)->FindClass(env, clsUtf8);
    if (!cls || (*env)->ExceptionCheck(env)) {
        (*env)->ExceptionDescribe(env);
        (*env)->ExceptionClear(env);
        LogLine(logFile, "xb_jvm: FindClass(%s) failed", clsUtf8);
        free(clsUtf8); free(logFile);
        InterlockedExchange(&g_running, 0);
        return -102;
    }

    jmethodID main = (*env)->GetStaticMethodID(env, cls, "main", "([Ljava/lang/String;)V");
    if (!main || (*env)->ExceptionCheck(env)) {
        (*env)->ExceptionClear(env);
        LogLine(logFile, "xb_jvm: GetStaticMethodID(main) failed");
        free(clsUtf8); free(logFile);
        InterlockedExchange(&g_running, 0);
        return -103;
    }

    jclass stringCls = (*env)->FindClass(env, "java/lang/String");
    jobjectArray argv = (*env)->NewObjectArray(env, gameArgCount, stringCls, NULL);
    for (int i = 0; i < gameArgCount; ++i) {
        char* a8 = WToUtf8(gameArgs[i]);
        jstring js = (*env)->NewStringUTF(env, a8 ? a8 : "");
        (*env)->SetObjectArrayElement(env, argv, i, js);
        (*env)->DeleteLocalRef(env, (jobject)js);
        free(a8);
    }

    LogLine(logFile, "xb_jvm: calling main with %d args", gameArgCount);
    (*env)->CallStaticVoidMethod(env, cls, main, argv);
    if ((*env)->ExceptionCheck(env)) {
        (*env)->ExceptionDescribe(env);
        (*env)->ExceptionClear(env);
        LogLine(logFile, "xb_jvm: main() threw an exception");
    }

    InterlockedExchange(&g_running, 0);
    LogLine(logFile, "xb_jvm: main() returned");
    free(clsUtf8);
    free(logFile);
    return 0;
}

BOOL WINAPI DllMain(HINSTANCE h, DWORD reason, LPVOID reserved)
{
    (void)h; (void)reason; (void)reserved;
    return TRUE;
}
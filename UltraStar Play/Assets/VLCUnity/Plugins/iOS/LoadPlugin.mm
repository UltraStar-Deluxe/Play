#import "UnityAppController.h"
#include "Unity/IUnityGraphics.h"

typedef void (*UnityPluginLoadFunc)(IUnityInterfaces* unityInterfaces);
typedef void (*UnityPluginUnloadFunc)();
extern "C" void UnityRegisterRenderingPluginV5(UnityPluginLoadFunc loadPlugin,
                                               UnityPluginUnloadFunc unloadPlugin);

extern "C" void VLCUnity_UnityPluginLoad(IUnityInterfaces* unityInterfaces);
extern "C" void VLCUnity_UnityPluginUnload();

#define DEBUG(fmt, ...) debugmsg( "[VLC-Unity] " fmt, ## __VA_ARGS__ )
static void debugmsg( const char* fmt, ...);
#if defined(UNITY_WIN)
void windows_print(const char* fmt, va_list args);
#endif

@interface MyAppController : UnityAppController
{
}
- (void)shouldAttachRenderDelegate;
@end
@implementation MyAppController
- (void)shouldAttachRenderDelegate
{
    // unlike desktops where plugin dynamic library is automatically loaded and registered
    // we need to do that manually on iOS
    UnityRegisterRenderingPluginV5(VLCUnity_UnityPluginLoad, VLCUnity_UnityPluginUnload);
}

@end
IMPL_APP_CONTROLLER_SUBCLASS(MyAppController);

extern "C"
{
#include <stdarg.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
}

void debugmsg( const char* fmt, ...)
{
    va_list args;
    va_start(args, fmt);
    vfprintf(stderr, fmt, args);
    vfprintf(stderr, "\n", args);
    va_end(args);
}

// Trackpad pinch for the Mac player. Unity's Input System never turns a magnify gesture into touches,
// so this bundle watches the window's own events and hands the magnification to C# (TrackpadPinch.cs).
// Build it with tools/build-pinch-bundle.sh; the compiled PinchZoom.bundle next to this folder is committed.
#import <Cocoa/Cocoa.h>

static id monitor;                       // the NSEvent local monitor, nil while stopped
static float magnification, pointerX, pointerY; // summed since the last read; pointer as a fraction of the window (y up)
static int smartTaps;                    // two-finger double taps since the last read

static void NotePointer(NSEvent *e)
{
    NSWindow *window = e.window;
    if (window == nil) return;
    NSRect content = [window contentRectForFrameRect:window.frame];
    if (content.size.width <= 0 || content.size.height <= 0) return;
    NSPoint at = e.locationInWindow; // window points from the bottom left, the same way up as Unity's screen
    pointerX = (float)(at.x / content.size.width);
    pointerY = (float)(at.y / content.size.height);
}

void VuonTra_PinchStart(void)
{
    if (monitor != nil) return;
    monitor = [NSEvent addLocalMonitorForEventsMatchingMask:(NSEventMaskMagnify | NSEventMaskSmartMagnify)
                                                    handler:^NSEvent *(NSEvent *e) {
        if (e.type == NSEventTypeMagnify) magnification += (float)e.magnification;
        else smartTaps++;
        NotePointer(e);
        return e;
    }];
}

// Returns 1 when a gesture happened since the last call and clears what it hands out.
// x and y are the pointer as a fraction of the window's content area (0..1, y up).
int VuonTra_PinchRead(float *outMagnification, float *outX, float *outY, int *outSmartTaps)
{
    int any = magnification != 0.0f || smartTaps != 0;
    *outMagnification = magnification;
    *outX = pointerX;
    *outY = pointerY;
    *outSmartTaps = smartTaps;
    magnification = 0.0f;
    smartTaps = 0;
    return any;
}

void VuonTra_PinchStop(void)
{
    if (monitor == nil) return;
    [NSEvent removeMonitor:monitor];
    monitor = nil;
}

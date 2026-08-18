//-----------------------------------------------------------------------------
// Copyright (c) 2013 GarageGames, LLC
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

#import "platformOSX/AppDelegate.h"

int main(int argc, char *argv[])
{
    @autoreleasepool
    {
        // The legacy Xcode build ships a .app bundle whose Info.plist names a
        // MainMenu nib (NSMainNibFile); NSApplicationMain loads it, and the nib
        // installs the AppDelegate and the menu bar. The CMake build is a bare
        // executable (no bundle/nib), so NSApplicationMain would leave NSApp with
        // no delegate — applicationDidFinishLaunching: never fires, runTorque2D is
        // never called, and the app sits in an empty run loop with no window.
        //
        // If we were launched from such a bundle, keep the original path.
        if ([[[NSBundle mainBundle] infoDictionary] objectForKey:@"NSMainNibFile"])
            return NSApplicationMain(argc, (const char **)argv);

        // Otherwise bootstrap AppKit by hand: become a regular (foreground) GUI
        // app so a window can appear and we get a Dock icon, install the delegate
        // that starts Torque, and run. The engine creates its own NSWindow in
        // runTorque2D, so no nib is required.
        NSApplication *application = [NSApplication sharedApplication];
        [application setActivationPolicy:NSApplicationActivationPolicyRegular];
        [application setDelegate:[[AppDelegate alloc] init]];
        [application activateIgnoringOtherApps:YES];
        [application run];
    }

    return 0;
}

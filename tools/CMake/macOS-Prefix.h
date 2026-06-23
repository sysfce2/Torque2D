//
// macOS prefix header (force-included by the CMake build for the Torque2D
// target). Mirrors the legacy Xcode project's Torque2D-Prefix.pch: the
// platformOSX Objective-C++ back-end uses AppKit/Foundation types (NSEvent,
// NSCursor, NSApplicationMain, NSAutoreleasePool, NSTask, NSString, ...) at
// file scope without importing Cocoa locally, relying on the prefix header.
//
// Guarded by __OBJC__ so the cross-platform C/C++ engine sources (which are
// force-included with the same flag) pull in nothing.
//

#ifdef __OBJC__
    #import <Cocoa/Cocoa.h>
#endif

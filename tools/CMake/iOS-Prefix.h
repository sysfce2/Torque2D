//
// iOS prefix header (force-included by the CMake build for the Torque2D target
// when CMAKE_SYSTEM_NAME=iOS). Mirrors the legacy Xcode_iOS project's
// Torque2D-Prefix.pch: the platformiOS Objective-C++ back-end uses UIKit /
// Foundation types at file scope without importing them locally.
//
// Guarded by __OBJC__ so the cross-platform C/C++ engine sources (force-included
// with the same flag) pull in nothing.
//

#ifdef __OBJC__
    #import <UIKit/UIKit.h>
    #import <Foundation/Foundation.h>
#endif

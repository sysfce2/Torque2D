# -----------------------------------------------------------------------------
# engine/lib/CMakeLists.txt
#
# Vendored third-party libraries, each built as a STATIC library target with
# modern, target-scoped include directories so the engine inherits them via
# target_link_libraries(). Source lists mirror the known-good sets historically
# compiled by the per-library VS projects; the standalone tools and test/example
# programs that live in these trees (e.g. ljpeg/extras, cjpeg/djpeg, pngtest,
# vorbis barkmel/tone) are intentionally excluded.
#
# Warnings in third-party C are silenced (/W0) and CRT-secure warnings disabled.
# The MSVC runtime (static /MT) is inherited from CMAKE_MSVC_RUNTIME_LIBRARY set
# by the root CMakeLists before this directory is added.
# -----------------------------------------------------------------------------

function(torque_thirdparty_lib name)
    if(MSVC)
        target_compile_options(${name} PRIVATE /W0)
        target_compile_definitions(${name} PRIVATE _CRT_SECURE_NO_WARNINGS _CRT_SECURE_NO_DEPRECATE)
    endif()
    set_target_properties(${name} PROPERTIES FOLDER "ThirdParty")
endfunction()

# --- zlib (built from source on desktop; Android uses the NDK system libz) -----
if(NOT ANDROID)
add_library(zlib STATIC
    ${CMAKE_CURRENT_SOURCE_DIR}/zlib/adler32.c
    ${CMAKE_CURRENT_SOURCE_DIR}/zlib/compress.c
    ${CMAKE_CURRENT_SOURCE_DIR}/zlib/crc32.c
    ${CMAKE_CURRENT_SOURCE_DIR}/zlib/deflate.c
    ${CMAKE_CURRENT_SOURCE_DIR}/zlib/gzclose.c
    ${CMAKE_CURRENT_SOURCE_DIR}/zlib/gzlib.c
    ${CMAKE_CURRENT_SOURCE_DIR}/zlib/gzread.c
    ${CMAKE_CURRENT_SOURCE_DIR}/zlib/gzwrite.c
    ${CMAKE_CURRENT_SOURCE_DIR}/zlib/infback.c
    ${CMAKE_CURRENT_SOURCE_DIR}/zlib/inffast.c
    ${CMAKE_CURRENT_SOURCE_DIR}/zlib/inflate.c
    ${CMAKE_CURRENT_SOURCE_DIR}/zlib/inftrees.c
    ${CMAKE_CURRENT_SOURCE_DIR}/zlib/trees.c
    ${CMAKE_CURRENT_SOURCE_DIR}/zlib/uncompr.c
    ${CMAKE_CURRENT_SOURCE_DIR}/zlib/zutil.c
)
target_include_directories(zlib PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/zlib)
torque_thirdparty_lib(zlib)
endif()

# --- lpng (libpng) ------------------------------------------------------------
add_library(lpng STATIC
    ${CMAKE_CURRENT_SOURCE_DIR}/lpng/png.c
    ${CMAKE_CURRENT_SOURCE_DIR}/lpng/pngerror.c
    ${CMAKE_CURRENT_SOURCE_DIR}/lpng/pngget.c
    ${CMAKE_CURRENT_SOURCE_DIR}/lpng/pngmem.c
    ${CMAKE_CURRENT_SOURCE_DIR}/lpng/pngpread.c
    ${CMAKE_CURRENT_SOURCE_DIR}/lpng/pngread.c
    ${CMAKE_CURRENT_SOURCE_DIR}/lpng/pngrio.c
    ${CMAKE_CURRENT_SOURCE_DIR}/lpng/pngrtran.c
    ${CMAKE_CURRENT_SOURCE_DIR}/lpng/pngrutil.c
    ${CMAKE_CURRENT_SOURCE_DIR}/lpng/pngset.c
    ${CMAKE_CURRENT_SOURCE_DIR}/lpng/pngtrans.c
    ${CMAKE_CURRENT_SOURCE_DIR}/lpng/pngwio.c
    ${CMAKE_CURRENT_SOURCE_DIR}/lpng/pngwrite.c
    ${CMAKE_CURRENT_SOURCE_DIR}/lpng/pngwtran.c
    ${CMAKE_CURRENT_SOURCE_DIR}/lpng/pngwutil.c
)
# png.h includes zlib.h, so zlib's headers are part of lpng's public interface.
target_include_directories(lpng PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/lpng)
if(ANDROID)
    target_link_libraries(lpng PUBLIC z)   # NDK system zlib (headers in the sysroot)
else()
    target_link_libraries(lpng PUBLIC zlib)
endif()
torque_thirdparty_lib(lpng)

# --- ljpeg (libjpeg) ----------------------------------------------------------
add_library(ljpeg STATIC
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jcapimin.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jcapistd.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jccoefct.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jccolor.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jcdctmgr.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jchuff.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jcinit.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jcmainct.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jcmarker.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jcmaster.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jcomapi.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jcparam.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jcphuff.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jcprepct.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jcsample.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jctrans.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jdapimin.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jdapistd.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jdatadst.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jdatasrc.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jdcoefct.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jdcolor.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jddctmgr.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jdhuff.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jdinput.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jdmainct.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jdmarker.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jdmaster.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jdmerge.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jdphuff.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jdpostct.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jdsample.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jdtrans.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jerror.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jfdctflt.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jfdctfst.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jfdctint.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jidctflt.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jidctfst.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jidctint.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jidctred.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jmemmgr.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jmemnobs.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jquant1.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jquant2.c
    ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg/jutils.c
)
target_include_directories(ljpeg PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/ljpeg)
torque_thirdparty_lib(ljpeg)

# --- libogg -------------------------------------------------------------------
# config_types.h is already populated on disk; no configure step required.
add_library(libogg STATIC
    ${CMAKE_CURRENT_SOURCE_DIR}/libogg/src/bitwise.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libogg/src/framing.c
)
target_include_directories(libogg PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/libogg/include)
torque_thirdparty_lib(libogg)

# --- libvorbis ----------------------------------------------------------------
add_library(libvorbis STATIC
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/analysis.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/bitrate.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/block.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/codebook.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/envelope.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/floor0.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/floor1.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/info.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/lookup.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/lpc.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/lsp.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/mapping0.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/mdct.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/psy.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/registry.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/res0.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/sharedbook.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/smallft.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/synthesis.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/vorbisenc.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/vorbisfile.c
    ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/window.c
)
target_include_directories(libvorbis
    PUBLIC  ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/include
    # Root holds the internal headers (codec_internal.h, window.h, ...); lib/ holds
    # the mode/codebook trees referenced as "modes/..." and "books/..." (matches VS).
    PRIVATE ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis
            ${CMAKE_CURRENT_SOURCE_DIR}/libvorbis/lib)
target_link_libraries(libvorbis PUBLIC libogg)
torque_thirdparty_lib(libvorbis)

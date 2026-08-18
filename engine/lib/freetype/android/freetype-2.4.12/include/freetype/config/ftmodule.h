/*
 *  This file registers the FreeType modules compiled into the library.
 *
 *  If you use GNU make, this file IS NOT USED!  Instead, it is created in
 *  the objects directory (normally `<topdir>/objs/') based on information
 *  from `<topdir>/modules.cfg'.
 *
 *  Please read `docs/INSTALL.ANY' and `docs/CUSTOMIZE' how to compile
 *  FreeType without GNU make.
 *
 */

/* Torque2D: trimmed to the modules actually compiled by the from-source build in
 * engine/lib/CMakeLists.txt (the EMSCRIPTEN `freetype` target — the only consumer of
 * this source tree; Android links the prebuilt libfreetype.a). ftinit.c registers
 * exactly the drivers listed here, so leaving in modules we don't compile (type1,
 * cff, cid, pfr, type42, winfnt, pcf, bdf) produced undefined-symbol link errors.
 * This is the TrueType + anti-aliased-raster path needed to rasterize .ttf glyphs. */
FT_USE_MODULE( FT_Module_Class, autofit_module_class )
FT_USE_MODULE( FT_Driver_ClassRec, tt_driver_class )
FT_USE_MODULE( FT_Module_Class, psaux_module_class )
FT_USE_MODULE( FT_Module_Class, psnames_module_class )
FT_USE_MODULE( FT_Module_Class, pshinter_module_class )
FT_USE_MODULE( FT_Renderer_Class, ft_raster1_renderer_class )
FT_USE_MODULE( FT_Module_Class, sfnt_module_class )
FT_USE_MODULE( FT_Renderer_Class, ft_smooth_renderer_class )
FT_USE_MODULE( FT_Renderer_Class, ft_smooth_lcd_renderer_class )
FT_USE_MODULE( FT_Renderer_Class, ft_smooth_lcdv_renderer_class )

/* EOF */

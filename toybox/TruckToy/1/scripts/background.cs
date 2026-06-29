//-----------------------------------------------------------------------------
// TruckToy parallax background.
//
// A small, data-driven parallax system. Each layer is a Scroller plus a parallax
// "factor": 0 = pinned to the screen (a far backdrop), 1 = moves with the world
// (foreground speed), values in between scroll proportionally.
//
// Every render frame each layer is cover-fit to the camera's visible size
// (getCameraArea extent) and centred on the post-view-limit render position
// (getCameraRenderPosition). The texture is then scrolled by centerX * factor,
// so the parallax is camera-driven: it stops when the truck stops. We update per
// render frame (RenderCallback) rather than per physics tick so the layers track
// the interpolated camera smoothly instead of "ticking" between ticks.
//
// To add a layer: drop in art that tiles seamlessly left<->right (a factor-0
// layer needn't tile) and add one createParallaxLayer() call below, far -> near.
//-----------------------------------------------------------------------------

function TruckToy::createBackground( %this )
{
    // Rebuild the registries from scratch (reset() clears the scene, so the old
    // Scroller objects / decorations are already gone).
    %this.ParallaxCount = 0;
    %this.ParallaxObjCount = 0;

    // Layers, ordered far -> near. %aspect must match the art's width:height so
    // the fit-to-height sizing doesn't distort it (1024x512 -> 2.0, 1536x512 -> 3.0).
    // Nearer layers use a higher factor and a lower scene layer (drawn in front).
    //                          image                     factor  aspect  yOffFrac  repeatX  sceneLayer
    %this.createParallaxLayer( "TruckToy:background_0",   0.05,    2.0,   0.0,      1,    TruckToy.BackdropDomain );
    %this.createParallaxLayer( "TruckToy:background_1",   0.12,    2.0,   0.0,      1,    TruckToy.BackdropDomain - 1 );

    // Ground band sitting on/above the floor, looping seamlessly in X. It's drawn
    // BEHIND the floor, so anything below FloorLevel is hidden; bottomY slides it
    // up/down, worldHeight sets its thickness. (Image 1232x130 -> aspect 9.48.)
    //                       image                   factor  worldHeight  bottomY                  aspect  sceneLayer
    %this.createGroundLayer( "TruckToy:background_2",  0.30,    3.25,     TruckToy.FloorLevel - 0.5, 9.48,  TruckToy.BackdropDomain - 2 );

    // Drive the layers every render frame via SandboxScene::onSceneRender (below).
    // RenderCallback (a Scene field, not a method) fires per rendered frame, so the
    // layers track the interpolated camera; UpdateCallback (per physics tick) made
    // the sky "tick" back into place between ticks.
    SandboxScene.RenderCallback = true;
    %this.updateParallax();
}

//-----------------------------------------------------------------------------

// %aspect    = image width/height; width is derived from it when fitting the
//              image to the screen height, so a wrong value distorts the art.
// %yOffFrac  = vertical offset as a fraction of view height (0 = centred).
function TruckToy::createParallaxLayer( %this, %image, %factor, %aspect, %yOffFrac, %repeatX, %sceneLayer )
{
    %layer = new Scroller();
    %layer.setBodyType( "static" );
    %layer.setImage( %image );
    %layer.setRepeatX( %repeatX );
    %layer.setSceneLayer( %sceneLayer );
    %layer.setSceneGroup( TruckToy.BackdropDomain );
    %layer.setCollisionSuppress();
    SandboxScene.add( %layer );

    // Register in the parallax table (sizing/positioning happens in updateParallax).
    %i = %this.ParallaxCount;
    %this.ParallaxLayer[%i]   = %layer;
    %this.ParallaxFactor[%i]  = %factor;
    %this.ParallaxAspect[%i]  = %aspect;
    %this.ParallaxYOffset[%i] = %yOffFrac;
    %this.ParallaxMode[%i]    = "screen";
    %this.ParallaxCount = %i + 1;

    return %layer;
}

//-----------------------------------------------------------------------------

// A world-anchored, horizontally-tiling band (e.g. distant terrain near the
// floor). Unlike the screen-fit layers it has an explicit world height and a
// fixed world Y, and loops seamlessly in X -- so the art MUST tile left<->right.
// %worldHeight = band height in world units; %bottomY = world Y of its bottom.
function TruckToy::createGroundLayer( %this, %image, %factor, %worldHeight, %bottomY, %aspect, %sceneLayer )
{
    %layer = new Scroller();
    %layer.setBodyType( "static" );
    %layer.setImage( %image );
    %layer.setSceneLayer( %sceneLayer );
    %layer.setSceneGroup( TruckToy.BackdropDomain );
    %layer.setCollisionSuppress();
    SandboxScene.add( %layer );

    // Register as a ground-mode layer (sizing/positioning happens in updateParallax).
    %i = %this.ParallaxCount;
    %this.ParallaxLayer[%i]   = %layer;
    %this.ParallaxFactor[%i]  = %factor;
    %this.ParallaxAspect[%i]  = %aspect;
    %this.ParallaxMode[%i]    = "ground";
    %this.ParallaxHeight[%i]  = %worldHeight;
    %this.ParallaxBottomY[%i] = %bottomY;
    %this.ParallaxCount = %i + 1;

    return %layer;
}

//-----------------------------------------------------------------------------

// Make an already-placed object parallax. Its current X is captured as the
// anchor; each frame it renders at f*anchorX + (1-f)*cameraX. f = 1 leaves it
// world-fixed (no change), f < 1 drifts slower (reads further back), f > 1 moves
// faster (reads closer). Only X is touched, so it still bobs with the world in Y.
// Call this AFTER the object is positioned (end of a create* function, or right
// after the create* call in reset()).
function TruckToy::registerParallaxObject( %this, %obj, %factor )
{
    %i = %this.ParallaxObjCount;
    %this.ParallaxObj[%i]        = %obj;
    %this.ParallaxObjAnchorX[%i] = getWord( %obj.getPosition(), 0 );
    %this.ParallaxObjFactor[%i]  = %factor;
    %this.ParallaxObjCount = %i + 1;

    return %obj;
}

//-----------------------------------------------------------------------------

function TruckToy::updateParallax( %this )
{
    // View size comes from the camera area's extent (correct regardless of
    // clamping). Screen-fit layers use the height; ground bands use the width.
    %area = SandboxWindow.getCameraArea();
    %viewW = getWord( %area, 2 ) - getWord( %area, 0 );
    %viewH = getWord( %area, 3 ) - getWord( %area, 1 );

    // The CENTRE must come from the render position: getCameraArea reports the
    // area BEFORE the view limit is applied, so it diverges from what's actually
    // drawn near the world edges. getCameraRenderPosition is post-limit (and
    // there is no getCameraRenderArea, hence the split).
    %render  = SandboxWindow.getCameraRenderPosition();
    %centerX = getWord( %render, 0 );
    %centerY = getWord( %render, 1 );

    for ( %i = 0; %i < %this.ParallaxCount; %i++ )
    {
        %layer = %this.ParallaxLayer[%i];
        if ( !isObject( %layer ) )
            continue;

        if ( %this.ParallaxMode[%i] $= "ground" )
        {
            // World-anchored band: explicit world height at a fixed world Y, tiled
            // and looping seamlessly in X. Follows the camera in X (so it always
            // covers the view) but stays put in Y -- so it bobs with the world like
            // the floor, while the texture scroll gives the horizontal parallax.
            %h     = %this.ParallaxHeight[%i];
            %boxW  = %viewW * 1.10;                      // cover the view width
            %tileW = %h * %this.ParallaxAspect[%i];      // one tile at native aspect
            %layer.setSize( %boxW, %h );
            %layer.setRepeatX( %boxW / %tileW );         // tile across the box, no distortion
            %layer.setPosition( %centerX, %this.ParallaxBottomY[%i] + (%h * 0.5) );
            %layer.setScrollPositionX( %centerX * %this.ParallaxFactor[%i] );
        }
        else
        {
            // Screen-fit backdrop: full image height maps top-to-bottom, pinned to
            // the screen vertically; slight overscan (~2% off top & bottom) hides
            // camera shake / the 1-frame render lag. Width follows the image aspect
            // so it's not distorted -- draw the art wider than the screen for scroll
            // room (tile via repeatX if the edges are seamless).
            %h = %viewH * 1.04;
            %w = %h * %this.ParallaxAspect[%i];
            %layer.setSize( %w, %h );
            %layer.setPosition( %centerX, %centerY + ( %this.ParallaxYOffset[%i] * %viewH ) );
            %layer.setScrollPositionX( %centerX * %this.ParallaxFactor[%i] );
        }
    }

    // Parallax the registered decoration objects: render each at
    // f*anchorX + (1-f)*cameraX (the layer model, applied to X only -- Y is left
    // alone so they still bob with the world).
    for ( %j = 0; %j < %this.ParallaxObjCount; %j++ )
    {
        %obj = %this.ParallaxObj[%j];
        if ( !isObject( %obj ) )
            continue;

        %f = %this.ParallaxObjFactor[%j];
        %obj.setPositionX( (%f * %this.ParallaxObjAnchorX[%j]) + ((1 - %f) * %centerX) );
    }
}

//-----------------------------------------------------------------------------

// Per-frame scene hook. The scene fires this when RenderCallback is enabled
// (set in createBackground). Guarded so it's harmless if another toy is loaded.
function SandboxScene::onSceneRender( %this )
{
    if ( isObject( TruckToy ) && TruckToy.ParallaxCount > 0 )
        TruckToy.updateParallax();
}

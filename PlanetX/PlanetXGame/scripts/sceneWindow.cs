//-----------------------------------------------------------------------------
// PlanetXSceneWindow: the camera window onto the level. Keeps the camera height
// fixed and fits its width to the window's aspect ratio, so resizing widens or
// narrows the view instead of stretching it (same pattern as the Sandbox).
//-----------------------------------------------------------------------------

function PlanetXSceneWindow::updateCameraAspect(%this)
{
	%extent = Canvas.extent;
	%aspect = %extent.x / %extent.y;

	%camera = %this.getCameraSize();
	%camera.x = %camera.y * %aspect;
	%this.setCameraSize(%camera);
}

/// Engine callback: fires on every live window resize.
function PlanetXSceneWindow::onExtentChange(%this)
{
	%this.updateCameraAspect();
}

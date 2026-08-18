
function AssetAudioPlayButton::onClick(%this)
{
	if(alxIsPlaying(%this.sound))
	{
		%this.resetSound();
	}
	else
	{
		// alxPlayPreview, not alxPlay: an editor auditions the ASSET, not the
		// asset as the project currently loaded happens to be mixing it. Played
		// through alxPlay, a game that had turned its music channel down to
		// nothing made every music asset here silent -- and not quietly silent,
		// since the engine refuses to create a source on a muted channel at all,
		// so there was no handle and no way to tell that from a broken file.
		%this.sound = alxPlayPreview(%this.assetID);
		%this.setText("Stop");

		if(!%this.asset.Looping)
		{
			%this.soundSchedule = %this.schedule(100, "testSound");
		}
	}
}

function AssetAudioPlayButton::testSound(%this)
{
	if(alxIsPlaying(%this.sound))
	{
		%this.soundSchedule = %this.schedule(100, "testSound");
	}
	else
	{
		%this.setText("Play");
	}
}

function AssetAudioPlayButton::resetSound(%this)
{
	if(alxIsPlaying(%this.sound))
	{
		alxStop(%this.sound);
		%this.setText("Play");
		cancel(%this.soundSchedule);
	}
}

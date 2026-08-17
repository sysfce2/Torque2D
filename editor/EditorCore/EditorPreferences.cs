//-----------------------------------------------------------------------------
// The editor's memory between runs.
//
// Until now the editor had none: every choice a person made -- which theme, how
// a list was arranged -- lasted exactly as long as the process. The things worth
// remembering are the ones a person sets once and expects to stay set, and the
// Asset Library's view mode and sort order are the first two.
//
// Deliberately NOT $pref:: globals. Those are the engine's own settings, they
// are re-declared on every boot by defaultPreferences.cs, and script has no
// setVariable() to write one by name -- only eval(). Dynamic fields on a
// SimObject give the same key/value store with getFieldValue/setFieldValue and
// no string-built code.
//
// The file is written to the platform's per-user application data folder
// (getPrefsPath), never into the repository or a project, because it describes
// the person rather than the work.
//-----------------------------------------------------------------------------

function EditorPreferences::onAdd(%this)
{
	%this.path = getPrefsPath("editorPreferences.taml");
	%this.load();
}

// A value the editor has never been told is not an error -- it is the first run.
function EditorPreferences::get(%this, %key, %fallback)
{
	%value = %this.getFieldValue(%key);

	return (%value $= "") ? %fallback : %value;
}

// Written through immediately. There is no "apply" step anywhere in the editor,
// and a preferences file that only survives a clean exit is one that never
// survives the interesting exits.
function EditorPreferences::set(%this, %key, %value)
{
	if(%this.getFieldValue(%key) $= %value)
	{
		return;
	}

	%this.setFieldValue(%key, %value);
	%this.save();
}

//-----------------------------------------------------------------------------
// The file.
//-----------------------------------------------------------------------------

function EditorPreferences::load(%this)
{
	if(!%this.fileExists(%this.path))
	{
		return;
	}

	%stored = TamlRead(%this.path);
	if(!isObject(%stored))
	{
		warn("EditorPreferences: could not read " @ %this.path);
		return;
	}

	// getDynamicField answers with the field's NAME and nothing else, despite the
	// "myField myValue" its own doc comment implies -- see simObject_ScriptBinding.h,
	// which sprintfs entry->slotName alone. The value has to be asked for separately.
	%count = %stored.getDynamicFieldCount();
	for(%i = 0; %i < %count; %i++)
	{
		%name = %stored.getDynamicField(%i);
		%this.setFieldValue(%name, %stored.getFieldValue(%name));
	}

	%stored.delete();
}

// Saved as a plain ScriptObject rather than as this object, because "class" is a
// persistent field: writing this one out would record class="EditorPreferences",
// and reading it back would construct a second one, whose onAdd would load the
// file again.
function EditorPreferences::save(%this)
{
	%store = new ScriptObject();

	%count = %this.getDynamicFieldCount();
	for(%i = 0; %i < %count; %i++)
	{
		%name = %this.getDynamicField(%i);

		// path is this object's own bookkeeping, not something to remember.
		if(%name $= "path")
		{
			continue;
		}

		%store.setFieldValue(%name, %this.getFieldValue(%name));
	}

	// The per-user folder does not exist until something makes it, and TamlWrite
	// fails by logging rather than by telling the caller.
	createPath(%this.path);

	TamlWrite(%store, %this.path);
	%store.delete();
}

// isFile() answers from the ResourceManager's dictionary rather than from the
// disk, so it can say yes to a path nothing ever wrote and no to one written
// this session. Opening the file is the only answer that is about the file.
function EditorPreferences::fileExists(%this, %path)
{
	%file = new FileObject();
	%found = %file.openForRead(%path);
	%file.close();
	%file.delete();

	return %found;
}

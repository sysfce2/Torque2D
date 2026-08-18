//-----------------------------------------------------------------------------
// Renaming a module is not renaming its ModuleId.
//
// The engine calls <ModuleId>::<CreateFunction>, so the id in module.taml and
// the namespace in the module's script are the same name written twice. Asset
// ids are that name a third time: "<ModuleId>:<assetName>", in script and in
// every taml file that references an asset. Change only the one in module.taml
// -- which is what all three of the editor's rename paths used to do -- and the
// module still loads, still reports the new name, and silently does nothing:
// create never fires, and its assets resolve to a module that no longer exists.
//
// So a rename is a pass over the module's own source. Only .cs and .taml files
// are read; art and audio are left alone.
//
// The engine has half of this already: ModuleManager::copyModule runs a
// TamlModuleIdUpdateVisitor when the source and target ids differ. It is not
// enough on its own -- the visitor is root-only, so an asset id on a nested
// element is missed, it cannot touch .cs at all, and it renames module.taml to
// <ModuleId>.module.taml, a name every editor script that opens a module
// definition does not expect. The copy is therefore made under the template's
// own id and the rename happens here, on the copy.
//-----------------------------------------------------------------------------

// The extensions worth reading. Everything else in a module is content.
function ModuleStamper::onAdd(%this)
{
	%this.textExtensions = ".cs" TAB ".taml";
}

// Template modules carry two dynamic fields the engine knows nothing about:
// Template marks a module as something to stamp out rather than install, and
// DisplayName is what to call it in a picker. Neither is a ModuleDefinition
// field, so both ride along as taml attributes the way AppCore's Project and
// ProjectDescription do. The names live here so the dialogs that read them and
// the code that strips them off a copy agree on the spelling.
function ModuleStamper::displayName(%this, %module)
{
	if(%module.DisplayName !$= "")
	{
		return %module.DisplayName;
	}

	return %module.ModuleID;
}

// A stamped copy is a module in its own right, not a template, so the markers
// that made it stampable do not belong on it.
function ModuleStamper::clearTemplateMarkers(%this, %definition)
{
	%definition.Template = "";
	%definition.DisplayName = "";
}

// %modulePath is the module's folder; %oldId and %newId are module ids. Returns
// true if the walk completed, whether or not any file needed changing.
function ModuleStamper::renameInPlace(%this, %modulePath, %oldId, %newId)
{
	if(%oldId $= "" || %newId $= "" || %oldId $= %newId)
	{
		return true;
	}

	if(!isDirectory(%modulePath))
	{
		error("ModuleStamper: no module at " @ %modulePath);
		return false;
	}

	return %this.rewriteTree(%modulePath, %oldId, %newId);
}

// One level at a time rather than getDirectoryList's depth argument: that
// binding passes noBasePath, so the base folder is never in the list and the
// returned names are relative to it. Recursing by hand keeps the full path in
// hand at every level.
function ModuleStamper::rewriteTree(%this, %dir, %old, %new)
{
	%files = getFileList(%dir);
	for(%i = 0; %i < getFieldCount(%files); %i++)
	{
		%file = getField(%files, %i);
		if(%this.isTextFile(%file))
		{
			%this.rewriteFile(pathConcat(%dir, %file), %old, %new);
		}
	}

	%dirs = getDirectoryList(%dir);
	for(%i = 0; %i < getFieldCount(%dirs); %i++)
	{
		%sub = getField(%dirs, %i);
		if(%sub $= "" || %sub $= "." || %sub $= "..")
		{
			continue;
		}

		%this.rewriteTree(pathConcat(%dir, %sub), %old, %new);
	}

	return true;
}

function ModuleStamper::isTextFile(%this, %file)
{
	%ext = fileExt(%file);
	for(%i = 0; %i < getFieldCount(%this.textExtensions); %i++)
	{
		if(%ext $= getField(%this.textExtensions, %i))
		{
			return true;
		}
	}

	return false;
}

// Read the whole file before writing any of it. FileObject reads through the
// ResourceManager, which caches a file's size the first time it is asked for
// one, so a file read back after being written in the same session can be read
// at its old length.
function ModuleStamper::rewriteFile(%this, %path, %old, %new)
{
	%file = new FileObject();
	if(!%file.openForRead(%path))
	{
		%file.delete();
		error("ModuleStamper: could not read " @ %path);
		return false;
	}

	%count = 0;
	%changed = false;
	while(!%file.isEOF())
	{
		%line = %file.readLine();
		%rewritten = %this.replaceToken(%line, %old, %new);
		if(%rewritten !$= %line)
		{
			%changed = true;
		}

		%out[%count] = %rewritten;
		%count++;
	}
	%file.close();

	if(!%changed)
	{
		%file.delete();
		return true;
	}

	if(!%file.openForWrite(%path))
	{
		%file.delete();
		error("ModuleStamper: could not write " @ %path);
		return false;
	}

	for(%i = 0; %i < %count; %i++)
	{
		%file.writeLine(%out[%i]);
	}
	%file.close();
	%file.delete();

	return true;
}

// A whole-word replace. strreplace would do for "BlankGame", but this also runs
// over modules a person named themselves, where the old id can be a substring
// of an ordinary word in a comment or a string. A match counts only where the
// characters on either side cannot be part of an identifier -- which the three
// forms that matter all satisfy: ModuleId="Name", Name::create, "Name:asset".
function ModuleStamper::replaceToken(%this, %line, %old, %new)
{
	%length = strlen(%old);
	if(%length == 0)
	{
		return %line;
	}

	%result = "";
	%from = 0;

	while(true)
	{
		%at = strpos(%line, %old, %from);
		if(%at == -1)
		{
			return %result @ getSubStr(%line, %from, strlen(%line) - %from);
		}

		%before = (%at == 0) ? "" : getSubStr(%line, %at - 1, 1);
		%after = getSubStr(%line, %at + %length, 1);

		%result = %result @ getSubStr(%line, %from, %at - %from);
		if(%this.isIdentifierChar(%before) || %this.isIdentifierChar(%after))
		{
			%result = %result @ %old;
		}
		else
		{
			%result = %result @ %new;
		}

		%from = %at + %length;
	}
}

function ModuleStamper::isIdentifierChar(%this, %char)
{
	if(%char $= "")
	{
		return false;
	}

	if(%char $= "_")
	{
		return true;
	}

	// $= is case insensitive, so the lower case half of the alphabet answers for
	// both.
	return strpos("abcdefghijklmnopqrstuvwxyz0123456789", strlwr(%char)) != -1;
}

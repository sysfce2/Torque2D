
//-----------------------------------------------------------------------------
// The data side of the Gui Profile Editor. Owned by GuiEditor and persistent
// across dialog sessions, so theme member profiles stay alive for the guis
// that reference them (and appear in the Gui Editor's profile dropdowns).
// Loads themes and standalone profiles from the project's themes folder,
// maintains the proxy hierarchy the dialog tree displays, tracks dirty
// roots, saves them to their files, and reverts them by re-reading those
// files.
//-----------------------------------------------------------------------------

function GuiProfileEditorLibrary::onAdd(%this)
{
	%this.themeGroup = new SimGroup();

	%this.proxyRoot = new SimGroup()
	{
		kind = "root";
		treeLabel = "Gui Themes";
	};

	%this.standaloneFolder = new SimGroup()
	{
		kind = "folder";
		treeLabel = "Stand Alone";
	};
	%this.proxyRoot.add(%this.standaloneFolder);

	%this.dirtySet = new SimSet();
	%this.doomedFileCount = 0;
}

function GuiProfileEditorLibrary::onRemove(%this)
{
	if(isObject(%this.dirtySet))
	{
		%this.dirtySet.delete();
	}
	if(isObject(%this.proxyRoot))
	{
		%this.proxyRoot.delete();
	}
	if(isObject(%this.themeGroup))
	{
		%this.themeGroup.delete();
	}
}

function GuiProfileEditorLibrary::getThemesPath(%this)
{
	// An explicitly set project folder always wins.
	if(ProjectManager.projectFolder !$= "")
	{
		%projectPath = pathConcat(getMainDotCsDir(), ProjectManager.getProjectFolder());
		return pathConcat(%projectPath, "themes");
	}

	// Otherwise anchor on the loaded AppCore module, which sits at
	// <project>/AppCore/<version>. This holds whether getModulePath returns
	// a relative or an absolute path; ProjectManager's derivation caches a
	// bogus value when the module path is relative or the module database
	// state shifts, which sent themes to the wrong folder.
	%appCore = ModuleDatabase.findModule("AppCore", 1);
	if(isObject(%appCore))
	{
		%projectPath = filePath(filePath(%appCore.getModulePath()));
		return pathConcat(getMainDotCsDir(), %projectPath, "themes");
	}

	%projectPath = pathConcat(getMainDotCsDir(), ProjectManager.getProjectFolder());
	return pathConcat(%projectPath, "themes");
}

// Load any theme files not already loaded. Safe to call on every dialog
// open: files belonging to live objects are skipped.
function GuiProfileEditorLibrary::scanThemes(%this)
{
	%path = %this.getThemesPath();
	createPath(%path @ "/");

	%pattern = %path @ "/*.taml";
	for(%file = findFirstFile(%pattern); %file !$= ""; %file = findNextFile(%pattern))
	{
		if(%this.loadedFile[%file])
		{
			continue;
		}

		%object = TAMLRead(%file);
		if(!isObject(%object))
		{
			warn("GuiProfileEditorLibrary::scanThemes: could not read " @ %file);
			continue;
		}

		%class = %object.getClassName();
		if(%class $= "GuiProfileTheme")
		{
			if(%object.getName() $= "")
			{
				warn("GuiProfileEditorLibrary::scanThemes: skipping " @ %file @ " - the theme has no name (possibly a name collision).");
				%object.delete();
				continue;
			}
			%this.themeGroup.add(%object);
			%this.sourceFile[%object.getId()] = %file;
			%this.loadedFile[%file] = true;
			%this.addThemeProxies(%object);
		}
		else if(%class $= "GuiControlProfile")
		{
			%this.themeGroup.add(%object);
			%this.sourceFile[%object.getId()] = %file;
			%this.loadedFile[%file] = true;
			%this.addStandaloneProxy(%object);
		}
		else
		{
			warn("GuiProfileEditorLibrary::scanThemes: skipping " @ %file @ " - not a theme or profile.");
			%object.delete();
		}
	}
}

//-----------------------------------------------------------------------------
// Proxy tree.
//-----------------------------------------------------------------------------

function GuiProfileEditorLibrary::addThemeProxies(%this, %theme)
{
	%proxy = new SimGroup()
	{
		kind = "theme";
		target = %theme;
		baseLabel = %theme.getName();
		treeLabel = %theme.getName();
	};
	%this.themeProxy[%theme.getId()] = %proxy;

	%profileFolder = new SimGroup()
	{
		kind = "folder";
		treeLabel = "Profiles";
	};
	%proxy.add(%profileFolder);

	%categoryNames = %theme.getCategoryNames();
	for(%i = 0; %i < getWordCount(%categoryNames); %i++)
	{
		%category = getWord(%categoryNames, %i);
		%categoryProxy = new SimGroup()
		{
			kind = "category";
			theme = %theme;
			category = %category;
			treeLabel = %category;
		};
		%this.categoryProxy[%theme.getId() @ "_" @ %category] = %categoryProxy;
		%profileFolder.add(%categoryProxy);

		// A loaded theme file can carry extra profiles in this category.
		%profiles = %theme.getProfiles(%category);
		for(%p = 1; %p < getWordCount(%profiles); %p++)
		{
			%this.addExtraProxy(%theme, %category, getWord(%profiles, %p));
		}
	}

	%borderFolder = new SimGroup()
	{
		kind = "folder";
		treeLabel = "Borders";
	};
	%proxy.add(%borderFolder);

	%borderNames = %theme.getBorderCategoryNames();
	for(%i = 0; %i < getWordCount(%borderNames); %i++)
	{
		%category = getWord(%borderNames, %i);
		%borderProxy = new ScriptObject()
		{
			kind = "border";
			theme = %theme;
			category = %category;
			treeLabel = %category;
		};
		%borderFolder.add(%borderProxy);
	}

	%this.proxyRoot.add(%proxy);

	// The Stand Alone folder always stays at the bottom of the tree.
	%this.proxyRoot.pushToBack(%this.standaloneFolder);
}

function GuiProfileEditorLibrary::addExtraProxy(%this, %theme, %category, %profile)
{
	%categoryProxy = %this.categoryProxy[%theme.getId() @ "_" @ %category];
	if(!isObject(%categoryProxy))
	{
		return;
	}

	%label = %profile.getName();
	if(%label $= "")
	{
		%label = "(unnamed)";
	}

	%leaf = new ScriptObject()
	{
		kind = "extra";
		theme = %theme;
		target = %profile;
		category = %category;
		treeLabel = %label;
	};
	%this.extraProxy[%profile.getId()] = %leaf;
	%categoryProxy.add(%leaf);
}

function GuiProfileEditorLibrary::addStandaloneProxy(%this, %profile)
{
	%label = %profile.getName();
	if(%label $= "")
	{
		%label = "(unnamed)";
	}

	%leaf = new ScriptObject()
	{
		kind = "standalone";
		target = %profile;
		baseLabel = %label;
		treeLabel = %label;
	};
	%this.standaloneProxy[%profile.getId()] = %leaf;
	%this.standaloneFolder.add(%leaf);
}

function GuiProfileEditorLibrary::getRootProxy(%this, %root)
{
	%proxy = %this.themeProxy[%root.getId()];
	if(!isObject(%proxy))
	{
		%proxy = %this.standaloneProxy[%root.getId()];
	}
	return %proxy;
}

function GuiProfileEditorLibrary::removeProxiesFor(%this, %root)
{
	%proxy = %this.getRootProxy(%root);
	if(isObject(%proxy))
	{
		%proxy.delete();
	}
	%this.themeProxy[%root.getId()] = "";
	%this.standaloneProxy[%root.getId()] = "";
}

//-----------------------------------------------------------------------------
// Dirty tracking.
//-----------------------------------------------------------------------------

function GuiProfileEditorLibrary::markDirty(%this, %root)
{
	if(!isObject(%root))
	{
		return;
	}

	if(!%this.dirtySet.isMember(%root))
	{
		%this.dirtySet.add(%root);
	}

	%proxy = %this.getRootProxy(%root);
	if(isObject(%proxy) && !%proxy.isDirtyMarked)
	{
		%proxy.isDirtyMarked = true;
		%proxy.treeLabel = %proxy.baseLabel @ " *";
		if(isObject(%this.dialog))
		{
			%this.dialog.tree.refresh();
		}
	}
}

function GuiProfileEditorLibrary::unmarkDirty(%this, %root)
{
	%proxy = %this.getRootProxy(%root);
	if(isObject(%proxy) && %proxy.isDirtyMarked)
	{
		%proxy.isDirtyMarked = false;
		%proxy.treeLabel = %proxy.baseLabel;
	}
}

function GuiProfileEditorLibrary::isDirty(%this)
{
	return (%this.dirtySet.getCount() > 0 || %this.doomedFileCount > 0);
}

//-----------------------------------------------------------------------------
// Save and revert.
//-----------------------------------------------------------------------------

function GuiProfileEditorLibrary::saveAll(%this)
{
	%path = %this.getThemesPath();
	createPath(%path @ "/");

	while(%this.dirtySet.getCount() > 0)
	{
		%root = %this.dirtySet.getObject(0);
		%this.dirtySet.remove(%root);

		%file = %this.sourceFile[%root.getId()];
		if(%file $= "")
		{
			if(%root.getName() $= "")
			{
				warn("GuiProfileEditorLibrary::saveAll: cannot save an unnamed theme or profile - name it first.");
				continue;
			}
			%file = pathConcat(%path, %root.getName() @ ".taml");
		}

		TAMLWrite(%root, %file);
		%this.sourceFile[%root.getId()] = %file;
		%this.loadedFile[%file] = true;
		%this.unmarkDirty(%root);
	}

	for(%i = 0; %i < %this.doomedFileCount; %i++)
	{
		if(isFile(%this.doomedFile[%i]))
		{
			fileDelete(%this.doomedFile[%i]);
		}
	}
	%this.doomedFileCount = 0;
}

// Discard the session's edits: every dirty root is deleted and, when it has
// a source file, re-read from it. Doomed files are simply forgotten (they
// were never deleted from disk), so a deleted theme returns at the next
// scan.
function GuiProfileEditorLibrary::revertAll(%this)
{
	while(%this.dirtySet.getCount() > 0)
	{
		%root = %this.dirtySet.getObject(0);
		%this.dirtySet.remove(%root);

		%file = %this.sourceFile[%root.getId()];
		%this.removeProxiesFor(%root);
		%root.delete();

		if(%file !$= "" && isFile(%file))
		{
			%object = TAMLRead(%file);
			if(!isObject(%object))
			{
				warn("GuiProfileEditorLibrary::revertAll: could not re-read " @ %file);
				%this.loadedFile[%file] = "";
				continue;
			}
			%this.themeGroup.add(%object);
			%this.sourceFile[%object.getId()] = %file;
			%this.loadedFile[%file] = true;
			if(%object.getClassName() $= "GuiProfileTheme")
			{
				%this.addThemeProxies(%object);
			}
			else
			{
				%this.addStandaloneProxy(%object);
			}
		}
		else if(%file !$= "")
		{
			%this.loadedFile[%file] = "";
		}
	}

	%this.doomedFileCount = 0;
}

//-----------------------------------------------------------------------------
// Operations.
//-----------------------------------------------------------------------------

function GuiProfileEditorLibrary::doomSourceFile(%this, %root)
{
	%file = %this.sourceFile[%root.getId()];
	if(%file !$= "")
	{
		%this.doomedFile[%this.doomedFileCount] = %file;
		%this.doomedFileCount++;
		%this.sourceFile[%root.getId()] = "";
		%this.loadedFile[%file] = "";
	}
}

function GuiProfileEditorLibrary::createTheme(%this, %name)
{
	if(%name $= "" || isObject(%name))
	{
		warn("GuiProfileEditorLibrary::createTheme: the name '" @ %name @ "' is empty or already taken.");
		return 0;
	}

	%theme = new GuiProfileTheme();
	%this.themeGroup.add(%theme);
	if(!%theme.renameTheme(%name))
	{
		%theme.delete();
		return 0;
	}

	// Friendlier starting point than the C++ ctor defaults: no borders until
	// the author opts in, a readable 16px base font, and a real font directory
	// (the engine's font cache dir, where AppCore ships its fonts) so the form's
	// font dropdowns aren't empty. Each assignment restamps, fine for a fresh
	// theme.
	%theme.borderSize = 0;
	%theme.fontSize = 16;
	// Store the font directory relative to the game root (not the absolute
	// expanded path) so themes stay portable.
	%theme.fontDirectory = makeRelativePath($GUI::fontCacheDirectory, getMainDotCsDir());

	%this.sourceFile[%theme.getId()] = "";
	%this.addThemeProxies(%theme);
	%this.markDirty(%theme);
	return %theme;
}

function GuiProfileEditorLibrary::deleteTheme(%this, %theme)
{
	// The file is only removed on save; cancel keeps it (and the next scan
	// reloads it).
	%this.doomSourceFile(%theme);

	%this.dirtySet.removeIfMember(%theme);
	%this.removeProxiesFor(%theme);
	%theme.delete();
}

function GuiProfileEditorLibrary::renameThemeTo(%this, %theme, %name)
{
	if(!%theme.renameTheme(%name))
	{
		return false;
	}

	// The old file no longer matches the theme; replace it on save.
	%this.doomSourceFile(%theme);

	%proxy = %this.themeProxy[%theme.getId()];
	if(isObject(%proxy))
	{
		%proxy.baseLabel = %name;
		%proxy.isDirtyMarked = false;
	}
	%this.markDirty(%theme);

	// Member names changed too, so refresh every extra label in this theme.
	%categoryNames = %theme.getCategoryNames();
	for(%i = 0; %i < getWordCount(%categoryNames); %i++)
	{
		%profiles = %theme.getProfiles(getWord(%categoryNames, %i));
		for(%p = 1; %p < getWordCount(%profiles); %p++)
		{
			%profile = getWord(%profiles, %p);
			%leaf = %this.extraProxy[%profile.getId()];
			if(isObject(%leaf))
			{
				%leaf.treeLabel = %profile.getName();
			}
		}
	}

	return true;
}

function GuiProfileEditorLibrary::createExtraProfile(%this, %theme, %category)
{
	%profile = %theme.createProfile(%category);
	if(!isObject(%profile))
	{
		return 0;
	}

	%this.addExtraProxy(%theme, %category, %profile);
	%this.markDirty(%theme);
	return %profile;
}

function GuiProfileEditorLibrary::removeExtraProfile(%this, %theme, %profile)
{
	%leaf = %this.extraProxy[%profile.getId()];
	%removed = %theme.removeProfile(%profile);
	if(!%removed)
	{
		return false;
	}

	if(isObject(%leaf))
	{
		%leaf.delete();
	}
	%this.extraProxy[%profile.getId()] = "";
	%this.markDirty(%theme);
	return true;
}

function GuiProfileEditorLibrary::createStandalone(%this, %name)
{
	if(%name $= "" || isObject(%name))
	{
		warn("GuiProfileEditorLibrary::createStandalone: the name '" @ %name @ "' is empty or already taken.");
		return 0;
	}

	%profile = new GuiControlProfile(%name);
	%this.themeGroup.add(%profile);
	%this.sourceFile[%profile.getId()] = "";
	%this.addStandaloneProxy(%profile);
	%this.markDirty(%profile);
	return %profile;
}

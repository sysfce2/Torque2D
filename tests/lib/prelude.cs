//-----------------------------------------------------------------------------
// Exec'd ahead of every test by the stub tests/run.ps1 writes at the repo root.
//
// A relative path handed to a console function is expanded against the script
// that is calling, NOT against the working directory. That is invisible while a
// script lives at the repo root -- the two are the same place -- and it quietly
// breaks the moment one does not: exec("./editor/main.cs") looks for
// tests/smoke/editor/main.cs, and createPath("./shots/") makes tests/smoke/shots.
//
// getMainDotCsDir() is the repo root for the whole process, because the engine
// sets it from the boot script's folder and the boot stub sits at the root. So
// these two turn a repo-relative path into one that means the same thing no
// matter which file says it. Use them for every path a test names.
//-----------------------------------------------------------------------------

// "shots/x.png" -> "<repo root>/shots/x.png"
function testRoot(%relativePath)
{
	return pathConcat(getMainDotCsDir(), %relativePath);
}

// exec() a script named relative to the repo root.
function testExec(%relativePath)
{
	exec(testRoot(%relativePath));
}

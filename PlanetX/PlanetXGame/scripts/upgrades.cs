//-----------------------------------------------------------------------------
// PlanetXUpgrades: the weapon-upgrade catalog and the playthrough's upgrade state.
// A session singleton (built in PlanetXGame::create) that knows every upgrade - its
// title, description, card image, whether it is co-op only, and how many times it
// can be taken before the weapon caps out - and remembers how many times each of the
// (up to two) players has taken each one THIS RUN.
//
// The counts are the whole model: a player's weapon and suit are a pure function of
// them (applyToPlayer), recomputed from scratch each level because the player is
// rebuilt every level. reset() wipes the counts for a fresh run; the victory screen
// asks offer() what to show and take() to bank a choice. See TORQUE_SCRIPT.md.
//-----------------------------------------------------------------------------

function PlanetXUpgrades::onAdd(%this)
{
	%this.keys = "";

	// define(key, title, description, coOpOnly, maxCount)  -  maxCount -1 = uncapped.
	// The caps mirror the stat floors/ceilings in applyToPlayer: once a player has
	// taken an upgrade maxCount times its stat is pinned and it stops being offered.
	// Damage and Nanite Weave stay uncapped (always offerable in solo), and Dead
	// Man's Payload is uncapped in co-op, so the offer pool can never run dry.
	%this.define("damage",       "Capacitor Overload",   "Each shot deals +0.5 damage.",                     false, -1);
	%this.define("firerate",     "Cycle Accelerator",    "Fire 15ms faster between shots.",                   false,  8);
	%this.define("lessheat",     "Cryo-Coated Barrel",   "Each shot produces less heat.",                     false,  6);
	%this.define("ventfast",     "Coolant Vents",        "Vent heat faster after firing.",                    false,  6);
	%this.define("maxheat",      "Reinforced Heat Sink", "Store more heat before overheating.",               false,  5);
	%this.define("split",        "Fork Emitter",         "Fire one extra bolt each shot.",                    false,  6);
	%this.define("tighten",      "Focusing Array",       "Tighten the angle between bolts by 1 degree.",      false,  5);
	%this.define("selfdestruct", "Dead Man's Payload",   "When you fall, a blast destroys nearby enemies.",   true,  -1);
	%this.define("autorepair",   "Nanite Weave",         "Slowly repair your suit over time.",                false, -1);
}

/// Register one upgrade in the catalog.
function PlanetXUpgrades::define(%this, %key, %title, %desc, %coOpOnly, %maxCount)
{
	%this.keys = (%this.keys $= "") ? %key : (%this.keys SPC %key);
	%this.title[%key]    = %title;
	%this.desc[%key]     = %desc;
	// Card art follows the convention gui/images/upgrade_<key>.png -> asset id
	// PlanetXGame:upgrade_<key>. Each ships as a copy of the placeholder; drop real
	// art onto the file to replace it (no code change needed).
	%this.image[%key]    = "PlanetXGame:upgrade_" @ %key;
	%this.coOpOnly[%key] = %coOpOnly;
	%this.maxCount[%key] = %maxCount;
}

/// Clear every player's counts - a brand-new run starts with the stock blaster.
function PlanetXUpgrades::reset(%this)
{
	%n = getWordCount(%this.keys);
	for (%i = 0; %i < %n; %i++)
	{
		%key = getWord(%this.keys, %i);
		%this.count[1, %key] = 0;
		%this.count[2, %key] = 0;
	}
}

//-----------------------------------------------------------------------------
// Eligibility and the end-of-level offer.
//-----------------------------------------------------------------------------

/// Can player %pi still take %key? False if it is co-op only in a solo run, if its
/// prerequisite is unmet, or if this player has already hit its cap.
function PlanetXUpgrades::isEligible(%this, %key, %pi)
{
	if (%this.coOpOnly[%key] && !$PlanetX::twoPlayer)
		return false;

	// Focusing the fan only makes sense once this player has at least two bolts, i.e.
	// has taken the split-shot upgrade at least once.
	if (%key $= "tighten" && %this.count[%pi, "split"] < 1)
		return false;

	%cap = %this.maxCount[%key];
	if (%cap >= 0 && %this.count[%pi, %key] >= %cap)
		return false;

	return true;
}

/// Is %key worth offering at all - can either participating player still take it? A
/// card offered on this basis is always claimable by someone; applyToPlayer clamps
/// every stat, so a claim that happens to be a no-op for one player is harmless.
function PlanetXUpgrades::eligibleForAny(%this, %key)
{
	if (%this.isEligible(%key, 1))
		return true;
	if ($PlanetX::twoPlayer && %this.isEligible(%key, 2))
		return true;
	return false;
}

/// Pick %needed distinct upgrade keys (2 solo, 3 co-op) to offer on the victory
/// screen, at random from the still-takeable pool. Returns a space-separated key
/// list; if the pool is somehow smaller than %needed the caller shows fewer cards.
function PlanetXUpgrades::offer(%this, %needed)
{
	%pool = "";
	%n = getWordCount(%this.keys);
	for (%i = 0; %i < %n; %i++)
	{
		%key = getWord(%this.keys, %i);
		if (%this.eligibleForAny(%key))
			%pool = (%pool $= "") ? %key : (%pool SPC %key);
	}

	// Draw without replacement: pull a random word out of the pool %needed times.
	%result = "";
	for (%k = 0; %k < %needed && getWordCount(%pool) > 0; %k++)
	{
		%j = getRandom(0, getWordCount(%pool) - 1);
		%key = getWord(%pool, %j);
		%result = (%result $= "") ? %key : (%result SPC %key);
		%pool = removeWord(%pool, %j);
	}

	return %result;
}

//-----------------------------------------------------------------------------
// Banking a choice and applying the whole set to a player.
//-----------------------------------------------------------------------------

/// Record that player %pi chose %key (persists for the rest of the run).
function PlanetXUpgrades::take(%this, %key, %pi)
{
	%this.count[%pi, %key] = %this.count[%pi, %key] + 1;
}

/// Re-derive %player's weapon and suit stats from its stored counts. Called from the
/// player's onAdd every level, right after it builds its weapon. Each upgrade is a
/// fixed step per pick, clamped to its floor/ceiling; the bullet pool is (re)built
/// last, now that the final fire rate and bolt count are known.
function PlanetXUpgrades::applyToPlayer(%this, %player)
{
	%pi = %player.playerIndex;
	%w  = %player.weapon;
	if (!isObject(%w))
		return;

	// Weapon stats (deltas from the tuned blaster base already on the weapon).
	%w.bulletDamage       = %w.bulletDamage + 0.5 * %this.count[%pi, "damage"];
	%w.fireCooldown       = mClamp(%w.fireCooldown       - 15    * %this.count[%pi, "firerate"], 80,               %w.fireCooldown);
	%w.heatPerShot        = mClamp(%w.heatPerShot        - 0.015 * %this.count[%pi, "lessheat"], 0.04,             %w.heatPerShot);
	%w.heatDecayPerSecond = mClamp(%w.heatDecayPerSecond + 0.08  * %this.count[%pi, "ventfast"], %w.heatDecayPerSecond, 0.80);
	%w.maxHeat            = mClamp(%w.maxHeat            + 0.2   * %this.count[%pi, "maxheat"],  %w.maxHeat,        2.0);
	%w.bulletCount        = mClamp(%w.bulletCount        +         %this.count[%pi, "split"],    %w.bulletCount,    7);
	%w.spreadAngle        = mClamp(%w.spreadAngle        -         %this.count[%pi, "tighten"],  5,                %w.spreadAngle);

	// Suit upgrades live on the player, not the weapon.
	%player.selfDestructRadius = 8   * %this.count[%pi, "selfdestruct"];
	%player.autoRepairRate     = 1.5 * %this.count[%pi, "autorepair"];

	%w.buildBulletPool();
}

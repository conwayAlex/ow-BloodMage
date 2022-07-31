# Blood Mage v0.9.2

Utilize the power of your own life essence to heal yourself and damage your enemies.

New Status:
Hemorrhage: A burst of %hp damage over a brief period, removes any bleeds upon activating.  

## From Trainer:
A shady mage has appeared in Harmattan. It's said he can be found skulking in a corner
near the closed gate by the Speedster trainer. 

#### Harness Blood
Inflict bleeding upon oneself. Allows for casting of blood magic.
If bleeding, use to further increase your bleed level. 

## Skill Tree

### Universal Skills
Different levels of bleed effect the power of these spells.

#### Vile Word: Manipulate
While bleeding, redirects your life forces to your health.
Removes stamina, increases burnt stamina, makes the player more tired.

#### Vile Word: Harm
While bleeding, fire a projectile that deals decay damage and inflicts bleeding.

#### Vile Word: Exchange
Fires a projectile that deals damage on a bleeding enemy, consuming the bleed
and healing the caster. 

### Breakthrough Skill

#### Vile Pact
Passive
You regenerate 0.1% of max hp per second. 

### Specialization Skills

#### Hypovolemia
Passive
You take less (50%) damage from bleeding but become thirstier (100%) faster.

#### Leyline Abandonment
Passive, Paired
You sever your ties with the leyline, converting all mana back into health and stamina. 
All spell costs now apply to health instead of mana. No bonuses from mana reduction is gained.

#### Leyline Entanglement
Passive, Paired
You've compromised on your power sources. All spells now split their costs between 
health and mana. Mana cost reduction applies first, then split.

#### Plebotomist
Passive
While bleeding, certain skills gain additional effects. Usage of these skills causes
hemorrhage build up.

### Phlebotomist Combos
These cause varying levels of hemorrhage to build up on the caster.
Like the Word spells, bleed level effect the power of these skills.

#### Spark
Blood Boil -> Create a small blood mist, dealing fire/decay damage.

#### Mana Ward
Humours Maintanence -> Gain resistance to decay and ethereal damage.

#### Conjure
Blood Tide -> Apply decay damage in an area around you, or if extreme bleeding, cause hemorrhage on enemies.

## Changelog
v0.9.1 
- Attempt to fix Leyline Abandonment, was unsuccessful.
v0.9.2
- Leyline Abandonment has been fixed for all normal, non-cheated scenarios
- Vile Pact will now correctly apply its regeneration effect. In theory, no action by players will be needed, should fix itself on load.
- Two of the status effects I made for Humours Maintanence were mutually exclusive with their clone, fixed this.

## Development Plan
===v1.0===
- Fix any bugs, address incompatibilities.

===v1.1===
- Skill "Expansion" (More VFX, better behaviors, more relevance of hemorrhage)
- Corruption interactions

===v1.2===
- Consumables (Blood Thinner, Coagulant...)
- Unique Armor
- Unique Weapon

===v1.3===
- Unique Enchantment
- Questline

## Known Issues
 - Blood Boil only applies decay damage. 
 - Shady Mage's dialogue tree can softlock. If you close his dialogue and reopen, it should be fine.

## Additional Comments
I wouldn't consider this mod perfect yet. Damage numbers will need tweaked and there are some bugs I'm sure. I'm hoping I can release it and
have some testing take place so I can get an idea of if anything is broken/OP/needs buffed.

## Feedback
Please direct all feedback, bugs, incompatibilities, etc to Breadcrumb_Lord in the
Blood Mage channel of the Outward Modding Discord. My goal is to keep compatibility with every thing that exists and to
promote synergy between my tree, existing trees, and future trees. 

## Credits

Thank you to Mefino and Sinai for the mod template and usage instructions. Further thanks to everyone in the discord who has
helped me so far, especially Emo for the vfx help, and Stormcancer for the artwork!

# The Last Bullet — Sound Inventory

Real recorded sounds replacing the procedural synth (`SynthAudio.cs`).
The synth stays as a fallback for anything not covered here.

- **Total: 91 files** (88 SFX + 3 ambience loops), **12.2 MB**
- **Format:** SFX are WAV, mono, 44.1 kHz, 16-bit (mono so Unity can spatialise them in 3D);
  the two long musical stings and the three ambience loops are OGG Vorbis mono 44.1 kHz.
  Every file is peak-normalised to -3 dB so volumes are consistent.
- **Largest SFX:** SFX/horde_alarm.wav at 413 KB — all SFX are under 500 KB.
- **Sources:** 67 files from Mixkit, 24 files from Kenney. No other sources.

## Licenses

### Mixkit — Mixkit Sound Effects Free License
Free for commercial and non-commercial projects, and **video games are explicitly listed**
among the permitted uses. No attribution required. Conditions: the files must not be
redistributed on their own, as stock, in a tool or template, or with source files;
they must not be claimed as your own or registered with a rights-management service.
Shipping them inside this game (an end product) is the permitted use.
Source library: https://mixkit.co/free-sound-effects/ — each file below links to the
exact preview file it was downloaded from.

### Kenney — CC0 (public domain), by Kenney Vleugels (kenney.nl)
Free for personal and commercial projects, no attribution required (credit appreciated).
Packs used: Interface Sounds, Impact Sounds, RPG Audio, Sci-Fi Sounds.
Source: https://kenney.nl/assets

## How the game uses these

- **Weapons:** every one of the 20 `WeaponId` values has its own distinct shot sound.
- **Zombies:** every one of the 16 `ZombieKind` values and all 5 `BossKind` values has its
  own distinct sound.
- **Variety:** where a sound is heard constantly, several variants are provided and the
  game should pick one at random each time — groans x4, deaths x3, flesh hits x3,
  pistol x3, shotgun x2, SMG x2, rifle x2, footsteps x4 (2 grass + 2 concrete),
  coins x2, UI clicks x2.
- **Fit:** each file was chosen by measured audio profile (duration, loudness, bass vs
  treble) so it sounds like the thing it belongs to — big/heavy things are deep and
  loud, small/fast things are short and sharp, special types sound like what they are
  (Ghost = whisper, FireZombie = crackle, Bloater = gurgle, Tesla = zap, Acid = wet
  squelch, Toxic = hiss, Sawblade = whirr). The fit note on every file says why it fits.

## Weapons (all 20 WeaponId values + reload / dry-fire / switch)

### `SFX/pistol_shot_1.wav`
- **For:** WeaponId.Pistol (variant 1)
- **Fit:** Short sharp crack — a snappy pistol report, no lingering boom.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1670/1670-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 0.92s · **Size:** 79 KB

### `SFX/pistol_shot_2.wav`
- **For:** WeaponId.Pistol (variant 2)
- **Fit:** Very short 0.18s crack — the quickest pistol variant for rapid fire.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1664/1664-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 0.18s · **Size:** 15 KB

### `SFX/pistol_shot_3.wav`
- **For:** WeaponId.Pistol (variant 3)
- **Fit:** Sharp 0.93s crack with a bright top end, distinct from variants 1-2.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1660/1660-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 0.93s · **Size:** 80 KB

### `SFX/shotgun_blast_1.wav`
- **For:** WeaponId.Shotgun (variant 1)
- **Fit:** Big 0.35s boom-crack — heavy shotgun blast, much fatter than the pistol.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1665/1665-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 0.35s · **Size:** 30 KB

### `SFX/shotgun_blast_2.wav`
- **For:** WeaponId.Shotgun (variant 2)
- **Fit:** Second heavy blast, slightly different tail from variant 1.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1659/1659-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 0.38s · **Size:** 32 KB

### `SFX/smg_burst_1.wav`
- **For:** WeaponId.Smg (variant 1)
- **Fit:** Short 0.54s rapid crack — tight burst feel for the SMG.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1668/1668-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 0.54s · **Size:** 46 KB

### `SFX/smg_burst_2.wav`
- **For:** WeaponId.Smg (variant 2)
- **Fit:** 0.84s crack with more body — a beefier SMG burst.
- **Source:** https://assets.mixkit.co/active_storage/sfx/223/223-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 0.84s · **Size:** 72 KB

### `SFX/rifle_shot_1.wav`
- **For:** WeaponId.Rifle (variant 1)
- **Fit:** 1.20s crack with an echo tail — a full-power rifle report.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1678/1678-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.2s · **Size:** 103 KB

### `SFX/rifle_shot_2.wav`
- **For:** WeaponId.Rifle (variant 2)
- **Fit:** Longer 2.60s rifle shot, deeper than variant 1.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1681/1681-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 2.6s · **Size:** 223 KB

### `SFX/crossbow_shot.wav`
- **For:** WeaponId.Crossbow
- **Fit:** 0.48s sharp swoosh-thwip — a bowstring snap, no gunpowder.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1485/1485-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 0.48s · **Size:** 41 KB

### `SFX/grenade_launcher_thump.wav`
- **For:** WeaponId.GrenadeLauncher
- **Fit:** Deep 1s low-frequency thump — the launcher's dull 'whump'.
- **Source:** https://kenney.nl/assets/sci-fi-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 1.0s · **Size:** 86 KB

### `SFX/flamethrower_whoosh.wav`
- **For:** WeaponId.Flamethrower
- **Fit:** 1.09s deep burning whoosh — fire, not a gunshot.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1326/1326-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.09s · **Size:** 93 KB

### `SFX/minigun_fire_1.wav`
- **For:** WeaponId.Minigun
- **Fit:** 2.04s loud sustained automatic fire — a heavy rattle, not a single shot.
- **Source:** https://assets.mixkit.co/active_storage/sfx/2779/2779-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 2.04s · **Size:** 175 KB

### `SFX/railgun_fire.wav`
- **For:** WeaponId.Railgun
- **Fit:** 0.68s deep energy zap — a big charged beam, heavier than the plasma.
- **Source:** https://kenney.nl/assets/sci-fi-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.68s · **Size:** 58 KB

### `SFX/freeze_ray.wav`
- **For:** WeaponId.FreezeRay
- **Fit:** 2.04s cold crystalline shimmer — icy, not hot like the plasma.
- **Source:** https://assets.mixkit.co/active_storage/sfx/2834/2834-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 2.04s · **Size:** 175 KB

### `SFX/dual_pistols_shot.wav`
- **For:** WeaponId.DualPistols
- **Fit:** 2.45s deep double blast — two pistols at once, longer and heavier than one.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1679/1679-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 2.45s · **Size:** 211 KB

### `SFX/katana_swing.wav`
- **For:** WeaponId.Katana
- **Fit:** 1.09s loud blade slice — steel through air, no gunpowder.
- **Source:** https://assets.mixkit.co/active_storage/sfx/2789/2789-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.09s · **Size:** 94 KB

### `SFX/acid_spray.wav`
- **For:** WeaponId.AcidGun
- **Fit:** 0.50s wet squelch — liquid acid, not a spray hiss.
- **Source:** https://kenney.nl/assets/sci-fi-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.5s · **Size:** 43 KB

### `SFX/tesla_zap.wav`
- **For:** WeaponId.TeslaCoil
- **Fit:** 1.02s pure high-frequency electric crackle — a real zap, all treble.
- **Source:** https://assets.mixkit.co/active_storage/sfx/2595/2595-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.02s · **Size:** 88 KB

### `SFX/harpoon_shot.wav`
- **For:** WeaponId.HarpoonGun
- **Fit:** 1.10s deep heavy whoosh — a big projectile launching, weightier than the crossbow.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1491/1491-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.1s · **Size:** 94 KB

### `SFX/nail_gun.wav`
- **For:** WeaponId.NailGun
- **Fit:** 0.18s rapid sharp 'tak' — fast, light, percussive nail taps.
- **Source:** https://assets.mixkit.co/active_storage/sfx/3005/3005-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 0.18s · **Size:** 15 KB

### `SFX/plasma_shot.wav`
- **For:** WeaponId.PlasmaCannon
- **Fit:** 0.72s energy blast — a bright plasma bolt, distinct from the railgun's deeper zap.
- **Source:** https://kenney.nl/assets/sci-fi-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.72s · **Size:** 62 KB

### `SFX/sawblade_throw.wav`
- **For:** WeaponId.SawbladeLauncher
- **Fit:** 1.76s spinning whirr — a saw blade spinning through the air.
- **Source:** https://assets.mixkit.co/active_storage/sfx/827/827-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.76s · **Size:** 151 KB

### `SFX/toxic_spray.wav`
- **For:** WeaponId.ToxicSprayer
- **Fit:** 1.64s aerosol hiss — a gas spray, airy where the acid is wet.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1890/1890-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.64s · **Size:** 141 KB

### `SFX/boomerang_whoosh.wav`
- **For:** WeaponId.BoomerangBlade
- **Fit:** 1.33s airy swoosh — a blade spinning back through the air.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1492/1492-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.33s · **Size:** 114 KB

### `SFX/reload.wav`
- **For:** Weapon reload
- **Fit:** 0.26s mechanical latch click — a magazine seating.
- **Source:** https://kenney.nl/assets/rpg-audio
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.26s · **Size:** 22 KB

### `SFX/dry_fire_click.wav`
- **For:** Weapon dry-fire (empty)
- **Fit:** 0.45s hollow metal click — an empty chamber, no bang.
- **Source:** https://kenney.nl/assets/rpg-audio
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.45s · **Size:** 38 KB

### `SFX/weapon_switch.wav`
- **For:** Weapon switch
- **Fit:** 0.62s mechanical switch — a weapon being swapped.
- **Source:** https://kenney.nl/assets/interface-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.61s · **Size:** 53 KB

## Zombies (all 16 ZombieKind values + all 5 BossKind values + deaths)

### `SFX/walker_groan.wav`
- **For:** ZombieKind.Walker (+ groan variant 1)
- **Fit:** 1.93s loud shambling groan — the classic slow walker moan.
- **Source:** https://assets.mixkit.co/active_storage/sfx/963/963-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.93s · **Size:** 166 KB

### `SFX/zombie_groan_2.wav`
- **For:** Zombie groan variant 2
- **Fit:** 1.96s groan with a deeper body than the walker groan.
- **Source:** https://assets.mixkit.co/active_storage/sfx/297/297-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.96s · **Size:** 169 KB

### `SFX/zombie_groan_3.wav`
- **For:** Zombie groan variant 3
- **Fit:** 2.09s groan, a different voice from variants 1-2.
- **Source:** https://assets.mixkit.co/active_storage/sfx/2233/2233-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 2.09s · **Size:** 180 KB

### `SFX/zombie_groan_4.wav`
- **For:** Zombie groan variant 4
- **Fit:** 1.70s shorter groan — a quicker moan so repeats vary.
- **Source:** https://assets.mixkit.co/active_storage/sfx/2207/2207-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.7s · **Size:** 146 KB

### `SFX/runner_snarl.wav`
- **For:** ZombieKind.Runner
- **Fit:** 0.69s short aggressive snarl — fast and sharp, like the Runner itself.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1968/1968-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 0.69s · **Size:** 59 KB

### `SFX/crawler_screech.wav`
- **For:** ZombieKind.Crawler
- **Fit:** 1.17s very high-pitched screech — small, fast and shrill.
- **Source:** https://assets.mixkit.co/active_storage/sfx/311/311-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.17s · **Size:** 101 KB

### `SFX/brute_roar.wav`
- **For:** ZombieKind.Brute
- **Fit:** 4.00s loudest, deepest roar in the set — a huge heavy brute.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1972/1972-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 4.0s · **Size:** 344 KB

### `SFX/spitter_spit.wav`
- **For:** ZombieKind.Spitter
- **Fit:** 1.04s wet splat — a glob of spit landing.
- **Source:** https://assets.mixkit.co/active_storage/sfx/2889/2889-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.04s · **Size:** 89 KB

### `SFX/exploder_gurgle.wav`
- **For:** ZombieKind.Exploder
- **Fit:** 5.85s bubbling gurgle, trimmed to 4s — a bloated body full of gas.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1321/1321-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 4.0s · **Size:** 344 KB

### `SFX/leaper_snarl.wav`
- **For:** ZombieKind.Leaper
- **Fit:** 2.21s snarling growl — aggressive and mid-pitched, distinct from the Runner's short snarl.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1963/1963-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 2.21s · **Size:** 190 KB

### `SFX/screamer_shriek.wav`
- **For:** ZombieKind.Screamer
- **Fit:** 3.01s loud piercing shriek — the Screamer's call.
- **Source:** https://assets.mixkit.co/active_storage/sfx/392/392-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 3.01s · **Size:** 259 KB

### `SFX/zombie_dog_growl.wav`
- **For:** ZombieKind.ZombieDog
- **Fit:** 1.62s dog growl-bark — a beast, not a human groan.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1/1-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.62s · **Size:** 139 KB

### `SFX/armored_clank.wav`
- **For:** ZombieKind.Armored
- **Fit:** 0.17s heavy metallic clang — metal plates, not flesh.
- **Source:** https://kenney.nl/assets/impact-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.16s · **Size:** 14 KB

### `SFX/bloater_gurgle.wav`
- **For:** ZombieKind.Bloater
- **Fit:** 1.05s wet bubbling gurgle — a bloated, liquid-filled body.
- **Source:** https://assets.mixkit.co/active_storage/sfx/3000/3000-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.05s · **Size:** 90 KB

### `SFX/healer_chime.wav`
- **For:** ZombieKind.Healer
- **Fit:** 1.69s soft ethereal chime — the Healer sounds magical, not monstrous.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1107/1107-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.69s · **Size:** 145 KB

### `SFX/shieldbearer_clang.wav`
- **For:** ZombieKind.Shieldbearer
- **Fit:** 0.27s metallic clang, lighter than the Armored's heavy clank — a shield hit.
- **Source:** https://kenney.nl/assets/impact-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.27s · **Size:** 23 KB

### `SFX/ghost_whisper.wav`
- **For:** ZombieKind.Ghost
- **Fit:** 7.78s breathy whisper, trimmed to 4s — whispery and ethereal, not a groan.
- **Source:** https://assets.mixkit.co/active_storage/sfx/302/302-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 4.0s · **Size:** 344 KB

### `SFX/fire_zombie_crackle.wav`
- **For:** ZombieKind.FireZombie
- **Fit:** 1.90s sharp fire crackle — burning wood and embers.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1455/1455-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.9s · **Size:** 164 KB

### `SFX/spawner_growl.wav`
- **For:** ZombieKind.Spawner
- **Fit:** 3.00s deep guttural growl — a big brooding creature that births others.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1975/1975-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 3.0s · **Size:** 258 KB

### `SFX/charger_roar.wav`
- **For:** BossKind.Charger
- **Fit:** 3.17s deep charging roar — a big boss bellow.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1730/1730-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 3.17s · **Size:** 273 KB

### `SFX/summoner_roar.wav`
- **For:** BossKind.Summoner
- **Fit:** 3.43s deep dark roar — a summoning boss, lower and longer than the Charger.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1974/1974-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 3.43s · **Size:** 295 KB

### `SFX/spitter_king_roar.wav`
- **For:** BossKind.SpitterKing
- **Fit:** 3.43s huge guttural roar — the king of spitters, bigger than any normal Spitter.
- **Source:** https://assets.mixkit.co/active_storage/sfx/2234/2234-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 3.43s · **Size:** 295 KB

### `SFX/butcher_roar.wav`
- **For:** BossKind.TheButcher
- **Fit:** 4.00s massive roar — the biggest, meanest boss voice.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1728/1728-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 4.0s · **Size:** 344 KB

### `SFX/plague_queen_roar.wav`
- **For:** BossKind.PlagueQueen
- **Fit:** 3.83s shrill raspy shriek — the Plague Queen screeches, she doesn't roar.
- **Source:** https://assets.mixkit.co/active_storage/sfx/391/391-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 3.83s · **Size:** 330 KB

### `SFX/zombie_death_1.wav`
- **For:** Zombie death variant 1
- **Fit:** 1.53s dying groan — a short collapse sound.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1966/1966-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.53s · **Size:** 131 KB

### `SFX/zombie_death_2.wav`
- **For:** Zombie death variant 2
- **Fit:** 1.43s dying groan, a different voice from variant 1.
- **Source:** https://assets.mixkit.co/active_storage/sfx/2208/2208-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.43s · **Size:** 123 KB

### `SFX/zombie_death_3.wav`
- **For:** Zombie death variant 3
- **Fit:** 1.90s dying groan — longer, more drawn-out than variants 1-2.
- **Source:** https://assets.mixkit.co/active_storage/sfx/2231/2231-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.9s · **Size:** 163 KB

### `SFX/boss_death.wav`
- **For:** Boss death
- **Fit:** 4.31s long low dying roar — a boss takes longer to fall.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1978/1978-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 4.31s · **Size:** 371 KB

## Flesh hits

### `SFX/flesh_hit_1.wav`
- **For:** Flesh hit variant 1
- **Fit:** 0.43s deep meaty thud — a bullet hitting flesh.
- **Source:** https://kenney.nl/assets/impact-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.43s · **Size:** 37 KB

### `SFX/flesh_hit_2.wav`
- **For:** Flesh hit variant 2
- **Fit:** 0.40s meaty thud, slightly different weight from variant 1.
- **Source:** https://kenney.nl/assets/impact-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.4s · **Size:** 34 KB

### `SFX/flesh_hit_3.wav`
- **For:** Flesh hit variant 3
- **Fit:** 0.54s heavier meaty thud — the hardest flesh hit.
- **Source:** https://kenney.nl/assets/impact-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.54s · **Size:** 46 KB

## Player

### `SFX/hurt_grunt.wav`
- **For:** Player hurt
- **Fit:** 0.75s pained grunt — a human hurt sound, not a zombie groan.
- **Source:** https://assets.mixkit.co/active_storage/sfx/2200/2200-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 0.75s · **Size:** 64 KB

### `SFX/player_death.wav`
- **For:** Player death
- **Fit:** 4.13s dying scream — a human cry, much longer than a zombie death.
- **Source:** https://assets.mixkit.co/active_storage/sfx/523/523-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 4.13s · **Size:** 355 KB

### `SFX/dodge_roll.wav`
- **For:** Dodge roll
- **Fit:** 0.42s cloth swish — fabric moving fast, a roll not a whoosh.
- **Source:** https://kenney.nl/assets/rpg-audio
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.42s · **Size:** 35 KB

### `SFX/footstep_grass_1.wav`
- **For:** Footstep on grass variant 1
- **Fit:** 0.78s soft grass step — muffled, outdoor.
- **Source:** https://kenney.nl/assets/impact-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.77s · **Size:** 66 KB

### `SFX/footstep_grass_2.wav`
- **For:** Footstep on grass variant 2
- **Fit:** 0.67s soft grass step, a different tread from variant 1.
- **Source:** https://kenney.nl/assets/impact-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.67s · **Size:** 57 KB

### `SFX/footstep_concrete_1.wav`
- **For:** Footstep on concrete variant 1
- **Fit:** 0.25s hard single step — a solid heel on concrete.
- **Source:** https://kenney.nl/assets/rpg-audio
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.25s · **Size:** 21 KB

### `SFX/footstep_concrete_2.wav`
- **For:** Footstep on concrete variant 2
- **Fit:** 0.24s hard single step, a different tread from variant 1.
- **Source:** https://kenney.nl/assets/rpg-audio
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.24s · **Size:** 20 KB

### `SFX/level_up.wav`
- **For:** Level up
- **Fit:** 2.64s bright ascending chime — a rewarding rise.
- **Source:** https://assets.mixkit.co/active_storage/sfx/2015/2015-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 2.64s · **Size:** 227 KB

### `SFX/perk_pick.wav`
- **For:** Perk picked
- **Fit:** 1.30s short magical sparkle — a perk being chosen.
- **Source:** https://assets.mixkit.co/active_storage/sfx/871/871-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.3s · **Size:** 112 KB

## Pickups

### `SFX/coin_1.wav`
- **For:** Coin pickup variant 1
- **Fit:** 1.02s bright coin ding — a classic coin chime.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1993/1993-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.02s · **Size:** 88 KB

### `SFX/coin_2.wav`
- **For:** Coin pickup variant 2
- **Fit:** 1.03s bright coin ding, a different pitch from variant 1.
- **Source:** https://assets.mixkit.co/active_storage/sfx/2069/2069-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.03s · **Size:** 88 KB

### `SFX/health_pickup.wav`
- **For:** Health pickup
- **Fit:** 2.00s soft gentle chime — healing, calm not flashy.
- **Source:** https://assets.mixkit.co/active_storage/sfx/3108/3108-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 2.0s · **Size:** 172 KB

### `SFX/ammo_pickup.wav`
- **For:** Ammo pickup
- **Fit:** 0.35s light metal clink — bullets in a pouch.
- **Source:** https://kenney.nl/assets/impact-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.37s · **Size:** 32 KB

### `SFX/grenade_pickup.wav`
- **For:** Grenade pickup
- **Fit:** 0.48s heavier metal clunk — a grenade, weightier than ammo.
- **Source:** https://kenney.nl/assets/impact-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.48s · **Size:** 41 KB

### `SFX/power_up.wav`
- **For:** Power-up pickup
- **Fit:** 2.00s rising power-up jingle — energising and bright.
- **Source:** https://assets.mixkit.co/active_storage/sfx/3164/3164-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 2.0s · **Size:** 172 KB

## Events & UI

### `SFX/headshot_ding.wav`
- **For:** Headshot
- **Fit:** 2.15s sharp high bell ding — a crisp 'ping' for a headshot.
- **Source:** https://assets.mixkit.co/active_storage/sfx/591/591-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 2.15s · **Size:** 185 KB

### `SFX/kill_streak.wav`
- **For:** Kill streak
- **Fit:** 1.62s rising success tone — a streak building up.
- **Source:** https://assets.mixkit.co/active_storage/sfx/2865/2865-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.62s · **Size:** 139 KB

### `SFX/wave_horn.wav`
- **For:** Wave start
- **Fit:** 2.48s deep war horn — a new wave arriving.
- **Source:** https://assets.mixkit.co/active_storage/sfx/715/715-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 2.48s · **Size:** 213 KB

### `SFX/horde_alarm.wav`
- **For:** Horde surge event
- **Fit:** 4.80s loud alarm — danger, a horde is coming.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1007/1007-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 4.8s · **Size:** 413 KB

### `SFX/supply_whistle.wav`
- **For:** Supply drop event
- **Fit:** 1.71s loud falling whistle — something dropping from the sky.
- **Source:** https://assets.mixkit.co/active_storage/sfx/608/608-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 1.71s · **Size:** 147 KB

### `SFX/boss_sting.wav`
- **For:** Boss intro
- **Fit:** 2.04s deep drum hit — a dramatic boss entrance.
- **Source:** https://assets.mixkit.co/active_storage/sfx/578/578-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 2.04s · **Size:** 175 KB

### `SFX/victory_fanfare.ogg`
- **For:** Victory / level complete
- **Fit:** 8.33s full brass fanfare — a proper win.
- **Source:** https://assets.mixkit.co/active_storage/sfx/226/226-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 8.33s · **Size:** 72 KB

### `SFX/defeat_sting.ogg`
- **For:** Defeat / game over
- **Fit:** 6.02s sad descending sting — a loss.
- **Source:** https://assets.mixkit.co/active_storage/sfx/472/472-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 6.02s · **Size:** 48 KB

### `SFX/ui_click_1.wav`
- **For:** UI click variant 1
- **Fit:** 0.10s crisp interface click.
- **Source:** https://kenney.nl/assets/interface-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.1s · **Size:** 8 KB

### `SFX/ui_click_2.wav`
- **For:** UI click variant 2
- **Fit:** 0.04s lighter interface tick — a softer tap than variant 1.
- **Source:** https://kenney.nl/assets/interface-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.04s · **Size:** 3 KB

### `SFX/ui_confirm.wav`
- **For:** UI confirm
- **Fit:** 0.29s pleasant confirm chime — a 'yes'.
- **Source:** https://kenney.nl/assets/interface-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.29s · **Size:** 25 KB

### `SFX/ui_back.wav`
- **For:** UI back
- **Fit:** 0.06s short back blip — a step back.
- **Source:** https://kenney.nl/assets/interface-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.06s · **Size:** 5 KB

### `SFX/ui_error.wav`
- **For:** UI error
- **Fit:** 0.16s low error buzz — a 'no'.
- **Source:** https://kenney.nl/assets/interface-sounds
- **License:** CC0 (public domain) — Kenney Vleugels (kenney.nl)
- **Length:** 0.16s · **Size:** 14 KB

### `SFX/explosion_big.wav`
- **For:** Big explosion (grenade / Exploder death)
- **Fit:** 4.10s massive deep boom with almost no treble — a real explosion, not a pop.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1686/1686-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 4.1s · **Size:** 353 KB

## Ambience loops

### `Ambience/night_wind_loop.ogg`
- **For:** Ambience: night wind loop
- **Fit:** 54.98s deep night wind — a cold empty night.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1162/1162-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 54.98s · **Size:** 334 KB

### `Ambience/rain_loop.ogg`
- **For:** Ambience: rain loop
- **Fit:** 39.63s steady rain — a wet night.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1248/1248-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 39.63s · **Size:** 345 KB

### `Ambience/thunder_rumble_loop.ogg`
- **For:** Ambience: thunder rumble loop
- **Fit:** 22.74s deep thunder rumble, loop-extended to ~43s — distant rolling thunder.
- **Source:** https://assets.mixkit.co/active_storage/sfx/1297/1297-preview.mp3
- **License:** Mixkit Sound Effects Free License
- **Length:** 43.48s · **Size:** 322 KB


## Not found / still synth

Nothing on the required list is missing. Two things were deliberately left to the
synth fallback rather than forced with a bad fit:

- **Rocket launcher** — there is no `RocketLauncher` in the game's `WeaponId` enum
  (the 20 weapons are Pistol, Shotgun, Smg, Rifle, Crossbow, GrenadeLauncher,
  Flamethrower, Minigun, Railgun, FreezeRay, DualPistols, Katana, AcidGun, TeslaCoil,
  HarpoonGun, NailGun, PlasmaCannon, SawbladeLauncher, ToxicSprayer, BoomerangBlade),
  so no rocket sound was sourced. If a rocket weapon is added later, the big
  explosion (`explosion_big.wav`) plus a whoosh covers the launch.
- **Sniper / revolver** — also not in the enum, so no files were made for them.

## Credits (for the in-game credits screen)

- Sound effects: Mixkit (mixkit.co) — Mixkit Sound Effects Free License, no attribution
  required (a "Sounds: Mixkit" line is a nice touch, not a licence requirement).
- Kenney (kenney.nl) — CC0, no attribution required (credit appreciated).

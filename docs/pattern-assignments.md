# Pattern assignments

The course requires **22** patterns. The catalogue below lists all **23** GoF patterns mapped
onto real Unhallowed features, so the team can **drop one** deliberately rather than discovering
a gap late.

Requirement 11: each student claims patterns and reports on them. Put your name in the Owner
column **and** in the matching `docs/patterns/NN-*.md` file, then keep both in sync.

Requirement 15 is cumulative - a pattern can be defended as soon as it is done. Aim to finish
and defend in small batches rather than all at once at the end.

## Suggested drop

**15. Interpreter** is the usual one to cut. It is the hardest to justify in a game like this
(the item-synergy language is a real but contrived use), and it is the most time-consuming to
build well. If someone genuinely wants it, drop **23. Visitor** instead - collision resolution
can be handled with straightforward double dispatch without the ceremony.

Decide this as a team early and mark the dropped row below.

## Catalogue

Legend: **done** = code exists and a demo runs; **wip** = in progress on a branch; **todo** = not
started; **defended** = accepted by the lecturer.

### Creational

| # | Pattern | Where it lives in the game | Owner | Status |
| --- | --- | --- | --- | --- |
| 01 | Singleton | `ServiceRegistry` - one service locator per process | | done |
| 02 | Factory Method | `EnemyFactory.Create(archetype)` replacing direct `new Enemy(...)` | | todo |
| 03 | Abstract Factory | `IBiomeFactory` - Basement / Caves produce matching families | | todo |
| 04 | Builder | `DungeonBuilder` - floor layout assembled step by step | | todo |
| 05 | Prototype | Item and enemy prototype registry, cloned per spawn | | todo |

### Structural

| # | Pattern | Where it lives in the game | Owner | Status |
| --- | --- | --- | --- | --- |
| 06 | Adapter | `GdiRenderSurface` adapts `Graphics` to `IRenderSurface` | | done |
| 07 | Bridge | Weapon abstraction x projectile behaviour | | todo |
| 08 | Composite | HUD element tree | | todo |
| 09 | Decorator | Item pickups wrapping `IPlayerStats` | | done |
| 10 | Facade | `GameClient` over SignalR, JSON, reconnect, interpolation | | done |
| 11 | Flyweight | Shared intrinsic tile data | | todo |
| 12 | Proxy | `RemotePlayerProxy` for server-simulated players | | todo |

### Behavioral

| # | Pattern | Where it lives in the game | Owner | Status |
| --- | --- | --- | --- | --- |
| 13 | Chain of Responsibility | Damage pipeline: i-frames, shield, armour, health | | todo |
| 14 | Command | Player intents as objects, serialised over SignalR | | done |
| 15 | Interpreter | Item synergy mini-language | | todo |
| 16 | Iterator | Dungeon graph traversal for minimap reveal | | todo |
| 17 | Mediator | `CombatMediator` coordinating entity interactions | | todo |
| 18 | Memento | `WorldSnapshot` for the wire protocol and rollback | | done |
| 19 | Observer | `EventBus` with explicit Subject/Observer interfaces | | done |
| 20 | State | Player states: Idle, Moving, Hurt, Dead | | done |
| 21 | Strategy | Enemy movement: Chaser, Wanderer, Charger, Stationary | | done |
| 22 | Template Method | `RoomBase.Enter` fixes the room lifecycle | | done |
| 23 | Visitor | Collision resolution and end-of-run statistics | | todo |

## How to pick

Some practical advice on dividing the work.

**Start with the ones already scaffolded.** Eight patterns have working code and a runnable demo.
Whoever takes one of those still has to write the full report and, crucially, be able to *modify*
the code and diagrams live during the defence (requirement 13). The code being there is a head
start, not a free pass - read it until you could have written it.

**Spread the difficulty.** Rough ranking, easiest first:

- *Comfortable:* Singleton, Strategy, Observer, Template Method, Facade, Adapter, State
- *Middling:* Factory Method, Decorator, Builder, Iterator, Command, Composite, Prototype
- *Harder to justify or build:* Abstract Factory, Bridge, Flyweight, Proxy, Chain of
  Responsibility, Mediator, Memento, Visitor, Interpreter

Nobody should own only easy ones or only hard ones.

**Watch the pattern pairs examiners compare.** If you own one of these, learn the other well
enough to say why you did not use it:

- Strategy vs State - both swap an object to change behaviour; Strategy is chosen from outside,
  State transitions itself
- Factory Method vs Abstract Factory - one product vs a family of related products
- Decorator vs Bridge - adding responsibilities vs splitting two axes of variation
- Adapter vs Facade - fixing an incompatible interface vs simplifying a complicated one
- Proxy vs Decorator - same structure, different intent (control access vs add behaviour)

**Keep one pattern per branch and per PR** (see [CLAUDE.md](../CLAUDE.md)). It makes the
cumulative defence easy: point the lecturer at one PR.

## Before you defend

- [ ] Report section filled in for all eight headings
- [ ] Both UML diagrams exported from MagicDraw, classes tidily arranged (requirement 14)
- [ ] Demo runs from `main()` and you know which output line proves the pattern (requirement 10)
- [ ] You can add a new participant live if asked (a new strategy, a new decorator, a new room)
- [ ] Report uploaded to Moodle (requirement 12)

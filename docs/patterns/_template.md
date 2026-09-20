# NN. Pattern Name

> Report skeleton for one defended pattern. Every heading below maps to a bullet of
> requirement 9, so do not delete headings - fill them in. Requirement 13 means you must be
> able to explain and *modify* all of this live, so write it in your own words.

- **Category:** Creational / Structural / Behavioral
- **Owner:** *(requirement 11 - who reports on this)*
- **Code:** `src/...`
- **Demo:** `dotnet run --project src/Unhallowed.PatternDemos -- <key>`
- **Status:** planned / implemented / defended

---

## 1. Problem description

*What specifically goes wrong in Unhallowed without this pattern?*

Describe the concrete situation in our game, not the textbook one. Name the classes involved
and the change that would be painful. A good answer usually has this shape:

> When we add a new *X*, we currently have to edit *A*, *B* and *C*, because ...

Include the pain you actually felt: a long `switch`, a class that grew every time a feature
landed, duplicated code across three enemy types, a merge conflict the whole team hit.

## 2. Why this pattern

*Justify the choice. Answer all three:*

- **What forces does it resolve?** Which axis of change does it isolate?
- **Why this pattern and not a neighbour?** (Strategy vs State, Factory Method vs Abstract
  Factory, Decorator vs Bridge, Adapter vs Facade - examiners ask exactly this.)
- **What does it cost?** More classes, more indirection, harder to trace in a debugger. Say so
  honestly; pretending a pattern is free is a weak defence.

## 3. UML class diagram - BEFORE

*The design as it stood before the pattern. Export from MagicDraw (requirement 14).*

![before](../uml/NN-<key>-before.png)

Must show the problem visually: the fat class, the repeated branch, the wrong dependency
direction.

## 4. UML class diagram - AFTER

*The design once the pattern is applied. Export from MagicDraw (requirement 14).*

![after](../uml/NN-<key>-after.png)

Label the GoF roles on the diagram (Context, Strategy, ConcreteStrategy, ...) so the mapping
between the book and our code is obvious at a glance.

## 5. Essential code fragment

*The smallest excerpt that makes the pattern legible. Usually 15-40 lines: the participant
interface plus one concrete participant plus the call site that uses it.*

```csharp
// src/Unhallowed.Core/...
```

Add one sentence under the fragment naming each GoF role:

| GoF role | Our type |
| --- | --- |
| *Context* | `...` |
| *Strategy* | `...` |
| *ConcreteStrategy* | `...` |

## 6. Specific requirements for this pattern

*Every pattern has details an examiner will probe. Show that yours are handled.* Examples:

- **Singleton** - private constructor, thread-safe lazy init, why not a static class
- **Decorator** - component and decorator share an interface; order of wrapping matters
- **Observer** - detach works; no leak; safe to modify the observer list while notifying
- **Command** - undo/redo or replay; command is independent of its receiver
- **Template Method** - the template method itself is non-virtual; hooks vs abstract steps
- **Memento** - the caretaker cannot read the memento's contents
- **Factory Method** - the creator does not know the concrete product type

## 7. Demonstration

How to run it, and what to point at on screen (requirement 10):

```
dotnet run --project src/Unhallowed.PatternDemos -- <key>
```

Expected output, and the one or two lines that prove the pattern is doing its job:

```
...
```

If the pattern is also visible in the running game, say where: *"start the server, join with two
clients, and watch ..."*

## 8. Defence notes

Questions you should expect, and your answers:

- *"What if I asked you to add a new ...?"* - show the one file you would add.
- *"Why not just use an `if`?"* - have a real answer about how often this axis changes.
- *"Change this diagram to ..."* - practise once before the defence (requirement 13).

# 22. Template Method

- **Category:** Behavioral
- **Owner:** _unclaimed - put your name here (requirement 11)_
- **Code:** `src/Unhallowed.Core/Rooms/RoomBase.cs`
- **Demo:** `dotnet run --project src/Unhallowed.PatternDemos -- template-method`
- **Status:** implemented

> Code scaffolded and demonstrable. **The report below is still yours to write.**
> Fill in every section: each one maps to a bullet of requirement 9.
> See [\_template.md](_template.md) for what a good answer to each looks like.

## Where it goes in Unhallowed

RoomBase.Enter fixes the room lifecycle (populate, seal, clear, reward, unseal); each room type fills in the variable steps.

## 1. Problem description

_What breaks or becomes painful in our codebase without this pattern? Name the real classes._

## 2. Why this pattern

_Which forces does it resolve, why not the neighbouring pattern, and what does it cost us?_

## 3. UML class diagram - BEFORE

![before](../uml/22-template-method-before.png)

_Export from MagicDraw (requirement 14)._

## 4. UML class diagram - AFTER

![after](../uml/22-template-method-after.png)

_Export from MagicDraw (requirement 14). Label the GoF roles._

## 5. Essential code fragment

```csharp
// src/Unhallowed.Core/Rooms/RoomBase.cs
```

| GoF role | Our type |
| --- | --- |
|  |  |

## 6. Specific requirements for this pattern

_What will the examiner probe? Show it is handled._

## 7. Demonstration

```
dotnet run --project src/Unhallowed.PatternDemos -- template-method
```

_Paste the output and point at the lines that prove the pattern works (requirement 10)._

## 8. Defence notes

_Expected questions and your answers (requirement 13)._

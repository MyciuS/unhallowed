# UML generator

MagicDraw can't reverse-engineer C# (its code engineering only covers Java, C++, CORBA IDL and XML
Schema), so this folder does it instead:

1. `extract/` reads the game's C# sources with Roslyn (the C# compiler's parser) and writes every
   type, attribute, operation, base type and create/use dependency to `model.json`.
2. `Build.groovy` runs MagicDraw in batch mode, through its OpenAPI, and builds the model from that
   JSON:
   - one package per namespace
   - all classes with their members
   - generalizations, interface realizations and associations, with composition/aggregation where
     the code owns the object
   - three laid-out class diagrams: the whole game, *Prototype - before* and *Prototype - after*

   It saves `docs/uml/Isaac-Coop-model.mdzip` and exports the diagrams as PNG.

Run it after changing the code, from Git Bash:

```bash
tools/uml/generate.sh
```

It needs the .NET SDK and a licensed Magic Systems of Systems Architect 2026x. Set `MAGICDRAW_HOME`
if it isn't installed in `D:/MSOSA`. MagicDraw can stay open while it runs: the batch copy is a
separate instance.

To add a diagram for a new pattern, copy `fillAfterDiagram` in `Build.groovy` and list that
pattern's classes.

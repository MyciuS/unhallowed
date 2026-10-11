#!/usr/bin/env bash
# Regenerates docs/uml/Isaac-Coop-model.mdzip and the diagram PNGs from the C# sources.
# Needs the .NET SDK and a licensed Magic Systems of Systems Architect (MagicDraw) 2026x on Windows.
# Run from Git Bash: tools/uml/generate.sh   (set MAGICDRAW_HOME if it isn't installed in D:/MSOSA)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd -W)"
MD="${MAGICDRAW_HOME:-D:/MSOSA}"
WORK="$ROOT/tools/uml/out"
rm -rf "$WORK" && mkdir -p "$WORK"

echo "1/2 reading the C# sources"
dotnet run --project "$ROOT/tools/uml/extract" -- "$ROOT" "$WORK/model.json"

echo "2/2 building the MagicDraw project (takes about a minute)"
(cd "$MD" && ./jre/bin/java.exe -Xmx3G \
    -Desi.system.config="$MD/data/application.conf" -Dfile.encoding=UTF-8 @"$MD/bin/vm.options" \
    -cp "$MD/plugins/com.nomagic.magicdraw.automaton/lib/groovy-5.0.0.jar;$MD/lib/classpath.jar" \
    groovy.ui.GroovyMain "$ROOT/tools/uml/Build.groovy" NOGUI \
    "model=$WORK/model.json" "out=$WORK/Isaac-Coop-model.mdzip" "images=$WORK/images") >/dev/null 2>&1 || true

if [[ ! -f "$WORK/Isaac-Coop-model.mdzip" ]]; then
    echo "MagicDraw did not produce the project. Its output is in the msosa.log of the newest instance under" >&2
    echo "  %LOCALAPPDATA%/.magic.systems.of.systems.architect/2026x/" >&2
    exit 1
fi

cp "$WORK/Isaac-Coop-model.mdzip" "$ROOT/docs/uml/"
cp "$WORK/images/Isaac_Co-op_-_main_classes.png" "$ROOT/docs/uml/main-class-diagram.png"
cp "$WORK/images/Isaac_Co-op_-_class_diagram.png" "$ROOT/docs/uml/game-class-diagram.png"
cp "$WORK/images/Prototype_-_after.png" "$ROOT/docs/uml/prototype-after.png"
cp "$WORK/images/Prototype_-_before.png" "$ROOT/docs/uml/prototype-before.png"
echo "done: docs/uml/Isaac-Coop-model.mdzip and the diagram PNGs are up to date"

import com.google.gson.Gson
import com.nomagic.magicdraw.commandline.CommandLine
import com.nomagic.magicdraw.core.Application
import com.nomagic.magicdraw.core.project.ProjectDescriptorsFactory
import com.nomagic.magicdraw.export.image.ImageExporter
import com.nomagic.magicdraw.openapi.uml.ModelElementsManager
import com.nomagic.magicdraw.openapi.uml.PresentationElementsManager
import com.nomagic.magicdraw.openapi.uml.SessionManager
import com.nomagic.magicdraw.properties.BooleanProperty
import com.nomagic.magicdraw.properties.PropertyID
import com.nomagic.magicdraw.properties.PropertyManager
import com.nomagic.magicdraw.uml.DiagramTypeConstants
import com.nomagic.magicdraw.uml.symbols.layout.Layouting
import com.nomagic.uml2.ext.jmi.helpers.CoreHelper
import com.nomagic.uml2.ext.jmi.helpers.ModelHelper
import com.nomagic.uml2.ext.jmi.helpers.StereotypesHelper
import com.nomagic.uml2.ext.magicdraw.classes.mdkernel.AggregationKindEnum
import com.nomagic.uml2.ext.magicdraw.classes.mdkernel.ParameterDirectionKindEnum
import com.nomagic.uml2.ext.magicdraw.classes.mdkernel.VisibilityKindEnum

// Builds a MagicDraw project from model.json (extracted from the C# sources): every class with
// its attributes, operations and relationships, plus the game and Prototype class diagrams.
class Build extends CommandLine {
    static Map<String, String> cfg = [:]

    static void main(String[] args) {
        args.each { a -> int i = a.indexOf('='); if (i > 0) cfg[a.substring(0, i)] = a.substring(i + 1) }
        new Build().launch(args)
    }

    def project, ef, pem
    Map<String, Object> byName = [:]
    Map<String, String> kindOf = [:]
    Map<String, Object> packages = [:]
    Map<String, Object> datatypes = [:]
    Set<String> related = new HashSet<>()
    List<String> problems = []
    List structural = []   // [relationship, from, to]: generalizations, realizations, associations

    static final Map<String, AggregationKindEnum> AGGREGATION = [
        'Character.Stats'                : AggregationKindEnum.COMPOSITE,
        'CharacterRegistry._prototypes'  : AggregationKindEnum.SHARED,
        'World.Players'                  : AggregationKindEnum.COMPOSITE,
        'World.Enemies'                  : AggregationKindEnum.COMPOSITE,
        'World.Tears'                    : AggregationKindEnum.COMPOSITE,
        'World._characters'              : AggregationKindEnum.COMPOSITE,
        'World._director'                : AggregationKindEnum.COMPOSITE,
        'World.Events'                   : AggregationKindEnum.COMPOSITE,
        'Floor.Rooms'                    : AggregationKindEnum.COMPOSITE,
        'Room.Pickups'                   : AggregationKindEnum.COMPOSITE,
        'HostSession._world'             : AggregationKindEnum.COMPOSITE,
        'HostSession._inbox'             : AggregationKindEnum.COMPOSITE,
        'IsaacGame._menu'                : AggregationKindEnum.COMPOSITE,
    ]

    protected byte execute() {
        try {
            run()
            return 0
        } catch (Throwable t) {
            t.printStackTrace()
            println "FAILED: $t"
            return -1
        }
    }

    void run() {
        def model = new Gson().fromJson(new File(cfg.model).getText('UTF-8'), Map)
        List types = model.types
        def pm = Application.getInstance().getProjectsManager()
        project = pm.createProject()
        ef = project.getElementsFactory()
        pem = PresentationElementsManager.getInstance()

        SessionManager.getInstance().createSession(project, 'Generate Isaac model')
        types.each { t -> safe("classifier ${t.name}") { createClassifier(t) } }
        types.each { t -> safe("bases of ${t.name}") { addBases(t) } }
        types.each { t ->
            def owner = byName[base(t.name)]
            t.attributes.each { a -> safe("attr ${t.name}.${a.name}") { addAttribute(t, owner, a) } }
            t.operations.each { o -> safe("op ${t.name}.${o.name}") { addOperation(owner, o) } }
        }
        types.each { t -> safe("deps of ${t.name}") { addDependencies(t) } }
        safe('registry clones through IPrototype') { usage('CharacterRegistry', 'IPrototype', false) }
        def before = buildBeforeModel()

        def full = classDiagram(pkg('Isaac'), 'Isaac Co-op - class diagram')
        def after = classDiagram(pkg('Prototype pattern'), 'Prototype - after')
        def beforeDiagram = classDiagram(pkg('Prototype pattern'), 'Prototype - before')
        safe('full diagram') { fillFullDiagram(full, types) }
        safe('after diagram') { fillAfterDiagram(after) }
        safe('before diagram') { fillBeforeDiagram(beforeDiagram, before) }
        SessionManager.getInstance().closeSession(project)

        [full, after, beforeDiagram].each { d -> safe("layout ${d.getHumanName()}") { d.open(); Layouting.layout(d) } }

        println "MODEL classifiers=${byName.size()} datatypes=${datatypes.size()} problems=${problems.size()}"
        problems.each { println "  PROBLEM: $it" }
        def out = new File(cfg.out)
        out.parentFile.mkdirs()
        boolean saved = pm.saveProject(ProjectDescriptorsFactory.createLocalProjectDescriptor(project, out), true)
        println "saved=${saved} -> ${out}"

        def images = new File(cfg.images)
        images.mkdirs()
        [full, after, beforeDiagram].each { d ->
            safe("image ${d.getName()}") {
                ImageExporter.export(d, ImageExporter.PNG, new File(images, d.getName().replaceAll(/[^A-Za-z0-9-]+/, '_') + '.png'))
            }
        }
        println "classifiers=${byName.size()} datatypes=${datatypes.size()} problems=${problems.size()}"
        problems.each { println "  PROBLEM: $it" }
    }

    // ------------------------------------------------------------------ model

    def createClassifier(Map t) {
        def el
        switch (t.kind) {
            case 'interface': el = ef.createInterfaceInstance(); break
            case 'enum': el = ef.createEnumerationInstance(); break
            case 'struct': el = ef.createDataTypeInstance(); break
            default: el = ef.createClassInstance()
        }
        el.setName(t.name)
        el.setOwner(pkg(nsPath(t.namespace)))
        el.setVisibility(vis(t.visibility))
        if (t.isAbstract) el.setAbstract(true)
        if (t.doc) CoreHelper.setComment(el, t.doc)
        if (t.kind == 'enum') t.literals.each { l ->
            def lit = ef.createEnumerationLiteralInstance()
            lit.setName(l)
            el.getOwnedLiteral().add(lit)
        }
        byName[base(t.name)] = el
        kindOf[base(t.name)] = t.kind
    }

    def addBases(Map t) {
        def el = byName[base(t.name)]
        t.bases.each { String b ->
            def sup = byName[b] ?: external(b)
            String supKind = kindOf[b] ?: (b ==~ /I[A-Z].*/ ? 'interface' : 'class')
            def rel = (supKind == 'interface' && t.kind != 'interface')
                ? ef.createInterfaceRealizationInstance()
                : ef.createGeneralizationInstance()
            CoreHelper.setClientElement(rel, el)
            CoreHelper.setSupplierElement(rel, sup)
            rel.setOwner(el)
            structural << [rel, el, sup]
            relate(base(t.name), b)
        }
    }

    def addAttribute(Map t, owner, Map a) {
        def r = resolve(a.type)
        def prop
        if (r.simple && kindOf[r.simple] in ['class', 'interface']) {
            prop = association(owner, r.el, AGGREGATION["${base(t.name)}.${a.name}".toString()])
            relate(base(t.name), r.simple)
        } else {
            prop = ef.createPropertyInstance()
            prop.setType(r.el)
            owner.getOwnedAttribute().add(prop)
        }
        prop.setName(a.name)
        prop.setVisibility(vis(a.visibility))
        if (a.isStatic) prop.setStatic(true)
        if (r.mult) CoreHelper.setMultiplicity(r.mult, prop)
        if (a.init) {
            def lit = ef.createLiteralStringInstance()
            lit.setValue(a.init)
            prop.setDefaultValue(lit)
        }
        prop
    }

    // The returned end is the attribute of `owner` that points at `target`.
    def association(owner, target, aggregation) {
        def assoc = ef.createAssociationInstance()
        CoreHelper.setClientElement(assoc, owner)
        CoreHelper.setSupplierElement(assoc, target)
        assoc.setOwner(owner.getOwner())
        structural << [assoc, owner, target]
        def e1 = CoreHelper.getFirstMemberEnd(assoc)
        def e2 = CoreHelper.getSecondMemberEnd(assoc)
        if (e1.getType() == null && e2.getType() == null) { e1.setType(owner); e2.setType(target) }
        def end = (e1.getType() == target && e2.getType() != target) ? e1 : e2
        def other = end.is(e1) ? e2 : e1
        ModelHelper.setNavigable(end, true)
        ModelHelper.setNavigable(other, false)
        if (end.getOwner() != owner) owner.getOwnedAttribute().add(end)
        if (aggregation) end.setAggregation(aggregation)
        end
    }

    def addOperation(owner, Map o) {
        def op = ef.createOperationInstance()
        op.setName(o.name)
        owner.getOwnedOperation().add(op)
        op.setVisibility(vis(o.visibility))
        if (o.isStatic) op.setStatic(true)
        if (o.isAbstract) op.setAbstract(true)
        o.parameters.each { p -> param(op, p.name, p.type, ParameterDirectionKindEnum.IN) }
        if (o.returnType) param(op, null, o.returnType, ParameterDirectionKindEnum.RETURN)
        op
    }

    def param(op, String name, String type, direction) {
        def r = resolve(type)
        def p = ef.createParameterInstance()
        if (name) p.setName(name)
        p.setType(r.el)
        p.setDirection(direction)
        op.getOwnedParameter().add(p)
        if (r.mult) CoreHelper.setMultiplicity(r.mult, p)
    }

    def addDependencies(Map t) {
        String from = base(t.name)
        t.creates.each { usage(from, it, true) }
        t.uses.each { usage(from, it, false) }
    }

    def usage(String from, String to, boolean create) {
        if (to == 'Config' || to == from || !(kindOf[to] in ['class', 'interface'])) return
        if (related.contains("${from}->${to}".toString())) return
        def u = ef.createUsageInstance()
        CoreHelper.setClientElement(u, byName[from])
        CoreHelper.setSupplierElement(u, byName[to])
        u.setOwner(byName[from].getOwner())
        if (create) {
            def st = StereotypesHelper.getStereotype(project, 'Create', null)
            if (st) StereotypesHelper.addStereotype(u, st) else u.setName('create')
        }
        relate(from, to)
    }

    // Prototype "before": players built with new and the same hard-coded Stats.
    Map buildBeforeModel() {
        def p = pkg('Prototype pattern::Before')
        def world = newClass(p, 'World'), player = newClass(p, 'Player'), stats = newClass(p, 'Stats')
        def itemKind = byName['ItemKind']
        def players = association(world, player, AggregationKindEnum.COMPOSITE)
        players.setName('Players'); players.setVisibility(VisibilityKindEnum.PUBLIC); CoreHelper.setMultiplicity('0..4', players)
        simpleOp(world, 'AddPlayer', [['id', 'int']], player)
        simpleOp(world, 'RemovePlayer', [['id', 'int']], null)
        simpleOp(world, 'Restart', [], null)

        [['Id', 'int'], ['Pos', 'Vector2'], ['Health', 'int']].each { n, ty -> simpleAttr(player, n, dt(ty)) }
        def items = simpleAttr(player, 'Items', itemKind); CoreHelper.setMultiplicity('*', items)
        def st = association(player, stats, AggregationKindEnum.COMPOSITE)
        st.setName('Stats'); st.setVisibility(VisibilityKindEnum.PUBLIC); CoreHelper.setMultiplicity('1', st)
        simpleOp(player, 'Player', [['id', 'int']], null)
        simpleOp(player, 'GiveItem', [['item', itemKind]], null)
        simpleOp(player, 'TryDamage', [['halfHearts', 'int']], dt('bool'))

        [['Speed', 'float'], ['Damage', 'float'], ['FireDelay', 'float'], ['ShotSpeed', 'float'],
         ['Range', 'float'], ['TearSize', 'float'], ['MaxHealth', 'int']].each { n, ty -> simpleAttr(stats, n, dt(ty)) }
        simpleOp(stats, 'Apply', [['item', itemKind]], null)

        def note = ef.createCommentInstance()
        note.setBody('Before Prototype: every player is created with new Player(id), which calls new Stats() ' +
                     'with the same hard-coded values. Different characters would need a subclass or a switch per character.')
        note.setOwner(p)
        [world: world, player: player, stats: stats, itemKind: itemKind, note: note]
    }

    def newClass(p, String name) { def c = ef.createClassInstance(); c.setName(name); c.setOwner(p); c }

    def simpleAttr(owner, String name, type) {
        def a = ef.createPropertyInstance()
        a.setName(name); a.setType(type); a.setVisibility(VisibilityKindEnum.PUBLIC)
        owner.getOwnedAttribute().add(a)
        a
    }

    def simpleOp(owner, String name, List params, ret) {
        def op = ef.createOperationInstance()
        op.setName(name); op.setVisibility(VisibilityKindEnum.PUBLIC)
        owner.getOwnedOperation().add(op)
        params.each { n, ty ->
            def p = ef.createParameterInstance(); p.setName(n); p.setType(ty instanceof String ? dt(ty) : ty)
            p.setDirection(ParameterDirectionKindEnum.IN); op.getOwnedParameter().add(p)
        }
        if (ret) {
            def r = ef.createParameterInstance(); r.setType(ret); r.setDirection(ParameterDirectionKindEnum.RETURN)
            op.getOwnedParameter().add(r)
        }
    }

    // --------------------------------------------------------------- diagrams

    def classDiagram(owner, String name) {
        def d = ModelElementsManager.getInstance().createDiagram(DiagramTypeConstants.UML_CLASS_DIAGRAM, owner)
        d.setName(name)
        def view = project.getDiagram(d)
        view.open()
        view
    }

    // The whole-game diagram draws only structure (inheritance, realization, association);
    // the many create/use dependencies stay in the model but would bury the picture in lines.
    def fillFullDiagram(view, List types) {
        def shapes = [:]
        def owners = byName.values().findAll { kindOf[it.getName().replaceAll(/<.*/, '')] }.groupBy { it.getOwner() }
        owners.each { pkgEl, members ->
            def frame = pem.createShapeElement(pkgEl, view)
            members.each { m -> shapes[m] = pem.createShapeElement(m, frame, false) }
        }
        structural.each { rel, from, to ->
            if (shapes[from] && shapes[to]) safe("path ${from.getName()}->${to.getName()}") {
                pem.createPathElement(rel, shapes[from], shapes[to])
            }
        }
    }

    def fillAfterDiagram(view) {
        ['IPrototype', 'Character', 'Stats', 'CharacterRegistry', 'World', 'Player', 'Program', 'PatternDemos', 'PrototypeDemo'].each { n ->
            def shape = pem.createShapeElement(byName[n], view, true)
            if (n == 'World') suppress(shape)
        }
        def note = ef.createCommentInstance()
        note.setBody('After Prototype: each character is a configured Character prototype in CharacterRegistry. ' +
                     'World.AddPlayer asks the registry for a clone; Clone() only knows IPrototype and calls DeepClone(), ' +
                     'so item pickups change the player\'s own copy and never the prototype.')
        note.setOwner(pkg('Prototype pattern'))
        pem.createShapeElement(note, view)
    }

    def fillBeforeDiagram(view, Map before) {
        [before.world, before.player, before.stats, before.itemKind].each { pem.createShapeElement(it, view, true) }
        pem.createShapeElement(before.note, view)
    }

    def suppress(shape) {
        def props = new PropertyManager()
        props.addProperty(new BooleanProperty(PropertyID.SUPPRESS_CLASS_ATTRIBUTES, true))
        props.addProperty(new BooleanProperty(PropertyID.SUPPRESS_CLASS_OPERATIONS, true))
        pem.setPresentationElementProperties(shape, props)
    }

    // ---------------------------------------------------------------- helpers

    // Collections and arrays become a multiplicity; model types resolve to the real element.
    Map resolve(String text) {
        String t = text.trim()
        String mult = null
        if (t.endsWith('?')) { t = t[0..-2]; mult = '0..1' }
        if (t.endsWith('[]')) { t = t[0..-3]; mult = '*' }
        def m = t =~ /^(List|IReadOnlyList|IReadOnlyCollection|IEnumerable|ICollection|HashSet|ConcurrentQueue|Queue)<(.+)>$/
        if (m.matches()) { t = m.group(2).trim(); mult = '*' }
        def d = t =~ /^(Dictionary|IReadOnlyDictionary|ConcurrentDictionary)<.+,\s*([A-Za-z0-9_]+)>$/
        if (d.matches()) { t = d.group(2).trim(); mult = '*' }
        String simple = base(t)
        if (byName.containsKey(simple) && !t.contains('[')) return [el: byName[simple], mult: mult, simple: simple]
        [el: dt(text.trim()), mult: null, simple: null]
    }

    def dt(String name) {
        if (!datatypes.containsKey(name)) {
            def d = ef.createDataTypeInstance()
            d.setName(name)
            d.setOwner(pkg('C# types'))
            datatypes[name] = d
        }
        datatypes[name]
    }

    def external(String name) {
        if (!byName.containsKey('ext:' + name)) {
            def e = name ==~ /I[A-Z].*/ ? ef.createInterfaceInstance() : ef.createClassInstance()
            e.setName(name)
            e.setOwner(pkg('External .NET'))
            byName['ext:' + name] = e
        }
        byName['ext:' + name]
    }

    def pkg(String path) {
        if (!packages.containsKey(path)) {
            def parts = path.split('::')
            def parent = parts.length == 1 ? project.getPrimaryModel() : pkg(parts[0..-2].join('::'))
            def p = ef.createPackageInstance()
            p.setName(parts[-1])
            p.setOwner(parent)
            packages[path] = p
        }
        packages[path]
    }

    static String nsPath(String ns) {
        if (!ns || ns == 'Isaac') return 'Isaac'
        ns.split(/\./).join('::')
    }

    static String base(String name) { int i = name.indexOf('<'); i >= 0 ? name.substring(0, i) : name }

    static VisibilityKindEnum vis(String v) {
        switch (v) {
            case 'public': return VisibilityKindEnum.PUBLIC
            case 'protected': return VisibilityKindEnum.PROTECTED
            case 'package': return VisibilityKindEnum.PACKAGE
            default: return VisibilityKindEnum.PRIVATE
        }
    }

    void relate(String a, String b) { related << "${a}->${b}".toString(); related << "${b}->${a}".toString() }

    void safe(String what, Closure c) {
        try { c() } catch (Throwable t) { problems << "${what}: ${t.class.simpleName}: ${t.message}".toString() }
    }
}

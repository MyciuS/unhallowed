namespace Isaac.Core;

/// <summary>
/// PROTOTYPE pattern, the Prototype interface: an object that can produce copies of itself,
/// so new objects are made by cloning a ready-made instance instead of calling <c>new</c>.
/// Both kinds of copy are exposed so they can be compared directly:
/// - <see cref="ShallowClone"/> copies the fields one to one. Value-type fields get their own
///   copy, but reference-type fields in the copy still point at the original's objects.
/// - <see cref="DeepClone"/> also copies every mutable object the original refers to, so the
///   copy shares nothing that can change.
/// </summary>
public interface IPrototype<out T>
{
    T ShallowClone();

    T DeepClone();
}

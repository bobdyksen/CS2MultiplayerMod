using System;
using System.Reflection;
using Unity.Entities;

namespace CS2MultiplayerMod.Game.Sync.Infrastructure
{
    /// <summary>
    /// Calls <see cref="EntityManager.CreateEntity(EntityArchetype)"/> without asking the compiler
    /// to bind that method group.
    ///
    /// Unity.Entities 1.3 also has <c>CreateEntity(ReadOnlySpan&lt;ComponentType&gt;)</c>. That
    /// overload has one argument, so <c>CreateEntity(archetype)</c> makes the compiler test whether
    /// an <see cref="EntityArchetype"/> converts to the span. <c>System.ReadOnlySpan&lt;T&gt;</c> is a
    /// compiler predefined type and has to live in the core library. The toolchain's .NET Framework
    /// reference mscorlib does not have it, so the call fails with CS0518. A reference to
    /// System.Memory.dll does not count: the compiler does not look there for predefined types.
    /// A typed local and a lower language version do not either, because both still perform that
    /// conversion check. Parameterless <c>CreateEntity()</c> is a different arity, so it never asks.
    ///
    /// The delegate is bound to the archetype overload by its parameter list. EntityManager is a
    /// pointer-sized struct, so the copy the property already returns still addresses the same world.
    /// </summary>
    internal static class ArchetypeEntities
    {
        private delegate Entity ArchetypeCreate(ref EntityManager manager, EntityArchetype archetype);

        private static readonly ArchetypeCreate CreateFromArchetype = Bind();

        public static Entity Create(EntityManager manager, EntityArchetype archetype)
        {
            // EntityManager is a struct and the property already returns a copy. Calling through
            // that copy is what CreateEntity(archetype) does today.
            return CreateFromArchetype(ref manager, archetype);
        }

        private static ArchetypeCreate Bind()
        {
            MethodInfo method = typeof(EntityManager).GetMethod(
                nameof(EntityManager.CreateEntity),
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(EntityArchetype) },
                null);
            if (method == null)
                throw new MissingMethodException(typeof(EntityManager).FullName, "CreateEntity(EntityArchetype)");
            // An open by-value Func cannot bind a struct instance method. The ref delegate can,
            // and it is the same overload: CreateEntity(EntityArchetype), not the span one.
            return (ArchetypeCreate)Delegate.CreateDelegate(typeof(ArchetypeCreate), method);
        }
    }
}

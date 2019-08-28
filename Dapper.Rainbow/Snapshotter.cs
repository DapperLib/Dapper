using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Dapper
{
    /// <summary>
    /// Snapshots an object for comparison later.
    /// </summary>
    public static class Snapshotter
    {
        /// <summary>
        /// Starts the snapshot of an objec by making a copy of the current state.
        /// </summary>
        /// <typeparam name="T">The type of object to snapshot.</typeparam>
        /// <param name="obj">The object to snapshot.</param>
        /// <returns>The snapshot of the object.</returns>
        public static Snapshot<T> Start<T>(T obj)
        {
            return new Snapshot<T>(obj);
        }

        /// <summary>
        /// A snapshot of an object's state.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class Snapshot<T>
        {
            private static Func<T, T> cloner;
            private static Func<T, T, List<Change>> differ;
            private readonly T memberWiseClone;
            private readonly T trackedObject;

            /// <summary>
            /// Creates a snapshot from an object.
            /// </summary>
            /// <param name="original">The original object to snapshot.</param>
            public Snapshot(T original)
            {
                memberWiseClone = Clone(original);
                trackedObject = original;
            }

            /// <summary>
            /// A holder for listing new values of changes fields and properties.
            /// </summary>
            public class Change
            {
                /// <summary>
                /// The name of the field or property that changed.
                /// </summary>
                public string Name { get; set; }
                /// <summary>
                /// The new value of the field or property.
                /// </summary>
                public object NewValue { get; set; }
            }

            /// <summary>
            /// Does a diff between the original object and the current state.
            /// </summary>
            /// <returns>The list of the fields changes in the object.</returns>
            public DynamicParameters Diff()
            {
                return Diff(memberWiseClone, trackedObject);
            }

            private static T Clone(T myObject)
            {
                cloner = cloner ?? GenerateCloner();
                return cloner(myObject);
            }

            private static DynamicParameters Diff(T original, T current)
            {
                var dm = new DynamicParameters();
                differ = differ ?? GenerateDiffer();
                foreach (var pair in differ(original, current))
                {
                    dm.Add(pair.Name, pair.NewValue);
                }
                return dm;
            }

            private static List<PropertyInfo> RelevantProperties()
            {
                return typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(p =>
                        p.GetSetMethod(true) != null
                        && p.GetGetMethod(true) != null
                        && (p.PropertyType == typeof(string)
                             || p.PropertyType.IsValueType
                             || (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                        ).ToList();
            }

            private static bool AreEqual<U>(U first, U second)
            {
                if (EqualityComparer<U>.Default.Equals(first, default(U)) && EqualityComparer<U>.Default.Equals(second, default(U))) return true;
                if (EqualityComparer<U>.Default.Equals(first, default(U))) return false;
                return first.Equals(second);
            }

            private static Func<T, T, List<Change>> GenerateDiffer()
            {
                var dm = new DynamicMethod("DoDiff", typeof(List<Change>), new[] { typeof(T), typeof(T) }, true);

                var il = dm.GetILGenerator();
                // change list
                var list = il.DeclareLocal(typeof(List<Change>));
                var change = il.DeclareLocal(typeof(Change));
                var boxed = il.DeclareLocal(typeof(object)); // boxed change

                il.Emit(OpCodes.Newobj, typeof(List<Change>).GetConstructor(Type.EmptyTypes));
                // [list]
                il.Emit(OpCodes.Stloc, list);

                foreach (var prop in RelevantProperties())
                {
                    // []
                    il.Emit(OpCodes.Ldarg_0);
                    // [original]
                    il.Emit(OpCodes.Callvirt, prop.GetGetMethod(true));
                    // [original prop val]
                    il.Emit(OpCodes.Ldarg_1);
                    // [original prop val, current]
                    il.Emit(OpCodes.Callvirt, prop.GetGetMethod(true));
                    // [original prop val, current prop val]

                    il.Emit(OpCodes.Dup);
                    // [original prop val, current prop val, current prop val]

                    if (prop.PropertyType != typeof(string))
                    {
                        il.Emit(OpCodes.Box, prop.PropertyType);
                        // [original prop val, current prop val, current prop val boxed]
                    }

                    il.Emit(OpCodes.Stloc, boxed);
                    // [original prop val, current prop val]

                    il.EmitCall(OpCodes.Call, typeof(Snapshot<T>).GetMethod(nameof(AreEqual), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(new Type[] { prop.PropertyType }), null);
                    // [result] 

                    Label skip = il.DefineLabel();
                    il.Emit(OpCodes.Brtrue_S, skip);
                    // []

                    il.Emit(OpCodes.Newobj, typeof(Change).GetConstructor(Type.EmptyTypes));
                    // [change]
                    il.Emit(OpCodes.Dup);
                    // [change,change]

                    il.Emit(OpCodes.Stloc, change);
                    // [change]

                    il.Emit(OpCodes.Ldstr, prop.Name);
                    // [change, name]
                    il.Emit(OpCodes.Callvirt, typeof(Change).GetMethod("set_Name"));
                    // []

                    il.Emit(OpCodes.Ldloc, change);
                    // [change]

                    il.Emit(OpCodes.Ldloc, boxed);
                    // [change, boxed]

                    il.Emit(OpCodes.Callvirt, typeof(Change).GetMethod("set_NewValue"));
                    // []

                    il.Emit(OpCodes.Ldloc, list);
                    // [change list]
                    il.Emit(OpCodes.Ldloc, change);
                    // [change list, change]
                    il.Emit(OpCodes.Callvirt, typeof(List<Change>).GetMethod("Add"));
                    // []

                    il.MarkLabel(skip);
                }

                il.Emit(OpCodes.Ldloc, list);
                // [change list]
                il.Emit(OpCodes.Ret);

                return (Func<T, T, List<Change>>)dm.CreateDelegate(typeof(Func<T, T, List<Change>>));
            }

            // adapted from https://stackoverflow.com/a/966466/17174
            private static Func<T, T> GenerateCloner()
            {
                var dm = new DynamicMethod("DoClone", typeof(T), new Type[] { typeof(T) }, true);
                var ctor = typeof(T).GetConstructor(new Type[] { });

                var il = dm.GetILGenerator();

                var typed = il.DeclareLocal(typeof(T));

                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Stloc, typed);

                foreach (var prop in RelevantProperties())
                {
                    il.Emit(OpCodes.Ldloc, typed);
                    // [clone]
                    il.Emit(OpCodes.Ldarg_0);
                    // [clone, source]
                    il.Emit(OpCodes.Callvirt, prop.GetGetMethod(true));
                    // [clone, source val]
                    il.Emit(OpCodes.Callvirt, prop.GetSetMethod(true));
                    // []
                }

                // Load new constructed obj on eval stack -> 1 item on stack
                il.Emit(OpCodes.Ldloc, typed);
                // Return constructed object.   --> 0 items on stack
                il.Emit(OpCodes.Ret);

                var myExec = dm.CreateDelegate(typeof(Func<T, T>));

                return (Func<T, T>)myExec;
            }
        }
    }
}

using System;

namespace Dapper.Tests.SQlite
{
    /// <summary>
    /// ClassWithGuidPropertyWithNoDefaultConstructor: simple class with a Guid property and no default constructor.
    /// </summary>
    public class ClassWithGuidPropertyWithNoDefaultConstructor : IEquatable<ClassWithGuidPropertyWithNoDefaultConstructor>
    {
        /// <summary>
        /// Id: is the GUID id for "this" record.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name: is an arbitary (null-guid) dummy property
        /// </summary>
        public string Name { get; set; }

        public ClassWithGuidPropertyWithNoDefaultConstructor(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <inheritdoc cref="ClassWithGuidPropertyWithNoDefaultConstructor" />
        public bool Equals(ClassWithGuidPropertyWithNoDefaultConstructor x, ClassWithGuidPropertyWithNoDefaultConstructor y)
        {
            return x.Id.Equals(y.Id) && x.Name.Equals(y.Name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ClassWithGuidPropertyWithNoDefaultConstructor other)
        {
            return Id.Equals(other.Id) && Name.Equals(other.Name);
        }

        /// <inheritdoc cref="ClassWithGuidPropertyWithNoDefaultConstructor" />
        public int GetHashCode(ClassWithGuidPropertyWithNoDefaultConstructor obj) => throw new NotImplementedException();
    }

}

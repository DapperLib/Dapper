using System;

namespace Dapper.Tests.SQlite
{
    /// <summary>
    /// ClassWithGuidPropertyWithDefaultConstructor: simple class with a Guid property and a default constructor.
    /// </summary>
    public class ClassWithGuidPropertyWithDefaultConstructor : IEquatable<ClassWithGuidPropertyWithDefaultConstructor>
    {
        /// <summary>
        /// Id: is the GUID id for "this" record.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name: is an arbitary (null-guid) dummy property
        /// </summary>
        public string Name { get; set; }

        /// <inheritdoc cref="ClassWithGuidPropertyWithDefaultConstructor " />
        public bool Equals(ClassWithGuidPropertyWithDefaultConstructor x, ClassWithGuidPropertyWithDefaultConstructor y)
        {
            return x.Id.Equals(y.Id) && x.Name.Equals(y.Name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ClassWithGuidPropertyWithDefaultConstructor other)
        {
            return Id.Equals(other.Id) && Name.Equals(other.Name);
        }

        /// <inheritdoc cref="ClassWithGuidPropertyWithDefaultConstructor" />
        public int GetHashCode(ClassWithGuidPropertyWithDefaultConstructor obj) => throw new NotImplementedException();
    }

}

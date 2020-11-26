using System;
using System.Collections.Generic;
using Topper = CrudDatastore.Topper;

namespace CrudDatastore.Topper.Test.Entities
{
    public class Person : Topper.EntityBase
    {
        public int PersonId { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public virtual ICollection<Identification> Identifications { get; set; }
    }
}

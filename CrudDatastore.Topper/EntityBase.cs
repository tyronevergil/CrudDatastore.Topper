using System;

namespace CrudDatastore.Topper
{
    public class EntityBase : CrudDatastore.EntityBase, ICreatedEntity, ILastModifiedEntity, IDeletedEntity
    {
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }

        public string LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        public bool IsDeleted { get; set; }
    }
}

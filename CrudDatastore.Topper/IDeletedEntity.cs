using System;

namespace CrudDatastore.Topper
{
    public interface IDeletedEntity
    {
        bool IsDeleted { get; set; }
    }
}

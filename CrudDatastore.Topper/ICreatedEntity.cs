using System;

namespace CrudDatastore.Topper
{
    public interface ICreatedEntity
    {
        string CreatedBy { get; set; }
        DateTime CreatedDate { get; set; }
    }
}

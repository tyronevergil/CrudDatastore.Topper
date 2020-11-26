using System;

namespace CrudDatastore.Topper
{
    public interface ILastModifiedEntity
    {
        string LastModifiedBy { get; set; }
        DateTime LastModifiedDate { get; set; }
    }
}

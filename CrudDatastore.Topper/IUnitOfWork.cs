using System;

namespace CrudDatastore.Topper
{
    public interface IUnitOfWork : CrudDatastore.IUnitOfWork, IContextInfo
    {
    }
}
